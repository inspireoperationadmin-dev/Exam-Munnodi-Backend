using MediatR;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Subjects.DeleteSubject;

public sealed class DeleteSubjectCommandHandler(ISubjectRepository repo)
    : IRequestHandler<DeleteSubjectCommand>
{
    public async Task Handle(DeleteSubjectCommand request, CancellationToken ct)
    {
        var subject = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Subject not found.");

        repo.Delete(subject);
        await repo.SaveChangesAsync(ct);
    }
}
