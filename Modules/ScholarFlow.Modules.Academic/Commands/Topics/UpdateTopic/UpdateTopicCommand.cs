using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Topics.UpdateTopic;

public sealed record UpdateTopicCommand(
    Guid Id,
    string NameEnglish,
    int OrderIndex,
    string? NameTamil = null,
    string? NameSinhala = null) : IRequest;
