using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using ScholarFlow.Infrastructure;
using ScholarFlow.Infrastructure.Persistence;
using ScholarFlow.Modules.Academic;
using ScholarFlow.Modules.Analytics;
using ScholarFlow.Modules.Examination;
using ScholarFlow.Modules.Identity;
using ScholarFlow.Modules.UserProfiles;
using ScholarFlow.WebAPI.Filters;
using ScholarFlow.WebAPI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure (DbContext, Identity, TokenService) ────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── CORS Configuration ──────────────────────────────────────────────────────
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

// ── JWT Authentication ────────────────────────────────────────────────────────
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience            = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]!))
        };
    });

// ── Global exception handler ──────────────────────────────────────────────────
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ── API ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllers(options =>
    options.Filters.Add<ApiResponseFilter>());

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
app.MapControllers();

app.Run();
