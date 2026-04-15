using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Topics.UpdateSubTopic;

public sealed record UpdateSubTopicCommand(Guid Id, string Name, int OrderIndex) : IRequest;
