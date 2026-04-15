using MediatR;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Papers.AddExplanation;

public sealed class AddExplanationCommandHandler(
    IQuestionRepository questionRepo,
    IExplanationRepository explanationRepo,
    IPaperRepository paperRepo,
    ICurrentUser currentUser)
    : IRequestHandler<AddExplanationCommand, Guid>
{
    public async Task<Guid> Handle(AddExplanationCommand request, CancellationToken ct)
    {
        var question = await questionRepo.GetByIdAsync(request.QuestionId, ct)
            ?? throw new NotFoundException("Question not found.");

        if (currentUser.IsInRole(AppRole.Teacher))
        {
            var isOwner = await paperRepo.IsTeacherOwnerAsync(question.PaperId, currentUser.UserId, ct);
            if (!isOwner) throw new ForbiddenException("You can only add explanations to your own questions.");
        }

        var existing = await explanationRepo.GetByQuestionIdAsync(request.QuestionId, ct);
        if (existing is not null)
            throw new ConflictException("This question already has an explanation.");

        var explanation = new Explanation
        {
            Id         = Guid.NewGuid(),
            QuestionId = request.QuestionId,
            Type       = request.Type,
            VideoUrl   = request.VideoUrl
        };

        await explanationRepo.AddAsync(explanation, ct);
        await explanationRepo.SaveChangesAsync(ct);

        foreach (var item in request.Sections)
        {
            var section = new ExplanationSection
            {
                Id            = Guid.NewGuid(),
                ExplanationId = explanation.Id,
                Title         = item.Title,
                Content       = item.Content,
                OrderIndex    = item.OrderIndex
            };
            await explanationRepo.AddSectionAsync(section, ct);
        }

        await explanationRepo.SaveChangesAsync(ct);

        return explanation.Id;
    }
}
