using ScholarFlow.Domain.Entities.Base;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Domain.Entities;

public class StudentProfile : AggregateRoot
{
    public Guid    UserId           { get; private set; }
    public string  FullName         { get; private set; } = string.Empty;
    public string? PhoneNumber      { get; private set; }
    public Guid?   AcademicStreamId { get; private set; }
    public PaperMedium? Medium      { get; private set; }
    public int?    ExamYear         { get; private set; }
    public DateTime JoinedAt        { get; private set; }

    public ApplicationUser                        User              { get; set; } = null!;
    public AcademicStream?                        AcademicStream    { get; set; }
    public ICollection<StudentSubjectSelection>   SubjectSelections { get; set; } = new List<StudentSubjectSelection>();
    public ICollection<StudentTeacherConnection>  TeacherConnections { get; set; } = new List<StudentTeacherConnection>();

    private StudentProfile() { }

    public static StudentProfile Create(
        Guid        userId,
        string      fullName,
        string?     phoneNumber,
        Guid?       academicStreamId)
        => new()
        {
            Id               = Guid.NewGuid(),
            UserId           = userId,
            FullName         = fullName,
            PhoneNumber      = phoneNumber,
            AcademicStreamId = academicStreamId,
            JoinedAt         = DateTime.UtcNow,
        };

    public void CompleteSetup(
        Guid        academicStreamId,
        PaperMedium medium,
        int         examYear)
    {
        AcademicStreamId = academicStreamId;
        Medium           = medium;
        ExamYear         = examYear;
    }

    public void Update(string fullName, string? phoneNumber, Guid? academicStreamId)
    {
        FullName         = fullName;
        PhoneNumber      = phoneNumber;
        AcademicStreamId = academicStreamId;
    }
}