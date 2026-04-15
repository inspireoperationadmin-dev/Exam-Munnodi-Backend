namespace ScholarFlow.Modules.Identity.DTOs;

public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAt,
    Guid UserId,
    string Email,
    string Role);
