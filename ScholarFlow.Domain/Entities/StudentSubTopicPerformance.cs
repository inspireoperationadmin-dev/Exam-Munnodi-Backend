namespace ScholarFlow.Domain.Entities;

/// <summary>
/// Cached performance per student per sub-topic.
/// Upserted after every exam via Analytics module event handler.
/// Used for weak-area detection and personalized exam generation.
/// </summary>
public class StudentSubTopicPerformance
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SubTopicId { get; set; }
    public Guid TopicId { get; set; }       // denormalized for fast queries
    public Guid SubjectId { get; set; }     // denormalized for fast queries
    public int TotalAttempts { get; set; }
    public int CorrectCount { get; set; }
    public decimal CorrectPercentage { get; set; }
    public DateTime LastUpdated { get; set; }

    public SubTopic SubTopic { get; set; } = null!;
}
