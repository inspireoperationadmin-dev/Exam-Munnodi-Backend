namespace ScholarFlow.Modules.Academic.DTOs;

public sealed record SubjectDetailDto(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<StreamDto> Streams,
    int TopicCount);
