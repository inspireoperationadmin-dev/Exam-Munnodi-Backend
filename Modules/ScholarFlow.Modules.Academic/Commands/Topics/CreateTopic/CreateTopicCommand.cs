using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Topics.CreateTopic;

public sealed record CreateTopicCommand(
    Guid SubjectId,
    string NameEnglish,
    int OrderIndex,
    List<CreateSubTopicItem> SubTopics,
    string? NameTamil = null,
    string? NameSinhala = null) : IRequest<Guid>;

public sealed record CreateSubTopicItem(
    string NameEnglish,
    int OrderIndex,
    string? NameTamil = null,
    string? NameSinhala = null);
