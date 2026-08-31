namespace ScholarFlow.Infrastructure.Services;

public interface IEmailService
{
    Task SendOtpAsync(string toEmail, string otpCode, CancellationToken ct = default);
    Task SendAccountAccessChangedAsync(
        string toEmail,
        string fullName,
        string accountStatus,
        DateTimeOffset? suspendedUntil,
        CancellationToken ct = default);
    Task SendAccountDeletedAsync(string toEmail, CancellationToken ct = default);
}
