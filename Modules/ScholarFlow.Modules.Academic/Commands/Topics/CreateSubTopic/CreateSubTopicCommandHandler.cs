using MediatR;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Topics.CreateSubTopic;

public sealed class CreateSubTopicCommandHandler(ITopicRepository repo)
    : IRequestHandler<CreateSubTopicCommand, Guid>
{
    public async Task<Guid> Handle(CreateSubTopicCommand request, CancellationToken ct)
    {
        var topic = await repo.GetByIdAsync(request.TopicId, ct)
            ?? throw new NotFoundException("Topic not found.");

        var subTopic = SubTopic.Create(topic.Id, request.Name, request.OrderIndex);

        await repo.AddSubTopicAsync(subTopic, ct);
        await repo.SaveChangesAsync(ct);

        return subTopic.Id;
    }
}
