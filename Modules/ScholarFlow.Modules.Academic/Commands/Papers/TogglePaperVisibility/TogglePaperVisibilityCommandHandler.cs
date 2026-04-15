using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Papers.TogglePaperVisibility;

public sealed class TogglePaperVisibilityCommandHandler(
    IPaperRepository paperRepo,
    ICurrentUser currentUser)
    : IRequestHandler<TogglePaperVisibilityCommand>
{
    public async Task Handle(TogglePaperVisibilityCommand request, CancellationToken ct)
    {
        var paper = await paperRepo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Paper not found.");

        if (currentUser.IsInRole(AppRole.Teacher))
        {
            var isOwner = await paperRepo.IsTeacherOwnerAsync(request.Id, currentUser.UserId, ct);
            if (!isOwner) throw new ForbiddenException("You can only update visibility of your own papers.");
        }

        paper.UpdateVisibility(request.IsPublic);
        paperRepo.Update(paper);
        await paperRepo.SaveChangesAsync(ct);
    }
}
