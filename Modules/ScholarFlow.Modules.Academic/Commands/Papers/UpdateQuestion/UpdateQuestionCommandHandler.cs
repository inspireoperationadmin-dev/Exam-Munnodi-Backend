using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Papers.UpdateQuestion;

public sealed class UpdateQuestionCommandHandler(
    IQuestionRepository questionRepo,
    IPaperRepository paperRepo,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateQuestionCommand>
{
    public async Task Handle(UpdateQuestionCommand request, CancellationToken ct)
    {
        var question = await questionRepo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Question not found.");

        if (currentUser.IsInRole(AppRole.Teacher))
        {
            var isOwner = await paperRepo.IsTeacherOwnerAsync(question.PaperId, currentUser.UserId, ct);
            if (!isOwner) throw new ForbiddenException("You can only update questions in your own papers.");
        }

        question.Update(request.SubTopicId, request.QuestionText, request.QuestionImageUrl, request.OrderIndex, request.ManualDifficulty);
        questionRepo.Update(question);
        await questionRepo.SaveChangesAsync(ct);
    }
}
