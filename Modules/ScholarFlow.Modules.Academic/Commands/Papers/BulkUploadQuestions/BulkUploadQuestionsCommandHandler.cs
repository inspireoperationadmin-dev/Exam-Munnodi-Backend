using MediatR;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Papers.BulkUploadQuestions;

public sealed class BulkUploadQuestionsCommandHandler(
    IPaperRepository paperRepo,
    IQuestionRepository questionRepo,
    ICurrentUser currentUser)
    : IRequestHandler<BulkUploadQuestionsCommand, bool>
{
    public async Task<bool> Handle(BulkUploadQuestionsCommand request, CancellationToken ct)
    {
        // 1. Verify paper existence
        var paper = await paperRepo.GetByIdAsync(request.PaperId, ct)
            ?? throw new NotFoundException("Paper not found.");

        // 2. Access control check for Teachers
        if (currentUser.IsInRole(AppRole.Teacher))
        {
            var isOwner = await paperRepo.IsTeacherOwnerAsync(request.PaperId, currentUser.UserId, ct);
            if (!isOwner) throw new ForbiddenException("You can only add questions to your own papers.");
        }

        var questionsList = new List<Question>();
        var optionsList = new List<Option>();

        // 3. Construct the entire entity graph in memory [1, 25]
        foreach (var qItem in request.Questions)
        {
            var question = Question.Create(
                paperId:          paper.Id,
                subTopicId:       qItem.SubTopicId,
                questionText:     qItem.QuestionText,
                orderIndex:       qItem.OrderIndex,
                questionImageUrl: qItem.QuestionImageUrl,
                manualDifficulty: qItem.ManualDifficulty,
                marks:            qItem.Marks);

            questionsList.Add(question);

            foreach (var opt in qItem.Options)
            {
                var option = new Option
                {
                    Id             = Guid.NewGuid(),
                    QuestionId     = question.Id,
                    Label          = opt.Label,
                    OptionText     = opt.OptionText ?? string.Empty,
                    OptionImageUrl = opt.OptionImageUrl,
                    IsCorrect      = opt.IsCorrect
                };
                optionsList.Add(option);
            }
        }

        // 4. Bulk Range Insertion (Reduces Change Tracker scan overhead by up to 80%) [25]
        await questionRepo.AddRangeAsync(questionsList, ct);
        await questionRepo.AddOptionsRangeAsync(optionsList, ct);

        // 5. Commit all changes to Azure SQL in a single transaction [1]
        await questionRepo.SaveChangesAsync(ct);

        return true;
    }
}