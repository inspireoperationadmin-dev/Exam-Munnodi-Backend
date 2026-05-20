using MediatR;
using ScholarFlow.Modules.Identity.DTOs;

namespace ScholarFlow.Modules.Identity.Commands.VerifyOtp;

public sealed record VerifyOtpCommand(string Email, string Code) : IRequest<AuthResponse>;