namespace ScholarFlow.Domain.Entities;

/// <summary>
/// Subjects a student has selected to study / practice.
/// Used to personalise the dashboard and exam generation.
/// </summary>
public class StudentSubjectSelection
{
    public Guid Id { get; set; }
    public Guid StudentProfileId { get; set; }
    public Guid SubjectId { get; set; }
    public DateTime AddedAt { get; set; }

    public StudentProfile StudentProfile { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
}
