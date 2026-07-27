namespace ScholarFlow.Modules.Academic.DTOs;

public sealed record StreamDto(
    Guid Id,
    string NameEnglish,
    string? NameTamil = null,
    string? NameSinhala = null,
    string? Description = null);
