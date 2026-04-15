using MediatR;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.IntegrationEvents;

namespace ScholarFlow.Modules.UserProfiles.EventHandlers;

/// <summary>
/// Creates the domain profile for a newly registered user.
/// Triggered by <see cref="UserRegisteredIntegrationEvent"/> published from the Identity module.
/// Idempotent — skips creation if a profile already exists for the user.
/// </summary>
public sealed class UserRegisteredIntegrationEventHandler(
    IStudentProfileRepository studentRepo,
    ITeacherProfileRepository teacherRepo)
    : INotificationHandler<UserRegisteredIntegrationEvent>
{
    public async Task Handle(UserRegisteredIntegrationEvent e, CancellationToken ct)
    {
        if (e.Role == AppRole.Teacher)
            await HandleTeacher(e, ct);
        else
            await HandleStudent(e, ct);
    }

    private async Task HandleStudent(UserRegisteredIntegrationEvent e, CancellationToken ct)
    {
        if (await studentRepo.ExistsByUserIdAsync(e.UserId, ct)) return;

        var profile = StudentProfile.Create(
            userId:           e.UserId,
            fullName:         e.FullName,
            phoneNumber:      e.PhoneNumber,
            academicStreamId: null);

        await studentRepo.AddAsync(profile, ct);
        await studentRepo.SaveChangesAsync(ct);
    }

    private async Task HandleTeacher(UserRegisteredIntegrationEvent e, CancellationToken ct)
    {
        if (await teacherRepo.ExistsByUserIdAsync(e.UserId, ct)) return;

        // TeacherCode is a UserProfiles concern — generated here, not in the Identity module
        var teacherCode = $"TCH-{Guid.NewGuid():N}"[..10].ToUpper();

        var profile = TeacherProfile.Create(
            userId:        e.UserId,
            fullName:      e.FullName,
            subjectId:     e.SubjectId!.Value,
            phoneNumber:   e.PhoneNumber!,
            qualification: e.Qualification!,
            bio:           e.Bio,
            teacherCode:   teacherCode);

        await teacherRepo.AddAsync(profile, ct);
        await teacherRepo.SaveChangesAsync(ct);
    }
}
