using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Topics.UpdateTopic;

public sealed record UpdateTopicCommand(Guid Id, string TopicName, int OrderIndex) : IRequest;
