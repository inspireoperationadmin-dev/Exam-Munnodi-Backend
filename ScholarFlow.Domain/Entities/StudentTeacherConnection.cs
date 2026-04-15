using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Domain.Entities;

/// <summary>
/// Connection request between a student and a teacher.
/// Once Accepted, the student can access the teacher's private papers.
/// </summary>
public class StudentTeacherConnection
{
    public Guid Id { get; set; }
    public Guid StudentProfileId { get; set; }
    public Guid TeacherProfileId { get; set; }
    public ConnectionStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    public StudentProfile StudentProfile { get; set; } = null!;
    public TeacherProfile TeacherProfile { get; set; } = null!;
}
