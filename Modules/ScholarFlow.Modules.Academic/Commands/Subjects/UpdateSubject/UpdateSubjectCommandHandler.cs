using MediatR;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Subjects.UpdateSubject;

public sealed class UpdateSubjectCommandHandler(ISubjectRepository repo)
    : IRequestHandler<UpdateSubjectCommand>
{
    public async Task Handle(UpdateSubjectCommand request, CancellationToken ct)
    {
        var subject = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Subject not found.");

        var nameConflict = await repo.ExistsByNameAsync(request.Name, ct);
        if (nameConflict && subject.Name != request.Name)
            throw new ConflictException($"A subject named '{request.Name}' already exists.");

        subject.Update(request.Name, request.Description);
        repo.Update(subject);
        await repo.SaveChangesAsync(ct);
    }
}
