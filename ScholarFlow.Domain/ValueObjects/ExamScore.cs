namespace ScholarFlow.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing the result of a completed exam.
/// Percentage = (ObtainedMarks / TotalMarks) * 100
/// </summary>
public sealed record ExamScore
{
    public decimal Percentage { get; }
    public decimal ObtainedMarks { get; }
    public decimal TotalMarks { get; }

    public ExamScore(decimal percentage, decimal obtainedMarks, decimal totalMarks)
    {
        Percentage = Math.Round(percentage, 2);
        ObtainedMarks = Math.Round(obtainedMarks, 2);
        TotalMarks = totalMarks;
    }

    public static ExamScore Calculate(decimal obtainedMarks, decimal totalMarks)
    {
        var percentage = totalMarks > 0
            ? Math.Round(obtainedMarks / totalMarks * 100, 2)
            : 0m;
        return new ExamScore(percentage, obtainedMarks, totalMarks);
    }

    public bool IsPassing(decimal passMark = 40m) => Percentage >= passMark;
}
