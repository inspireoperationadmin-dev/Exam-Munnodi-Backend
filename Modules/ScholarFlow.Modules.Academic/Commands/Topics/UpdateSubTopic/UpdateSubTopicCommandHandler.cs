using MediatR;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Topics.UpdateSubTopic;

public sealed class UpdateSubTopicCommandHandler(ITopicRepository repo)
    : IRequestHandler<UpdateSubTopicCommand>
{
    public async Task Handle(UpdateSubTopicCommand request, CancellationToken ct)
    {
        var subTopic = await repo.GetSubTopicByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("SubTopic not found.");

        subTopic.Update(request.Name, request.OrderIndex);
        repo.UpdateSubTopic(subTopic);
        await repo.SaveChangesAsync(ct);
    }
}
