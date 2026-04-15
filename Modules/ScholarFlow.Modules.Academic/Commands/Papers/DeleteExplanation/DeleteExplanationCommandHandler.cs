using MediatR;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Papers.DeleteExplanation;

public sealed class DeleteExplanationCommandHandler(IExplanationRepository repo)
    : IRequestHandler<DeleteExplanationCommand>
{
    public async Task Handle(DeleteExplanationCommand request, CancellationToken ct)
    {
        var explanation = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Explanation not found.");

        repo.Delete(explanation);
        await repo.SaveChangesAsync(ct);
    }
}
