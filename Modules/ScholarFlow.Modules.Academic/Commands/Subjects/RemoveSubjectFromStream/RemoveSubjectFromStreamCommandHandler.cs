using MediatR;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Subjects.RemoveSubjectFromStream;

public sealed class RemoveSubjectFromStreamCommandHandler(ISubjectRepository repo)
    : IRequestHandler<RemoveSubjectFromStreamCommand>
{
    public async Task Handle(RemoveSubjectFromStreamCommand request, CancellationToken ct)
    {
        var link = await repo.GetSubjectStreamAsync(request.SubjectId, request.StreamId, ct)
            ?? throw new NotFoundException("Subject is not assigned to this stream.");

        repo.RemoveSubjectStream(link);
        await repo.SaveChangesAsync(ct);
    }
}
