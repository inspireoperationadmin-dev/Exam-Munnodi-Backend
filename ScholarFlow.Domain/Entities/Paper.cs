using ScholarFlow.Domain.Entities.Base;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Domain.Entities;

public class Paper : AuditableAggregateRoot
{
    public Guid? SubjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public PaperType Type { get; private set; }
    public PaperMedium Medium { get; private set; }
    public bool IsPublic { get; private set; }
    public decimal NegativeMarkValue { get; private set; }
    public int TimeLimit { get; private set; }             // ← added, minutes, not nullable
    public ExamSitting? Sitting { get; private set; }
    public string? OfficialPaperCode { get; private set; }
    public Guid? CreatedByTeacherId { get; private set; }

    public Subject? Subject { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<ExamSession> ExamSessions { get; set; } = new List<ExamSession>();

    private Paper() { }

    public static Paper Create(
        Guid? subjectId,
        string title,
        int year,
        PaperType type,
        PaperMedium medium,
        bool isPublic,
        int timeLimit,
        decimal negativeMarkValue  = 0.25m,
        ExamSitting? sitting       = null,
        string? officialPaperCode  = null,
        Guid? createdByTeacherId   = null)
    {
        if (negativeMarkValue < 0 || negativeMarkValue > 1)
            throw new Exceptions.DomainException(
                "Negative mark value must be between 0 and 1.");

        if (timeLimit <= 0)
            throw new Exceptions.DomainException(
                "Time limit must be greater than 0.");

        return new Paper
        {
            Id                = Guid.NewGuid(),
            SubjectId         = subjectId,
            Title             = title,
            Year              = year,
            Type              = type,
            Medium            = medium,
            IsPublic          = isPublic,
            TimeLimit         = timeLimit,
            NegativeMarkValue = negativeMarkValue,
            Sitting           = sitting,
            OfficialPaperCode = officialPaperCode,
            CreatedByTeacherId = createdByTeacherId,
        };
    }

    public void Update(
        Guid? subjectId,
        string title,
        int year,
        PaperType type,
        PaperMedium medium,
        ExamSitting? sitting,
        decimal negativeMarkValue,
        int timeLimit,
        string? officialPaperCode)
    {
        if (negativeMarkValue < 0 || negativeMarkValue > 1)
            throw new Exceptions.DomainException(
                "Negative mark value must be between 0 and 1.");

        if (timeLimit <= 0)
            throw new Exceptions.DomainException(
                "Time limit must be greater than 0.");

        SubjectId         = subjectId;
        Title             = title;
        Year              = year;
        Type              = type;
        Medium            = medium;
        Sitting           = sitting;
        NegativeMarkValue = negativeMarkValue;
        TimeLimit         = timeLimit;
        OfficialPaperCode = officialPaperCode;
    }

    public void UpdateVisibility(bool isPublic) => IsPublic = isPublic;
}