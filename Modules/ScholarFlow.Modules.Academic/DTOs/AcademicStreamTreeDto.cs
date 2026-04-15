namespace ScholarFlow.Modules.Academic.DTOs;

public sealed record AcademicStreamTreeDto(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<SubjectTreeDto> Subjects);

public sealed record SubjectTreeDto(
    Guid Id,
    string Name,
    IReadOnlyList<TopicWithSubTopicsDto> Topics);
