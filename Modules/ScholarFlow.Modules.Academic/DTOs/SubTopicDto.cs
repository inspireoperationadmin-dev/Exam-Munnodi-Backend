namespace ScholarFlow.Modules.Academic.DTOs;

public sealed record SubTopicDto(
    Guid Id,
    int OrderIndex,
    string NameEnglish,
    string? NameTamil = null,
    string? NameSinhala = null);
