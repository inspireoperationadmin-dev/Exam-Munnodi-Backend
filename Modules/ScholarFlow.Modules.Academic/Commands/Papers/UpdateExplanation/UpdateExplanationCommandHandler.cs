using MediatR;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Academic.Commands.Papers.UpdateExplanation;

public sealed class UpdateExplanationCommandHandler(IExplanationRepository repo)
    : IRequestHandler<UpdateExplanationCommand>
{
    public async Task Handle(UpdateExplanationCommand request, CancellationToken ct)
    {
        var explanation = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Explanation not found.");

        explanation.Type     = request.Type;
        explanation.VideoUrl = request.VideoUrl;

        repo.Update(explanation);
        await repo.SaveChangesAsync(ct);
    }
}
