using MediatR;

namespace ScholarFlow.Modules.Subscriptions.Commands.CancelStudentSubscription;

public sealed record CancelStudentSubscriptionCommand(
    Guid UserId,
    Guid SubscriptionId,
    string? AdminNote) : IRequest;
