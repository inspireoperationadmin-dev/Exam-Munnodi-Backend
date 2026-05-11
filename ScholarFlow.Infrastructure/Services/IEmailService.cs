namespace ScholarFlow.Infrastructure.Services;

public interface IEmailService
{
    Task SendOtpAsync(string toEmail, string otpCode, CancellationToken ct = default);
}
