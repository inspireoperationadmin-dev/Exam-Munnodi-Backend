using MediatR;
using Microsoft.AspNetCore.Identity;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.UserProfiles.Commands.SetupStudentProfile;

public sealed class SetupStudentProfileCommandHandler(
    IStudentProfileRepository    profileRepo,
    UserManager<ApplicationUser> userManager,
    ICurrentUser                 currentUser)
    : IRequestHandler<SetupStudentProfileCommand>
{
    public async Task Handle(SetupStudentProfileCommand request, CancellationToken ct)
    {
        // ── Guard: email must be verified before profile setup is allowed ─────────
        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString())
            ?? throw new NotFoundException("User not found.");

        if (!user.EmailConfirmed)
            throw new ForbiddenException("Email must be verified before setting up your profile.");

        var profile = await profileRepo.GetByUserIdWithDetailsAsync(currentUser.UserId, ct)
            ?? throw new NotFoundException("Student profile not found.");

        // Replace existing subject selections
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

        user.MarkProfileSetup();
        await userManager.UpdateAsync(user);
    }
}