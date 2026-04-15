namespace ScholarFlow.Infrastructure.Services;

public interface ITokenService
{
    string GenerateToken(Guid userId, string email, string role);
    DateTime TokenExpiresAt();
}
