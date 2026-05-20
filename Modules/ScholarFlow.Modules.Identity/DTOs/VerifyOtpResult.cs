namespace ScholarFlow.Modules.Identity.DTOs;

public sealed record VerifyOtpResult(
    Guid UserId,
    bool IsProfileSetup
);