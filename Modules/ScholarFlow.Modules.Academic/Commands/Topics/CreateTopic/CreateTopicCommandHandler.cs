using MediatR;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Interfaces.Repositories;

namespace ScholarFlow.Modules.Academic.Commands.Topics.CreateTopic;

public sealed class CreateTopicCommandHandler(ITopicRepository repo)
    : IRequestHandler<CreateTopicCommand, Guid>
{
    public async Task<Guid> Handle(CreateTopicCommand request, CancellationToken ct)
    {
        var topic = Topic.Create(
            request.SubjectId,
            request.NameEnglish.Trim(),
            request.OrderIndex,
            NormalizeOptional(request.NameTamil),
            NormalizeOptional(request.NameSinhala));

        await repo.AddAsync(topic, ct);

        foreach (var item in request.SubTopics)
        {
            var subTopic = SubTopic.Create(
                topic.Id,
                item.NameEnglish.Trim(),
                item.OrderIndex,
                NormalizeOptional(item.NameTamil),
                NormalizeOptional(item.NameSinhala));
            await repo.AddSubTopicAsync(subTopic, ct);
        }

        await repo.SaveChangesAsync(ct);

        return topic.Id;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
