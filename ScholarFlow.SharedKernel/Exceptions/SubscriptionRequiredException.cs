namespace ScholarFlow.SharedKernel.Exceptions;

public sealed class SubscriptionRequiredException(string message = "Active subscription is required.")
    : AppException("SUBSCRIPTION_REQUIRED", message, 403);
