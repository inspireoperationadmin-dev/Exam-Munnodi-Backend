using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ScholarFlow.Infrastructure.Settings;

namespace ScholarFlow.Infrastructure.Services;

public sealed class RegistrationTicketService(IOptions<JwtSettings> options)
    : IRegistrationTicketService
{
    private const string Purpose = "registration";
    private readonly JwtSettings _settings = options.Value;

    public RegistrationTicket Create(Guid otpId, string email, DateTime expiresAt)
    {
        var credentials = new SigningCredentials(CreateKey(), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("purpose", Purpose),
            new Claim("otp_id", otpId.ToString())
        };

        var jwt = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new RegistrationTicket(
            new JwtSecurityTokenHandler().WriteToken(jwt),
            expiresAt);
    }

    public bool TryValidate(string token, out RegistrationTicketPayload payload)
    {
        payload = default!;

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = CreateKey(),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            }, out var validatedToken);

            if (principal.FindFirst("purpose")?.Value != Purpose ||
                !Guid.TryParse(principal.FindFirst("otp_id")?.Value, out var otpId))
                return false;

            var email = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrWhiteSpace(email) || validatedToken.ValidTo == DateTime.MinValue)
                return false;

            payload = new RegistrationTicketPayload(
                otpId,
                email.Trim().ToLowerInvariant(),
                validatedToken.ValidTo.ToUniversalTime());
            return true;
        }
        catch (SecurityTokenException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private SymmetricSecurityKey CreateKey()
        => new(Encoding.UTF8.GetBytes(_settings.Secret));
}
