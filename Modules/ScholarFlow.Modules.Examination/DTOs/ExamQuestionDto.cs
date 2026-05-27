namespace ScholarFlow.Modules.Examination.DTOs;

public sealed record ExamQuestionDto(
    Guid Id,
    int OrderIndex,
    string QuestionText,
    string? QuestionImageUrl,
    decimal Marks,
    List<ExamOptionDto> Options,
    Guid? CorrectOptionId = null,     // <-- Added for Practice Mode local check [1]
    string? ExplanationText = null);  // <-- Added for Practice Mode local check [1]