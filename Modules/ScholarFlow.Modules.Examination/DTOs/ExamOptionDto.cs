namespace ScholarFlow.Modules.Examination.DTOs;

public sealed record ExamOptionDto(
    Guid Id,
    string Label,
    string OptionText,
    string? OptionImageUrl);
