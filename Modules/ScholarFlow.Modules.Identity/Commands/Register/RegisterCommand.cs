using MediatR;
using ScholarFlow.Modules.Identity.DTOs;

namespace ScholarFlow.Modules.Identity.Commands.Register;

/// <summary>
/// Creates a Student or Teacher account after email verification.
/// Teacher-only fields (SubjectId, Qualification) are required when Role = "Teacher".
/// Throws <see cref="ScholarFlow.SharedKernel.Exceptions.ConflictException"/> if email is taken.
/// </summary>
public sealed record RegisterCommand(
    string RegistrationTicket,
    string Email,
    string Password,
    string FullName,
    string Role,             // AppRole.Student | AppRole.Teacher
    string? PhoneNumber,
    Guid?   SubjectId,       // Teacher only
    string? Qualification,   // Teacher only
    string? Bio              // Teacher only, optional
) : IRequest<AuthResponse>;
