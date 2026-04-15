using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Topics.CreateTopic;

public sealed record CreateTopicCommand(
    Guid SubjectId,
    string TopicName,
    int OrderIndex,
    List<CreateSubTopicItem> SubTopics) : IRequest<Guid>;

public sealed record CreateSubTopicItem(string Name, int OrderIndex);
