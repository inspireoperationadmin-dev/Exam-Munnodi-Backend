namespace ScholarFlow.Domain.Entities;

/// <summary>
/// Tracks how many times a student has seen/attempted each question.
/// Updated from MockExam results only. Used by the personalized exam
/// generator to avoid repetition and prioritize pending questions.
/// </summary>
public class StudentQuestionHistory
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid QuestionId { get; set; }
    public int TimesAttempted { get; set; }
    public int CorrectCount { get; set; }
    public bool LastAnswerCorrect { get; set; }
    public DateTime LastSeenAt { get; set; }

    public Question Question { get; set; } = null!;
}
