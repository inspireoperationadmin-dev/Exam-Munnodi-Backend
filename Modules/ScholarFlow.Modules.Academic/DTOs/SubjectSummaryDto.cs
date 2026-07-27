namespace ScholarFlow.Modules.Academic.DTOs;

public sealed record SubjectSummaryDto(
    Guid Id,
    int TopicCount,
    string NameEnglish,
    string? NameTamil = null,
    string? NameSinhala = null,
    string? Description = null);
