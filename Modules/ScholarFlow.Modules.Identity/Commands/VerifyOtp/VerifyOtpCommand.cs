using MediatR;

namespace ScholarFlow.Modules.Identity.Commands.VerifyOtp;

/// <summary>
/// Verifies a 6-digit OTP for the given email.
/// Marks the record as verified so the register endpoint can consume it.
/// Throws BadRequestException if the code is wrong or expired.
/// </summary>
public sealed record VerifyOtpCommand(string Email, string Code) : IRequest;
