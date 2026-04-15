namespace ScholarFlow.Domain.Entities;

/// <summary>
/// Maps which questions are included in a personalized exam session
/// and in what order they appear.
/// </summary>
public class ExamSessionQuestion
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid QuestionId { get; set; }
    public int OrderIndex { get; set; }

    public ExamSession Session { get; set; } = null!;
    public Question Question { get; set; } = null!;
}
