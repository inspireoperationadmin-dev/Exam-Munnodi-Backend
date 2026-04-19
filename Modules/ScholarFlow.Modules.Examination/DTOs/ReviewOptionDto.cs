namespace ScholarFlow.Modules.Examination.DTOs;

public sealed record ReviewOptionDto(
    Guid Id,
    string Label,
    string OptionText,
    string? OptionImageUrl,
    bool IsCorrect);
