namespace ScholarFlow.Domain.Entities;

/// <summary>
/// Subject-level performance summary for a student's dashboard.
/// Upserted from MockExam results only. Tracks streak and rolling average score.
/// </summary>
public class StudentSubjectPerformance
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SubjectId { get; set; }
    public int TotalExams { get; set; }
    public decimal AverageExamScore { get; set; }
    public decimal BestScore { get; set; }
    public int TotalQuestionsAttempted { get; set; }
    public decimal OverallCorrectPercentage { get; set; }
    public int StudyStreakDays { get; set; }
    public DateTime? LastStudiedAt { get; set; }
    public DateTime LastUpdated { get; set; }

    public Subject Subject { get; set; } = null!;
}
