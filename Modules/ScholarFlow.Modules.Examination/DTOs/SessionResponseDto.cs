namespace ScholarFlow.Modules.Examination.DTOs;

public sealed record SessionResponseDto(
    Guid QuestionId,
    int OrderIndex,
    Guid? SelectedOptionId,
    bool IsCorrect,
    decimal MarksAwarded,
    string ResponseStatus);
