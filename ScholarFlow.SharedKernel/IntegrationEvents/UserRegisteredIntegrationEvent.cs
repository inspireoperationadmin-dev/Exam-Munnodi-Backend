using ScholarFlow.SharedKernel.Primitives;

namespace ScholarFlow.SharedKernel.IntegrationEvents;

/// <summary>
/// Published by the Identity module after a user account is successfully created.
/// The UserProfiles module handles this to create the corresponding domain profile.
/// TeacherCode is NOT included — profile creation details are a UserProfiles concern.
/// </summary>
public sealed record UserRegisteredIntegrationEvent(
    Guid      EventId,
    DateTime  OccurredOn,
    Guid      UserId,
    string    Email,
    string    Role,
    string    FullName,
    string?   PhoneNumber,
    // Teacher-specific — null when Role = Student
    Guid?     SubjectId,
    string?   Qualification,
    string?   Bio
) : IIntegrationEvent;
