using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Domain.Entities;

/// <summary>
/// Per-question progress for topic exams only.
/// Keeps topic/unit analytics separate from mock-exam subject analytics.
/// </summary>
public class StudentTopicQuestionProgress
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid TopicId { get; set; }
    public Guid SubTopicId { get; set; }
    public int TimesAttempted { get; set; }
    public int CorrectCount { get; set; }
    public bool LastAnswerCorrect { get; set; }
    public QuestionProgressStatus Status { get; set; }
    public DateTime LastSeenAt { get; set; }

    public Question Question { get; set; } = null!;
    public SubTopic SubTopic { get; set; } = null!;
}
