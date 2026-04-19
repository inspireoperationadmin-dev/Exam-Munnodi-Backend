using MediatR;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Papers.AddQuestionToPaper;

public sealed class AddQuestionToPaperCommandHandler(
    IPaperRepository paperRepo,
    IQuestionRepository questionRepo,
    ICurrentUser currentUser)
    : IRequestHandler<AddQuestionToPaperCommand, Guid>
{
    public async Task<Guid> Handle(AddQuestionToPaperCommand request, CancellationToken ct)
    {
        var paper = await paperRepo.GetByIdAsync(request.PaperId, ct)
            ?? throw new NotFoundException("Paper not found.");

        if (currentUser.IsInRole(AppRole.Teacher))
        {
            var isOwner = await paperRepo.IsTeacherOwnerAsync(request.PaperId, currentUser.UserId, ct);
            if (!isOwner) throw new ForbiddenException("You can only add questions to your own papers.");
        }

        var question = Question.Create(
            paperId:          paper.Id,
            subTopicId:       request.SubTopicId,
            questionText:     request.QuestionText,
            orderIndex:       request.OrderIndex,
            questionImageUrl: request.QuestionImageUrl,
            manualDifficulty: request.ManualDifficulty);

        await questionRepo.AddAsync(question, ct);
        await questionRepo.SaveChangesAsync(ct);

        foreach (var opt in request.Options)
        {
            var option = new Option
            {
                Id             = Guid.NewGuid(),
                QuestionId     = question.Id,
                Label          = opt.Label,
                OptionText     = opt.OptionText,
                OptionImageUrl = opt.OptionImageUrl,
                IsCorrect      = opt.IsCorrect
            };
            await questionRepo.AddOptionAsync(option, ct);
        }

        await questionRepo.SaveChangesAsync(ct);

        return question.Id;
    }
}
