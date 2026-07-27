using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Topics.CreateSubTopic;

public sealed record CreateSubTopicCommand(
    Guid TopicId,
    string NameEnglish,
    int OrderIndex,
    string? NameTamil = null,
    string? NameSinhala = null) : IRequest<Guid>;
