namespace ScholarFlow.Modules.Academic.DTOs;

public sealed record QuestionWithOptionsDto(
    Guid Id,
    int OrderIndex,
    string QuestionText,
    string? QuestionImageUrl,
    Guid SubTopicId,
    string SubTopicName,
    bool HasExplanation,
    decimal Marks,
    IReadOnlyList<OptionDto> Options);

public sealed record OptionDto(
    Guid Id,
    string Label,
    string OptionText,
    string? OptionImageUrl,
    bool IsCorrect);
