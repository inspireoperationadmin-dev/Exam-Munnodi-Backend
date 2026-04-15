using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Topics.CreateSubTopic;

public sealed record CreateSubTopicCommand(Guid TopicId, string Name, int OrderIndex) : IRequest<Guid>;
