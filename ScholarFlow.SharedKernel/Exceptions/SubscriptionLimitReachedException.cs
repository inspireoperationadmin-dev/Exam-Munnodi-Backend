namespace ScholarFlow.SharedKernel.Exceptions;

public sealed class SubscriptionLimitReachedException(string message)
    : AppException("SUBSCRIPTION_LIMIT_REACHED", message, 403);
