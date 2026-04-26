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
        var profile = await profileRepo.GetByUserIdWithDetailsAsync(currentUser.UserId, ct)
            ?? throw new NotFoundException("Student profile not found.");

        // Remove existing subject selections before adding new ones
        if (profile.SubjectSelections.Count > 0)
            profileRepo.RemoveSubjectSelections(profile.SubjectSelections);

        // Apply stream, medium, exam year
        profile.CompleteSetup(request.StreamId, request.Medium, request.ExamYear);
        profileRepo.Update(profile);

        // Add new subject selections
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
}