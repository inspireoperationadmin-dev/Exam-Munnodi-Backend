namespace ScholarFlow.Modules.Academic.DTOs;

public sealed record ExplanationSectionDto(
    Guid   Id,
    string Title,
    string Content,
    int    OrderIndex);

public sealed record ExplanationDto(
    Guid                              Id,
    Guid                              QuestionId,
    string                            Type,
    string?                           VideoUrl,
    IReadOnlyList<ExplanationSectionDto> Sections);
