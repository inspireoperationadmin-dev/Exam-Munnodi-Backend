using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Topics.DeleteTopic;

public sealed record DeleteTopicCommand(Guid Id) : IRequest;
