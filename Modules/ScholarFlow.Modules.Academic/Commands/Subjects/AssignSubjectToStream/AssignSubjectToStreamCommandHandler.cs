using MediatR;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Subjects.AssignSubjectToStream;

public sealed class AssignSubjectToStreamCommandHandler(ISubjectRepository repo)
    : IRequestHandler<AssignSubjectToStreamCommand>
{
    public async Task Handle(AssignSubjectToStreamCommand request, CancellationToken ct)
    {
        var subject = await repo.GetByIdAsync(request.SubjectId, ct)
            ?? throw new NotFoundException("Subject not found.");

        var existing = await repo.GetSubjectStreamAsync(request.SubjectId, request.StreamId, ct);
        if (existing is not null)
            throw new ConflictException("Subject is already assigned to this stream.");

        await repo.AddSubjectStreamAsync(
            new SubjectStream { SubjectId = subject.Id, StreamId = request.StreamId }, ct);

        await repo.SaveChangesAsync(ct);
    }
}
