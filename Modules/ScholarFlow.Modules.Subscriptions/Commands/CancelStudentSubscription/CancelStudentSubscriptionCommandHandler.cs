using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.Subscriptions.Commands.CancelStudentSubscription;

public sealed class CancelStudentSubscriptionCommandHandler(
    ISubscriptionRepository subscriptionRepo,
    ICurrentUser currentUser)
    : IRequestHandler<CancelStudentSubscriptionCommand>
{
    public async Task Handle(CancelStudentSubscriptionCommand request, CancellationToken ct)
    {
        var subscription = await subscriptionRepo.GetSubscriptionAsync(request.SubscriptionId, ct)
            ?? throw new NotFoundException("Subscription period not found.");

        if (subscription.UserId != request.UserId)
            throw new NotFoundException("Subscription period not found for this student.");

        if (subscription.Status != ScholarFlow.Domain.Enums.SubscriptionStatus.Active
            || subscription.EndsAt <= DateTime.UtcNow)
            throw new ConflictException("This subscription period is no longer active or scheduled.");

        if (subscription.ActivationSource
            != ScholarFlow.Domain.Enums.SubscriptionActivationSource.AdminManual)
            throw new ConflictException("Launch-offer access cannot be cancelled as a paid subscription.");

        subscription.Cancel(currentUser.UserId, request.AdminNote);

        await subscriptionRepo.SaveChangesAsync(ct);
    }
}
