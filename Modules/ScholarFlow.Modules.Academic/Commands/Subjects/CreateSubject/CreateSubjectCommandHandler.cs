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
        if (await repo.ExistsByNameAsync(request.Name, ct))
            throw new ConflictException($"A subject named '{request.Name}' already exists.");

        var subject = Subject.Create(request.Name, request.Description);

        await repo.AddAsync(subject, ct);

        foreach (var streamId in request.StreamIds)
            await repo.AddSubjectStreamAsync(new SubjectStream { SubjectId = subject.Id, StreamId = streamId }, ct);

        await repo.SaveChangesAsync(ct);

        return subject.Id;
    }
}
