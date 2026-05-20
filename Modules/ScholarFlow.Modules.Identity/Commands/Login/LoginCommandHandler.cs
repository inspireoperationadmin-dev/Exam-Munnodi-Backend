using MediatR;
using Microsoft.AspNetCore.Identity;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Infrastructure.Services;
using ScholarFlow.Modules.Identity.Commands.SendOtp;
using ScholarFlow.Modules.Identity.DTOs;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Identity.Commands.Login;

public sealed class LoginCommandHandler(
    UserManager<ApplicationUser>  userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService                  tokenService,
    IMediator                     mediator)
    : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        // ── 1. Find user ──────────────────────────────────────────────────────
        var user = await userManager.FindByEmailAsync(
                       request.Email.Trim().ToLowerInvariant())
                   ?? throw new UnauthorizedException("Invalid email or password.");

        // ── 2. Validate password ──────────────────────────────────────────────
        var signInResult = await signInManager
            .CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            if (signInResult.IsLockedOut)
                throw new ForbiddenException(
                    "Account is temporarily locked. Please try again later.");

            throw new UnauthorizedException("Invalid email or password.");
        }

        var roles = await userManager.GetRolesAsync(user);
        var role  = roles.FirstOrDefault() ?? string.Empty;

        // ── 3. Email not verified → resend OTP, return unverified state ───────
        //    Frontend sees IsEmailVerified=false and navigates to OTP page.
        //    The issued token has no access to protected routes (middleware enforces this).
        if (!user.EmailConfirmed)
        {
            // Resend OTP (reuses SendOtpCommand — rate limit still applies)
            await mediator.Send(new SendOtpCommand(user.Email!), ct);

            return new AuthResponse(
                AccessToken:     tokenService.GenerateToken(user.Id, user.Email!, role),
                ExpiresAt:       tokenService.TokenExpiresAt(),
                UserId:          user.Id,
                Email:           user.Email!,
                Role:            role,
                IsEmailVerified: false,
                IsProfileSetup:  false);
        }

        // ── 4. Email verified but profile not complete → redirect to setup ─────
        if (!user.IsProfileSetup)
        {
            return new AuthResponse(
                AccessToken:     tokenService.GenerateToken(user.Id, user.Email!, role),
                ExpiresAt:       tokenService.TokenExpiresAt(),
                UserId:          user.Id,
                Email:           user.Email!,
                Role:            role,
                IsEmailVerified: true,
                IsProfileSetup:  false);
        }

        // ── 5. Fully onboarded — full access ──────────────────────────────────
        return new AuthResponse(
            AccessToken:     tokenService.GenerateToken(user.Id, user.Email!, role),
            ExpiresAt:       tokenService.TokenExpiresAt(),
            UserId:          user.Id,
            Email:           user.Email!,
            Role:            role,
            IsEmailVerified: true,
            IsProfileSetup:  true);
    }
}