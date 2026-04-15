using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ScholarFlow.Domain.Interfaces;

namespace ScholarFlow.Infrastructure.Services;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid UserId =>
        Guid.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : Guid.Empty;

    public string Email =>
        Principal?.FindFirstValue(JwtRegisteredClaimNames.Email) ?? string.Empty;

    public string Role =>
        Principal?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated is true;

    public bool IsInRole(string role) =>
        Principal?.IsInRole(role) is true;
}
