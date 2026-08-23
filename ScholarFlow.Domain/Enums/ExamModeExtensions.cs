namespace ScholarFlow.Domain.Enums;

public static class ExamModeExtensions
{
    public static bool IsPaperMode(this ExamMode mode)
        => mode is ExamMode.PaperPractice or ExamMode.PaperExam;

    public static bool IsTopicMode(this ExamMode mode)
        => mode is ExamMode.TopicPractice or ExamMode.TopicExam;

    public static bool IsPracticeMode(this ExamMode mode)
        => mode is ExamMode.PaperPractice or ExamMode.TopicPractice;

    public static bool IsTimedMode(this ExamMode mode)
        => !mode.IsPracticeMode();
}
