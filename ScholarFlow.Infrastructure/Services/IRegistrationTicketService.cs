namespace ScholarFlow.Infrastructure.Services;

public sealed record RegistrationTicketPayload(Guid OtpId, string Email, DateTime ExpiresAt);

public sealed record RegistrationTicket(string Token, DateTime ExpiresAt);

public interface IRegistrationTicketService
{
    RegistrationTicket Create(Guid otpId, string email, DateTime expiresAt);
    bool TryValidate(string token, out RegistrationTicketPayload payload);
}
