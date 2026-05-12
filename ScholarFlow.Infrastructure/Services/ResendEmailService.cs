using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarFlow.Infrastructure.Settings;

namespace ScholarFlow.Infrastructure.Services;

public sealed class ResendEmailService(
    IHttpClientFactory httpClientFactory,
    IOptions<ResendSettings> settings,
    ILogger<ResendEmailService> logger)
    : IEmailService
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task SendOtpAsync(string toEmail, string otpCode, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[OTP] Code for {Email} → {Code}  (also attempting email delivery)",
            toEmail, otpCode);

        var client = httpClientFactory.CreateClient("Resend");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", settings.Value.ApiKey);

        var payload = new
        {
            from    = "Exam Munnodi <support@exammunnodi.com>",
            to      = new[] { toEmail },
            subject = "Your Exam Munnodi verification code",
            html    = $"""
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
                """,
        };

        var json     = JsonSerializer.Serialize(payload, _json);
        var content  = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("emails", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(
                "[Resend] Rejected. Status={Status} Body={Body}",
                (int)response.StatusCode, body);
            throw new InvalidOperationException(
                $"Email delivery failed (HTTP {(int)response.StatusCode}). See server logs.");
        }
    }
}
