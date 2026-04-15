using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Domain.Entities;

/// <summary>
/// Student's response to one question within an exam session.
/// Award() is called by ExamSession.Complete() — do not call directly.
/// </summary>
public class UserResponse
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid? SelectedOptionId { get; private set; }
    public bool IsCorrect { get; private set; }
    public decimal MarksAwarded { get; private set; }
    public ResponseStatus ResponseStatus { get; set; }
    public int TimeSpentSeconds { get; set; }

    public ExamSession Session { get; set; } = null!;
    public Question Question { get; set; } = null!;
    public Option? SelectedOption { get; set; }     // loaded via Include for scoring

    public void SelectOption(Guid optionId)
    {
        SelectedOptionId = optionId;
        ResponseStatus = ResponseStatus.Answered;
    }

    // Called only by ExamSession.Complete()
    internal void Award(decimal marks, bool isCorrect)
    {
        MarksAwarded = marks;
        IsCorrect = isCorrect;
    }
}
