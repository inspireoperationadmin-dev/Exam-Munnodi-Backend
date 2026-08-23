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
    public Guid? TopicId { get; private set; }
    public Guid? SubjectId { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public DateTime LastActivityAt { get; private set; }
    public int? TimeLimitMinutes { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public decimal FinalScore { get; private set; }
    public decimal ObtainedMarks { get; private set; }
    public decimal TotalMarks { get; private set; }
    public ExamSessionStatus Status { get; private set; }
    public ExamMode Mode { get; private set; }

    public TimeSpan? Duration => EndTime.HasValue ? EndTime - StartTime : null;
    public bool HasExpired(DateTime utcNow) => ExpiresAt.HasValue && utcNow >= ExpiresAt.Value;

    public ApplicationUser User { get; set; } = null!;
    public Paper? Paper { get; set; }
    public Topic? Topic { get; set; }
    public Subject? Subject { get; set; }
    public IReadOnlyList<UserResponse> UserResponses => _userResponses.AsReadOnly();
    public IReadOnlyList<ExamSessionQuestion> SessionQuestions => _sessionQuestions.AsReadOnly();

    private ExamSession() { }

    // ── Factory ──────────────────────────────────────────────────────────────

    public static ExamSession Start(
        Guid userId,
        Guid? paperId,
        Guid? subjectId,
        ExamMode mode,
        int? timeLimitMinutes = null,
        Guid? topicId = null)
    {
        if (timeLimitMinutes.HasValue && timeLimitMinutes.Value <= 0)
            throw new DomainException("Time limit must be greater than 0 minutes.");

        var startedAt = DateTime.UtcNow;

        return new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PaperId = paperId,
            TopicId = topicId,
            SubjectId = subjectId,
            StartTime = startedAt,
            LastActivityAt = startedAt,
            TimeLimitMinutes = timeLimitMinutes,
            ExpiresAt = timeLimitMinutes.HasValue
                ? startedAt.AddMinutes(timeLimitMinutes.Value)
                : null,
            Status = ExamSessionStatus.InProgress,
            Mode = mode,
            FinalScore = 0
        };
    }

    // ── Domain Methods ────────────────────────────────────────────────────────

    /// <summary>
    /// Score all responses using per-question marks and correct options mapping [1].
    /// marksPerQuestion: QuestionId → Marks value [1]
    /// correctOptionPerQuestion: QuestionId → CorrectOptionId [1]
    /// </summary>
    public ExamScore Complete(
        IReadOnlyDictionary<Guid, decimal> marksPerQuestion,
        IReadOnlyDictionary<Guid, Guid> correctOptionPerQuestion,
        ExamSessionStatus finalStatus = ExamSessionStatus.Completed) // <-- Added correct options parameter [1]
    {
        if (Status != ExamSessionStatus.InProgress)
            throw new DomainException("Only in-progress sessions can be completed.");

        if (finalStatus is not ExamSessionStatus.Completed and not ExamSessionStatus.TimedOut)
            throw new DomainException("Invalid final status for completing an exam session.");

        decimal obtained = 0m;
        decimal total    = 0m;

        foreach (var response in _userResponses)
        {
            var marks = marksPerQuestion.GetValueOrDefault(response.QuestionId, 2m);
            total += marks;

            if (response.SelectedOptionId.HasValue)
            {
                var correctOptionId = correctOptionPerQuestion.GetValueOrDefault(response.QuestionId);

                // Safe Guid ID comparison, avoiding EF null navigation properties [1]
                if (response.SelectedOptionId.Value == correctOptionId)
                {
                    response.Award(marks, isCorrect: true);
                    obtained += marks;
                }
                else
                {
                    response.Award(0m, isCorrect: false);
                }
            }
            else
            {
                response.Award(0m, isCorrect: false);
            }
        }

        var score = ExamScore.Calculate(Math.Round(obtained, 2), total);

        var completedAt = finalStatus == ExamSessionStatus.TimedOut && ExpiresAt.HasValue
            ? ExpiresAt.Value
            : DateTime.UtcNow;

        Status        = finalStatus;
        EndTime       = completedAt;
        FinalScore    = score.Percentage;
        ObtainedMarks = score.ObtainedMarks;
        TotalMarks    = score.TotalMarks;

        Raise(new ExamSessionCompletedDomainEvent(
            Guid.NewGuid(), DateTime.UtcNow,
            Id, UserId, SubjectId, PaperId, score, Mode));

        return score;
    }

    public void Abandon()
    {
        if (Status != ExamSessionStatus.InProgress)
            throw new DomainException("Only in-progress sessions can be abandoned.");

        Status = ExamSessionStatus.Abandoned;
        EndTime = DateTime.UtcNow;
    }

    public void Touch(DateTime utcNow)
    {
        if (Status == ExamSessionStatus.InProgress)
        {
            LastActivityAt = utcNow;
        }
    }
}
