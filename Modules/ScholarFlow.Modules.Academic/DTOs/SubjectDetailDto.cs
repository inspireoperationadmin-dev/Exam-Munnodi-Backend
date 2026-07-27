namespace ScholarFlow.Modules.Academic.DTOs;

public sealed record SubjectDetailDto(
    Guid Id,
    IReadOnlyList<StreamDto> Streams,
    int TopicCount,
    string NameEnglish,
    string? NameTamil = null,
    string? NameSinhala = null,
    string? Description = null);
