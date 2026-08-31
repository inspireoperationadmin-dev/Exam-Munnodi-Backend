using MediatR;

namespace ScholarFlow.Modules.Identity.Commands.SendOtp;

/// <summary>
/// Generates a 6-digit OTP, stores a hash in the database, and emails it via Gmail SMTP.
/// The public response does not reveal whether a permanent account exists.
/// </summary>
public sealed record SendOtpCommand(string Email) : IRequest;
