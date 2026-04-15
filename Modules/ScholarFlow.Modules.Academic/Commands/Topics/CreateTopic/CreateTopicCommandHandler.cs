using MediatR;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Interfaces.Repositories;

namespace ScholarFlow.Modules.Academic.Commands.Topics.CreateTopic;

public sealed class CreateTopicCommandHandler(ITopicRepository repo)
    : IRequestHandler<CreateTopicCommand, Guid>
{
    public async Task<Guid> Handle(CreateTopicCommand request, CancellationToken ct)
    {
        var topic = Topic.Create(request.SubjectId, request.TopicName, request.OrderIndex);

        await repo.AddAsync(topic, ct);

        foreach (var item in request.SubTopics)
        {
            var subTopic = SubTopic.Create(topic.Id, item.Name, item.OrderIndex);
            await repo.AddSubTopicAsync(subTopic, ct);
        }

        await repo.SaveChangesAsync(ct);

        return topic.Id;
    }
}
