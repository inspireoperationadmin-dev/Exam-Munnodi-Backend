namespace ScholarFlow.Modules.Academic.DTOs;

public sealed record TopicWithSubTopicsDto(
    Guid Id,
    string TopicName,
    int OrderIndex,
    IReadOnlyList<SubTopicDto> SubTopics);
