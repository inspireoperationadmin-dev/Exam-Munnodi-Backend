namespace ScholarFlow.Domain.Enums;

public enum SubscriptionPlanCode
{
    // Legacy values remain readable until existing rows are retired.
    LaunchFree = 1,
    Trial = 2,
    Monthly = 3,
    Quarterly = 4,
    SixMonths = 5,

    Free = 10,
    BasicMonthly = 20,
    BasicAnnual = 21,
    ProMonthly = 30,
    ProAnnual = 31
}
