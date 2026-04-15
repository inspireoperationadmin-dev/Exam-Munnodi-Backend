using MediatR;
using Microsoft.AspNetCore.Identity;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Infrastructure.Services;
using ScholarFlow.Modules.Identity.DTOs;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Identity.Commands.Login;

public sealed class LoginCommandHandler(
    UserManager<ApplicationUser>   userManager,
    SignInManager<ApplicationUser>  signInManager,
    ITokenService                   tokenService)
    : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedException("Invalid email or password.");

        var signInResult = await signInManager
            .CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            if (signInResult.IsLockedOut)
                throw new ForbiddenException("Account is temporarily locked. Please try again later.");

            throw new UnauthorizedException("Invalid email or password.");
        }

        var roles = await userManager.GetRolesAsync(user);
        var role  = roles.FirstOrDefault() ?? string.Empty;

        return new AuthResponse(
            AccessToken: tokenService.GenerateToken(user.Id, user.Email!, role),
            ExpiresAt:   tokenService.TokenExpiresAt(),
            UserId:      user.Id,
            Email:       user.Email!,
            Role:        role);
    }
}
