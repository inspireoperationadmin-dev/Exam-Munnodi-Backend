namespace ScholarFlow.Modules.Academic.DTOs;

public sealed record SubjectSummaryDto(Guid Id, string Name, string? Description, int TopicCount);
