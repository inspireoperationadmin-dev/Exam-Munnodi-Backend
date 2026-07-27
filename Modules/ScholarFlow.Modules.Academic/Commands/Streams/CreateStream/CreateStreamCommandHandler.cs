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
        var nameEnglish = request.NameEnglish.Trim();

        if (await repo.ExistsByNameAsync(nameEnglish, ct))
            throw new ConflictException($"A stream named '{nameEnglish}' already exists.");

        var stream = AcademicStream.Create(
            nameEnglish,
            request.Description,
            NormalizeOptional(request.NameTamil),
            NormalizeOptional(request.NameSinhala));

        await repo.AddAsync(stream, ct);
        await repo.SaveChangesAsync(ct);

        return stream.Id;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
