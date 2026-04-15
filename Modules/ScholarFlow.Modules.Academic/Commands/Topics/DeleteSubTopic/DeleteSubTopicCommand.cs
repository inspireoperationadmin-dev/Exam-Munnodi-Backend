using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Topics.DeleteSubTopic;

public sealed record DeleteSubTopicCommand(Guid Id) : IRequest;
