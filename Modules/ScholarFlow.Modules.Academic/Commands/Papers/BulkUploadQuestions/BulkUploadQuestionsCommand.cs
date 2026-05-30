using MediatR;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Modules.Academic.Commands.Papers.BulkUploadQuestions;

public sealed record BulkQuestionItem(
    Guid SubTopicId,
    string QuestionText,
    string? QuestionImageUrl,
    int OrderIndex,
    DifficultyLevel? ManualDifficulty,
    decimal Marks,
    List<BulkOptionItem> Options);

public sealed record BulkOptionItem(
    string Label,
    string? OptionText,
    string? OptionImageUrl,
    bool IsCorrect);

public sealed record BulkUploadQuestionsCommand(
    Guid PaperId,
    List<BulkQuestionItem> Questions) : IRequest<bool>;