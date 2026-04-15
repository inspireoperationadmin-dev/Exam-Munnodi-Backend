using MediatR;
using ScholarFlow.Modules.Identity.DTOs;

namespace ScholarFlow.Modules.Identity.Commands.Login;

/// <summary>
/// Authenticates a user and returns a JWT.
/// Throws <see cref="ScholarFlow.SharedKernel.Exceptions.UnauthorizedException"/> on failure.
/// </summary>
public sealed record LoginCommand(
    string Email,
    string Password
) : IRequest<AuthResponse>;
