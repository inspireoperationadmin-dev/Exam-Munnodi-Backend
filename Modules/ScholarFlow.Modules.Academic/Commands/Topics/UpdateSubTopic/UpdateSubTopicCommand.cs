using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Topics.UpdateSubTopic;

public sealed record UpdateSubTopicCommand(
    Guid Id,
    string NameEnglish,
    int OrderIndex,
    string? NameTamil = null,
    string? NameSinhala = null) : IRequest;
