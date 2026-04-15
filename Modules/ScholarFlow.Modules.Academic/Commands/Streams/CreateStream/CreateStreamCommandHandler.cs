using MediatR;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Streams.CreateStream;

public sealed class CreateStreamCommandHandler(IStreamRepository repo)
    : IRequestHandler<CreateStreamCommand, Guid>
{
    public async Task<Guid> Handle(CreateStreamCommand request, CancellationToken ct)
    {
        if (await repo.ExistsByNameAsync(request.Name, ct))
            throw new ConflictException($"A stream named '{request.Name}' already exists.");

        var stream = AcademicStream.Create(request.Name, request.Description);

        await repo.AddAsync(stream, ct);
        await repo.SaveChangesAsync(ct);

        return stream.Id;
    }
}
