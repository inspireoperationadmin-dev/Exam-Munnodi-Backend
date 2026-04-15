using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Papers.UpdateOption;

public sealed class UpdateOptionCommandHandler(
    IQuestionRepository questionRepo,
    IPaperRepository paperRepo,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateOptionCommand>
{
    public async Task Handle(UpdateOptionCommand request, CancellationToken ct)
    {
        var option = await questionRepo.GetOptionByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Option not found.");

        var question = await questionRepo.GetByIdAsync(option.QuestionId, ct)
            ?? throw new NotFoundException("Question not found.");

        if (currentUser.IsInRole(AppRole.Teacher))
        {
            var isOwner = await paperRepo.IsTeacherOwnerAsync(question.PaperId, currentUser.UserId, ct);
            if (!isOwner) throw new ForbiddenException("You can only update options in your own papers.");
        }

        option.OptionText     = request.OptionText;
        option.OptionImageUrl = request.OptionImageUrl;
        option.IsCorrect      = request.IsCorrect;

        questionRepo.UpdateOption(option);
        await questionRepo.SaveChangesAsync(ct);
    }
}
