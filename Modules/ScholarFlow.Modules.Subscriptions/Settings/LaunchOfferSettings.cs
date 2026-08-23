namespace ScholarFlow.Modules.Subscriptions.Settings;

public sealed class LaunchOfferSettings
{
    public const string SectionName = "LaunchOffer";

    public bool Enabled { get; init; }
    public string PromotionCode { get; init; } = "INITIAL_LAUNCH_BASIC_30";
    public int DurationDays { get; init; } = 30;
    public DateTime? ClaimStartsAtUtc { get; init; }
    public DateTime? ClaimEndsAtUtc { get; init; }

    public bool IsClaimWindowOpen(DateTime utcNow)
        => Enabled
        && (!ClaimStartsAtUtc.HasValue || ClaimStartsAtUtc.Value <= utcNow)
        && (!ClaimEndsAtUtc.HasValue || ClaimEndsAtUtc.Value > utcNow);
}
