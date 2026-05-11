using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using ScholarFlow.Infrastructure.Settings;

namespace ScholarFlow.Infrastructure.Services;

public sealed class SmtpEmailService(
    IOptions<SmtpSettings> settings,
    ILogger<SmtpEmailService> logger)
    : IEmailService
{
    public async Task SendOtpAsync(string toEmail, string otpCode, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[OTP] Code for {Email} → {Code}  (also attempting email delivery)",
            toEmail, otpCode);

        var s = settings.Value;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(s.FromName, s.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Your Exam Munnodi verification code";
        message.Body = new BodyBuilder
        {
            HtmlBody = $"""
                <div style="font-family:sans-serif;max-width:480px;margin:0 auto;padding:32px">
                  <h2 style="color:#0F172A;margin-bottom:8px">Email Verification</h2>
                  <p style="color:#475569;margin-bottom:24px">
                    Use the code below to verify your email address.
                    It expires in <strong>5 minutes</strong>.
                  </p>
                  <div style="background:#F1F5F9;border-radius:12px;padding:24px;text-align:center;
                              font-size:36px;font-weight:700;letter-spacing:12px;color:#0F172A">
                    {otpCode}
                  </div>
                  <p style="color:#94A3B8;font-size:13px;margin-top:24px">
                    If you didn't request this, you can safely ignore this email.
                  </p>
                </div>
                """
        }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(s.Host, s.Port, SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(s.Username, s.Password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
