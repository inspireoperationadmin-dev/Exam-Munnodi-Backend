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

        var nameEnglish = request.NameEnglish.Trim();

        var nameConflict = await repo.ExistsByNameAsync(nameEnglish, ct);
        if (nameConflict && subject.NameEnglish != nameEnglish)
            throw new ConflictException($"A subject named '{nameEnglish}' already exists.");

        subject.Update(
            nameEnglish,
            request.Description,
            NormalizeOptional(request.NameTamil),
            NormalizeOptional(request.NameSinhala));
        repo.Update(subject);
        await repo.SaveChangesAsync(ct);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
