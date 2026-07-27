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

        var nameEnglish = request.NameEnglish.Trim();

        var nameConflict = await repo.ExistsByNameAsync(nameEnglish, ct);
        if (nameConflict && stream.NameEnglish != nameEnglish)
            throw new ConflictException($"A stream named '{nameEnglish}' already exists.");

        stream.Update(
            nameEnglish,
            request.Description,
            NormalizeOptional(request.NameTamil),
            NormalizeOptional(request.NameSinhala));
        repo.Update(stream);
        await repo.SaveChangesAsync(ct);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
