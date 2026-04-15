using MediatR;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Streams.DeleteStream;

public sealed class DeleteStreamCommandHandler(IStreamRepository repo)
    : IRequestHandler<DeleteStreamCommand>
{
    public async Task Handle(DeleteStreamCommand request, CancellationToken ct)
    {
        var stream = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Stream not found.");

        repo.Delete(stream);
        await repo.SaveChangesAsync(ct);
    }
}
