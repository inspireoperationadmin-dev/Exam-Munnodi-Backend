using MediatR;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Streams.UpdateStream;

public sealed class UpdateStreamCommandHandler(IStreamRepository repo)
    : IRequestHandler<UpdateStreamCommand>
{
    public async Task Handle(UpdateStreamCommand request, CancellationToken ct)
    {
        var stream = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Stream not found.");

        var nameConflict = await repo.ExistsByNameAsync(request.Name, ct);
        if (nameConflict && stream.Name != request.Name)
            throw new ConflictException($"A stream named '{request.Name}' already exists.");

        stream.Update(request.Name, request.Description);
        repo.Update(stream);
        await repo.SaveChangesAsync(ct);
    }
}
