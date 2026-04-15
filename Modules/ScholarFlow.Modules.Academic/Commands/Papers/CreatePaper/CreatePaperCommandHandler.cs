using MediatR;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Papers.CreatePaper;

public sealed class CreatePaperCommandHandler(
    IPaperRepository paperRepo,
    ICurrentUser currentUser)
    : IRequestHandler<CreatePaperCommand, Guid>
{
    public async Task<Guid> Handle(CreatePaperCommand request, CancellationToken ct)
    {
        if (await paperRepo.ExistsByTitleAsync(request.Title, ct))
            throw new ConflictException($"A paper titled '{request.Title}' already exists.");

        // Teachers always create private papers; Admins respect the IsPublic field.
        var isPublic = currentUser.IsInRole(AppRole.Teacher) ? false : request.IsPublic;

        Guid? teacherProfileId = null;
        if (currentUser.IsInRole(AppRole.Teacher))
        {
            teacherProfileId = await paperRepo.GetTeacherProfileIdByUserIdAsync(currentUser.UserId, ct)
                ?? throw new NotFoundException("Teacher profile not found.");
        }

        var paper = Paper.Create(
            subjectId:         request.SubjectId,
            title:             request.Title,
            year:              request.Year,
            type:              request.Type,
            medium:            request.Medium,
            isPublic:          isPublic,
            negativeMarkValue: request.NegativeMarkValue,
            sitting:           request.Sitting,
            officialPaperCode: request.OfficialPaperCode,
            createdByTeacherId: teacherProfileId);

        await paperRepo.AddAsync(paper, ct);
        await paperRepo.SaveChangesAsync(ct);

        return paper.Id;
    }
}
