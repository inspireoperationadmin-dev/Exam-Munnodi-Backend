namespace ScholarFlow.Modules.Examination.DTOs;

public sealed record SessionReviewItemDto(
    int OrderIndex,
    Guid QuestionId,
    string QuestionText,
    string? QuestionImageUrl,
    Guid? SelectedOptionId,
    Guid? CorrectOptionId,
    bool IsCorrect,
    decimal MarksAwarded,
    string ResponseStatus,
    List<ReviewOptionDto> Options,
    ReviewExplanationDto? Explanation);
