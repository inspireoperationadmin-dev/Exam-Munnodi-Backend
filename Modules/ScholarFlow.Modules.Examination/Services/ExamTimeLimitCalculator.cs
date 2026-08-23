namespace ScholarFlow.Modules.Examination.Services;

public static class ExamTimeLimitCalculator
{
    private const int FullPaperMinutes = 120;
    private const int FullPaperQuestionCount = 50;

    public static int CalculateRealPaperPacedMinutes(int questionCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(questionCount, 0);
        return (int)Math.Ceiling(questionCount * (decimal)FullPaperMinutes / FullPaperQuestionCount);
    }
}
