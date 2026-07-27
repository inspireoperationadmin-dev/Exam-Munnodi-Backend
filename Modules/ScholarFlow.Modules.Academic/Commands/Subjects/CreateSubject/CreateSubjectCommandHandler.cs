using MediatR;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Subjects.CreateSubject;

public sealed class CreateSubjectCommandHandler(ISubjectRepository repo)
    : IRequestHandler<CreateSubjectCommand, Guid>
{
    public async Task<Guid> Handle(CreateSubjectCommand request, CancellationToken ct)
    {
        var nameEnglish = request.NameEnglish.Trim();

        if (await repo.ExistsByNameAsync(nameEnglish, ct))
            throw new ConflictException($"A subject named '{nameEnglish}' already exists.");

        var subject = Subject.Create(
            nameEnglish,
            request.Description,
            NormalizeOptional(request.NameTamil),
            NormalizeOptional(request.NameSinhala));

        await repo.AddAsync(subject, ct);

        foreach (var streamId in request.StreamIds)
            await repo.AddSubjectStreamAsync(new SubjectStream { SubjectId = subject.Id, StreamId = streamId }, ct);

        await repo.SaveChangesAsync(ct);

        return subject.Id;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
