namespace ScholarFlow.Domain.Entities;

/// <summary>
/// One of the 5 answer options for a Question.
/// Label = "1","2","3","4","5" (Sri Lanka A/L MCQ format).
/// Only one option per question has IsCorrect = true.
/// </summary>
public class Option
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public string Label { get; set; } = string.Empty;      // "1","2","3","4","5"
    public string OptionText { get; set; } = string.Empty;
    public string? OptionImageUrl { get; set; }
    public bool IsCorrect { get; set; }

    public Question Question { get; set; } = null!;
}
