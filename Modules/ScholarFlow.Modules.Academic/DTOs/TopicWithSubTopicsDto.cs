namespace ScholarFlow.Modules.Academic.DTOs;

public sealed record TopicWithSubTopicsDto(
    Guid Id,
    int OrderIndex,
    IReadOnlyList<SubTopicDto> SubTopics,
    string NameEnglish,
    string? NameTamil = null,
    string? NameSinhala = null);
