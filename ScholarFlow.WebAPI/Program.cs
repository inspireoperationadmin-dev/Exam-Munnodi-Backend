using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using ScholarFlow.Infrastructure;
using ScholarFlow.Infrastructure.Persistence;
using ScholarFlow.Modules.Academic;
using ScholarFlow.Modules.Analytics;
using ScholarFlow.Modules.Examination;
using ScholarFlow.Modules.Identity;
using ScholarFlow.Modules.Notifications;
using ScholarFlow.Modules.Subscriptions;
using ScholarFlow.Modules.UserProfiles;
using ScholarFlow.WebAPI.Filters;
using ScholarFlow.WebAPI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── Validate required secrets at startup ──────────────────────────────────────
var jwtSecret = builder.Configuration["JwtSettings:Secret"]
    ?? throw new InvalidOperationException("JwtSettings:Secret is not configured.");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

// ── Infrastructure (DbContext, Identity, TokenService) ────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ── Modules ───────────────────────────────────────────────────────────────────
builder.Services.AddIdentityModule();
builder.Services.AddUserProfilesModule();
builder.Services.AddAcademicModule();
builder.Services.AddExaminationModule();
builder.Services.AddAnalyticsModule();
builder.Services.AddNotificationsModule();
builder.Services.AddSubscriptionsModule(builder.Configuration);

// ── JWT Authentication ────────────────────────────────────────────────────────
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience            = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                                            Encoding.UTF8.GetBytes(jwtSecret))
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdValue = context.Principal?.FindFirst("sub")?.Value;
                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    context.Fail("The access token does not identify a valid user.");
                    return;
                }

                var db = context.HttpContext.RequestServices
                    .GetRequiredService<ApplicationDbContext>();
                var account = await db.Users
                    .AsNoTracking()
                    .Where(user => user.Id == userId)
                    .Select(user => new { user.LockoutEnabled, user.LockoutEnd })
                    .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

                if (account is null ||
                    (account.LockoutEnabled && account.LockoutEnd > DateTimeOffset.UtcNow))
                {
                    context.Fail("Account access is restricted.");
                }
            }
        };
    });

// ── Global exception handler ──────────────────────────────────────────────────
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ── API ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllers(options =>
    options.Filters.Add<ApiResponseFilter>())
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        // Keep enum values as strings (e.g. "Student" not 0)
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
    });

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Components ??= new Microsoft.OpenApi.OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Type         = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            Description  = "Enter your JWT token (without 'Bearer ' prefix)"
        };
        document.Security ??= [];
        document.Security.Add(new Microsoft.OpenApi.OpenApiSecurityRequirement
        {
            [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// ── Seed ──────────────────────────────────────────────────────────────────────
await DataSeeder.SeedAsync(app.Services);

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.EndpointPathPrefix = "/scalar/{documentName}";
        options.Authentication = new Scalar.AspNetCore.ScalarAuthenticationOptions
        {
            PreferredSecurityScheme = "Bearer"
        };
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<OnboardingEnforcementMiddleware>();
app.MapControllers();


app.Run();
