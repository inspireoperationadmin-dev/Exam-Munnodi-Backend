namespace ScholarFlow.Modules.Subscriptions.DTOs;

public sealed record LaunchOfferStatusDto(
    bool IsEnabled,
    bool IsEligible,
    bool IsClaimed,
    string Status,
    string PromotionCode,
    int DurationDays,
    DateTime? ClaimStartsAtUtc,
    DateTime? ClaimEndsAtUtc,
    DateTime? ClaimedAt,
    DateTime? AccessEndsAt);
