using MediatR;

namespace ScholarFlow.Modules.Identity.Commands.SendOtp;

/// <summary>
/// Generates a 6-digit OTP, stores a hash in the database, and emails it via Gmail SMTP.
/// Always returns 200 OK — never reveals whether the email exists.
/// </summary>
public sealed record SendOtpCommand(string Email) : IRequest;
