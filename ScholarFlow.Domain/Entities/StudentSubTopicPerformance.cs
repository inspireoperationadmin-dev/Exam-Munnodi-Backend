namespace ScholarFlow.Domain.Entities;

/// <summary>
/// Cached performance per student per sub-topic.
/// Upserted from TopicExam results only.
/// </summary>
public class StudentSubTopicPerformance
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SubTopicId { get; set; }
    public Guid TopicId { get; set; }       // denormalized for fast queries
    public Guid SubjectId { get; set; }     // denormalized for fast queries
    public int TotalQuestionsInSubTopic { get; set; }
    public int UniqueQuestionsAttempted { get; set; }
    public int MasteredQuestions { get; set; }
    public int TotalAttempts { get; set; }
    public int CorrectCount { get; set; }
    public decimal CoveragePercentage { get; set; }
    public decimal MasteryPercentage { get; set; }
    public decimal CorrectPercentage { get; set; }
    public decimal HealthPercentage { get; set; }
    public DateTime LastUpdated { get; set; }

    public SubTopic SubTopic { get; set; } = null!;
}
