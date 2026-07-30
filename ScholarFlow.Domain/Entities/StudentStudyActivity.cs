using ScholarFlow.Domain.Entities.Base;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Domain.Entities;

public class StudentStudyActivity : AuditableEntity
{
    private StudentStudyActivity() { }

    public Guid UserId { get; private set; }
    public DateTime ActivityDate { get; private set; }
    public Guid? SessionId { get; private set; }
    public Guid? SubjectId { get; private set; }
    public ExamMode Mode { get; private set; }
    public int QuestionCount { get; private set; }

    public ApplicationUser User { get; set; } = null!;
    public ExamSession? Session { get; set; }
    public Subject? Subject { get; set; }

    public static StudentStudyActivity Create(
        Guid userId,
        DateTime activityDate,
        Guid? sessionId,
        Guid? subjectId,
        ExamMode mode,
        int questionCount)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActivityDate = activityDate.Date,
            SessionId = sessionId,
            SubjectId = subjectId,
            Mode = mode,
            QuestionCount = Math.Max(0, questionCount)
        };
}
