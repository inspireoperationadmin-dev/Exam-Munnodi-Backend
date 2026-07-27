namespace ScholarFlow.Modules.Academic.DTOs;

public sealed record AcademicStreamTreeDto(
    Guid Id,
    IReadOnlyList<SubjectTreeDto> Subjects,
    string NameEnglish,
    string? NameTamil = null,
    string? NameSinhala = null,
    string? Description = null);

public sealed record SubjectTreeDto(
    Guid Id,
    IReadOnlyList<TopicWithSubTopicsDto> Topics,
    string NameEnglish,
    string? NameTamil = null,
    string? NameSinhala = null);
