using ScholarFlow.Domain.Entities.Base;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Events;
using ScholarFlow.Domain.Exceptions;
using ScholarFlow.Domain.ValueObjects;

namespace ScholarFlow.Domain.Entities;

/// <summary>
/// Aggregate root for an exam attempt.
/// Owns UserResponses and ExamSessionQuestions.
/// Scoring logic lives here (DDD rich model).
/// </summary>
public class ExamSession : AggregateRoot
{
    private readonly List<UserResponse> _userResponses = new();
    private readonly List<ExamSessionQuestion> _sessionQuestions = new();

    public Guid UserId { get; private set; }
    public Guid? PaperId { get; private set; }       // null for personalized sessions
    public Guid? SubjectId { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public decimal FinalScore { get; private set; }
    public decimal ObtainedMarks { get; private set; }
    public decimal TotalMarks { get; private set; }
    public ExamSessionStatus Status { get; private set; }
    public bool IsPractice { get; private set; }
    public bool IsPersonalized { get; private set; }

    public TimeSpan? Duration => EndTime.HasValue ? EndTime - StartTime : null;

    public ApplicationUser User { get; set; } = null!;
    public Paper? Paper { get; set; }
    public Subject? Subject { get; set; }
    public IReadOnlyList<UserResponse> UserResponses => _userResponses.AsReadOnly();
    public IReadOnlyList<ExamSessionQuestion> SessionQuestions => _sessionQuestions.AsReadOnly();

    private ExamSession() { }

    // ── Factory ──────────────────────────────────────────────────────────────

    public static ExamSession Start(
        Guid userId,
        Guid? paperId,
        Guid? subjectId,
        bool isPractice,
        bool isPersonalized = false)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PaperId = paperId,
            SubjectId = subjectId,
            StartTime = DateTime.UtcNow,
            Status = ExamSessionStatus.InProgress,
            IsPractice = isPractice,
            IsPersonalized = isPersonalized,
            FinalScore = 0
        };

    // ── Domain Methods ────────────────────────────────────────────────────────

    /// <summary>
    /// Score all responses and transition to Completed.
    /// Requires UserResponses loaded with SelectedOption navigation.
    /// </summary>
    public ExamScore Complete(decimal negativeMarkValue)
    {
        if (Status != ExamSessionStatus.InProgress)
            throw new DomainException("Only in-progress sessions can be completed.");

        decimal obtained = 0m;
        decimal total = _userResponses.Count;

        foreach (var response in _userResponses)
        {
            if (response.SelectedOptionId.HasValue)
            {
                if (response.SelectedOption!.IsCorrect)
                {
                    response.Award(1m, isCorrect: true);
                    obtained += 1m;
                }
                else
                {
                    response.Award(-negativeMarkValue, isCorrect: false);
                    obtained -= negativeMarkValue;
                }
            }
            else
            {
                response.Award(0m, isCorrect: false);
            }
        }

        var score = ExamScore.Calculate(Math.Round(obtained, 2), total);

        Status = ExamSessionStatus.Completed;
        EndTime = DateTime.UtcNow;
        FinalScore = score.Percentage;
        ObtainedMarks = score.ObtainedMarks;
        TotalMarks = score.TotalMarks;

        Raise(new ExamSessionCompletedDomainEvent(
            Guid.NewGuid(), DateTime.UtcNow,
            Id, UserId, SubjectId, PaperId, score, IsPractice));

        return score;
    }

    public void Abandon()
    {
        if (Status != ExamSessionStatus.InProgress)
            throw new DomainException("Only in-progress sessions can be abandoned.");

        Status = ExamSessionStatus.Abandoned;
        EndTime = DateTime.UtcNow;
    }
}
