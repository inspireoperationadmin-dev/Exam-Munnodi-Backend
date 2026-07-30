namespace ScholarFlow.Modules.Examination.DTOs;

public sealed record ResumeAnswerDto(
    Guid QuestionId,
    Guid? SelectedOptionId,
    int TimeSpentSeconds,
    string ResponseStatus);
