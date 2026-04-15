using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Papers.DeleteQuestion;

public sealed class DeleteQuestionCommandHandler(
    IQuestionRepository questionRepo,
    IPaperRepository paperRepo,
    ICurrentUser currentUser)
    : IRequestHandler<DeleteQuestionCommand>
{
    public async Task Handle(DeleteQuestionCommand request, CancellationToken ct)
    {
        var question = await questionRepo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Question not found.");

        if (currentUser.IsInRole(AppRole.Teacher))
        {
            var isOwner = await paperRepo.IsTeacherOwnerAsync(question.PaperId, currentUser.UserId, ct);
            if (!isOwner) throw new ForbiddenException("You can only delete questions from your own papers.");
        }

        questionRepo.Delete(question);
        await questionRepo.SaveChangesAsync(ct);
    }
}
