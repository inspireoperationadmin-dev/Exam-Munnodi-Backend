namespace ScholarFlow.Modules.Identity.DTOs;

public sealed record VerifyOtpResponse(
    string Email,
    bool RequiresAccountCreation,
    string? RegistrationTicket,
    DateTime? RegistrationTicketExpiresAt,
    AuthResponse? Authentication);
