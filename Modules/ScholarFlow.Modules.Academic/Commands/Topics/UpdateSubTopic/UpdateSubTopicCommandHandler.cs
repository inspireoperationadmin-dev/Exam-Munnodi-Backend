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

        subTopic.Update(
            request.NameEnglish.Trim(),
            request.OrderIndex,
            NormalizeOptional(request.NameTamil),
            NormalizeOptional(request.NameSinhala));
        repo.UpdateSubTopic(subTopic);
        await repo.SaveChangesAsync(ct);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
