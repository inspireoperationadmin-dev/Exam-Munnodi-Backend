using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Domain.Entities;

/// <summary>
/// Explanation for a question's correct answer.
/// Can be Text-based or Video-based.
/// </summary>
public class Explanation
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public ExplanationType Type { get; set; }
    public string? VideoUrl { get; set; }

    public Question Question { get; set; } = null!;
    public ICollection<ExplanationSection> Sections { get; set; } = new List<ExplanationSection>();
}
