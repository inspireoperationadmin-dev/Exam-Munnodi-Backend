using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Papers.UpdatePaper;

public sealed class UpdatePaperCommandHandler(
    IPaperRepository paperRepo,
    ICurrentUser currentUser)
    : IRequestHandler<UpdatePaperCommand>
{
    public async Task Handle(UpdatePaperCommand request, CancellationToken ct)
    {
        var paper = await paperRepo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Paper not found.");

        if (currentUser.IsInRole(AppRole.Teacher))
        {
            var isOwner = await paperRepo.IsTeacherOwnerAsync(request.Id, currentUser.UserId, ct);
            if (!isOwner) throw new ForbiddenException("You can only update your own papers.");
        }

        var duplicate = await paperRepo.ExistsByCompositeKeyExcludingIdAsync(
            request.Id, request.Title, request.SubjectId, request.Year, request.Type, request.Medium, ct);

        if (duplicate)
            throw new ConflictException(
                "A paper with the same title, subject, year, type, and medium already exists.");

        paper.Update(
            subjectId:         request.SubjectId,
            title:             request.Title,
            year:              request.Year,
            type:              request.Type,
            medium:            request.Medium,
            sitting:           request.Sitting,
            negativeMarkValue: request.NegativeMarkValue,
            timeLimit:         request.TimeLimit,
            officialPaperCode: request.OfficialPaperCode);

        paperRepo.Update(paper);
        await paperRepo.SaveChangesAsync(ct);
    }
}