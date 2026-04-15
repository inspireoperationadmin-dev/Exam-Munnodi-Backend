using MediatR;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Topics.DeleteSubTopic;

public sealed class DeleteSubTopicCommandHandler(ITopicRepository repo)
    : IRequestHandler<DeleteSubTopicCommand>
{
    public async Task Handle(DeleteSubTopicCommand request, CancellationToken ct)
    {
        var subTopic = await repo.GetSubTopicByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("SubTopic not found.");

        repo.DeleteSubTopic(subTopic);
        await repo.SaveChangesAsync(ct);
    }
}
