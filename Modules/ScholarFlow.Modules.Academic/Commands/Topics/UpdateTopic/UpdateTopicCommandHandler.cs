using MediatR;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Topics.UpdateTopic;

public sealed class UpdateTopicCommandHandler(ITopicRepository repo)
    : IRequestHandler<UpdateTopicCommand>
{
    public async Task Handle(UpdateTopicCommand request, CancellationToken ct)
    {
        var topic = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Topic not found.");

        topic.Update(
            request.NameEnglish.Trim(),
            request.OrderIndex,
            NormalizeOptional(request.NameTamil),
            NormalizeOptional(request.NameSinhala));
        repo.Update(topic);
        await repo.SaveChangesAsync(ct);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
