using MediatR;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.UserProfiles.Commands.SetupStudentProfile;

public sealed class SetupStudentProfileCommandHandler(
    IStudentProfileRepository profileRepo,
    ICurrentUser              currentUser)
    : IRequestHandler<SetupStudentProfileCommand>
{
public async Task Handle(SetupStudentProfileCommand request, CancellationToken ct)
{
    try
    {
        var profile = await profileRepo.GetByUserIdWithDetailsAsync(currentUser.UserId, ct)
            ?? throw new NotFoundException("Student profile not found.");

        if (profile.SubjectSelections.Count > 0)
            profileRepo.RemoveSubjectSelections(profile.SubjectSelections);

        profile.CompleteSetup(request.StreamId, request.Medium, request.ExamYear);

        foreach (var subjectId in request.SubjectIds)
        {
            await profileRepo.AddSubjectSelectionAsync(new StudentSubjectSelection
            {
                Id               = Guid.NewGuid(),
                StudentProfileId = profile.Id,
                SubjectId        = subjectId,
                AddedAt          = DateTime.UtcNow,
            }, ct);
        }

        await profileRepo.SaveChangesAsync(ct);
    }
    catch (Exception ex)
    {
        // Temporary — remove after diagnosis
        throw new Exception($"SETUP_HANDLER_EXCEPTION: {ex.GetType().Name}: {ex.Message} | Inner: {ex.InnerException?.Message}", ex);
    }
}
}