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
        var exists = await paperRepo.ExistsByCompositeKeyAsync(
            request.Title, request.SubjectId, request.Year, request.Type, request.Medium, ct);

        if (exists)
            throw new ConflictException(
                $"A paper with the same title, subject, year, type, and medium already exists.");

        var isPublic = currentUser.IsInRole(AppRole.Teacher) ? false : request.IsPublic;

        Guid? teacherProfileId = null;
        if (currentUser.IsInRole(AppRole.Teacher))
        {
            teacherProfileId = await paperRepo.GetTeacherProfileIdByUserIdAsync(currentUser.UserId, ct)
                ?? throw new NotFoundException("Teacher profile not found.");
        }

        var paper = Paper.Create(
            subjectId:          request.SubjectId,
            title:              request.Title,
            year:               request.Year,
            type:               request.Type,
            medium:             request.Medium,
            isPublic:           isPublic,
            timeLimit:          request.TimeLimit,
            negativeMarkValue:  request.NegativeMarkValue,
            sitting:            request.Sitting,
            officialPaperCode:  request.OfficialPaperCode,
            createdByTeacherId: teacherProfileId);

        await paperRepo.AddAsync(paper, ct);
        await paperRepo.SaveChangesAsync(ct);

        return paper.Id;
    }
}