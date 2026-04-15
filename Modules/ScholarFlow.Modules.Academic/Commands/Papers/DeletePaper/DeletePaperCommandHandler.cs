using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Papers.DeletePaper;

public sealed class DeletePaperCommandHandler(
    IPaperRepository paperRepo,
    ICurrentUser currentUser)
    : IRequestHandler<DeletePaperCommand>
{
    public async Task Handle(DeletePaperCommand request, CancellationToken ct)
    {
        var paper = await paperRepo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Paper not found.");

        if (currentUser.IsInRole(AppRole.Teacher))
        {
            var isOwner = await paperRepo.IsTeacherOwnerAsync(request.Id, currentUser.UserId, ct);
            if (!isOwner) throw new ForbiddenException("You can only delete your own papers.");
        }

        paperRepo.Delete(paper);
        await paperRepo.SaveChangesAsync(ct);
    }
}
