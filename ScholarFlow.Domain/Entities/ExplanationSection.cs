namespace ScholarFlow.Domain.Entities;

/// <summary>
/// One section of a text explanation — title + content block.
/// Ordered by OrderIndex.
/// </summary>
public class ExplanationSection
{
    public Guid Id { get; set; }
    public Guid ExplanationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int OrderIndex { get; set; }

    public Explanation Explanation { get; set; } = null!;
}
