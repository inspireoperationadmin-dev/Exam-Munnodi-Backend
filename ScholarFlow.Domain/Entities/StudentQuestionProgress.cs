using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Domain.Entities;

/// <summary>
/// Permanent per-student, per-question progress source of truth.
/// Topic and subject mastery summaries are derived from this table.
/// </summary>
public class StudentQuestionProgress
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
    public bool LastResponseWasAnswered { get; set; }
    public int ConsecutiveCorrect { get; set; }
    public int ConsecutiveWrong { get; set; }
    public decimal MasteryScore { get; set; }
    public QuestionProgressStatus Status { get; set; }
    public DateTime LastSeenAt { get; set; }
    public ExamMode LastAttemptMode { get; set; }

    public Question Question { get; set; } = null!;
    public SubTopic SubTopic { get; set; } = null!;
}
