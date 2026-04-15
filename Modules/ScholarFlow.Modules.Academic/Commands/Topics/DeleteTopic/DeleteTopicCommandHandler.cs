using MediatR;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Topics.DeleteTopic;

public sealed class DeleteTopicCommandHandler(ITopicRepository repo)
    : IRequestHandler<DeleteTopicCommand>
{
    public async Task Handle(DeleteTopicCommand request, CancellationToken ct)
    {
        var topic = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Topic not found.");

        repo.Delete(topic);
        await repo.SaveChangesAsync(ct);
    }
}
