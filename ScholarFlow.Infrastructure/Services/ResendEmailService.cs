using System.Net.Http.Headers;
using System.Net;
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
        await SendAsync(
            toEmail,
            "Your Exam Munnodi verification code",
            $"""
                <div style="font-family:sans-serif;max-width:480px;margin:0 auto;padding:32px">
                  <h2 style="color:#0F172A;margin-bottom:8px">Email Verification</h2>
                  <p style="color:#475569;margin-bottom:24px">
                    Use the code below to verify your email address.
                    It expires in <strong>10 minutes</strong>.
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
            ct);
    }

    public async Task SendAccountAccessChangedAsync(
        string toEmail,
        string fullName,
        string accountStatus,
        DateTimeOffset? suspendedUntil,
        CancellationToken ct = default)
    {
        var safeName = WebUtility.HtmlEncode(fullName);
        var safeStatus = WebUtility.HtmlEncode(accountStatus.ToLowerInvariant());
        var timing = suspendedUntil.HasValue
            ? $" until <strong>{suspendedUntil.Value.UtcDateTime:dd MMM yyyy, HH:mm} UTC</strong>"
            : string.Empty;
        var guidance = accountStatus == "Active"
            ? "You can now sign in and continue using your account."
            : "If you believe this was a mistake or need help, please contact Exam Munnodi support.";

        await SendAsync(
            toEmail,
            $"Your Exam Munnodi account is {safeStatus}",
            $"""
                <div style="font-family:sans-serif;max-width:520px;margin:0 auto;padding:32px">
                  <h2 style="color:#0F172A;margin-bottom:8px">Account access updated</h2>
                  <p style="color:#475569">Hello {safeName},</p>
                  <p style="color:#475569;line-height:1.6">
                    Your Exam Munnodi account has been <strong>{safeStatus}</strong>{timing}.
                  </p>
                  <p style="color:#475569;line-height:1.6">{guidance}</p>
                  <p style="color:#94A3B8;font-size:13px;margin-top:24px">
                    This is an automated account security notification.
                  </p>
                </div>
                """,
            ct);
    }

    public async Task SendAccountDeletedAsync(
        string toEmail,
        CancellationToken ct = default)
    {
        await SendAsync(
            toEmail,
            "Your Exam Munnodi account has been deleted",
            """
                <div style="font-family:sans-serif;max-width:520px;margin:0 auto;padding:32px">
                  <h2 style="color:#0F172A;margin-bottom:8px">Account deleted</h2>
                  <p style="color:#475569;line-height:1.6">
                    Your Exam Munnodi student account and its associated learning data have been permanently deleted.
                  </p>
                  <p style="color:#475569;line-height:1.6">
                    If you did not expect this action, please contact Exam Munnodi support.
                  </p>
                  <p style="color:#94A3B8;font-size:13px;margin-top:24px">
                    This is an automated account security notification.
                  </p>
                </div>
                """,
            ct);
    }

    private async Task SendAsync(
        string toEmail,
        string subject,
        string html,
        CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("Resend");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", settings.Value.ApiKey);

        var payload = new
        {
            from = "Exam Munnodi <support@exammunnodi.com>",
            to = new[] { toEmail },
            subject,
            html,
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
