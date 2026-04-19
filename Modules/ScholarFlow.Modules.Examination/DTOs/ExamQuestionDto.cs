namespace ScholarFlow.Modules.Examination.DTOs;

public sealed record ExamQuestionDto(
    Guid Id,
    int OrderIndex,
    string QuestionText,
    string? QuestionImageUrl,
    List<ExamOptionDto> Options);
