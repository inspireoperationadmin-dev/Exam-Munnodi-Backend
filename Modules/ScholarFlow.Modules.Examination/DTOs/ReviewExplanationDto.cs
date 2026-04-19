namespace ScholarFlow.Modules.Examination.DTOs;

public sealed record ReviewExplanationSectionDto(
    string Title,
    string Content,
    int OrderIndex);

public sealed record ReviewExplanationDto(
    string Type,
    string? VideoUrl,
    List<ReviewExplanationSectionDto> Sections);
