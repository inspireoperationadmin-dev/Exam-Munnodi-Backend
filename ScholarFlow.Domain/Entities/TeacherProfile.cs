using ScholarFlow.Domain.Entities.Base;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Events;
using ScholarFlow.Domain.Exceptions;

namespace ScholarFlow.Domain.Entities;

/// <summary>
/// Teacher registration profile.
/// New teachers start as Pending → Admin reviews → Accepted or Rejected.
/// </summary>
public class TeacherProfile : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public Guid SubjectId { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public string Qualification { get; private set; } = string.Empty;
    public string? Bio { get; private set; }
    public string TeacherCode { get; private set; } = string.Empty;   // unique shareable code
    public TeacherRegistrationStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    public ApplicationUser User { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public ICollection<StudentTeacherConnection> StudentConnections { get; set; } = new List<StudentTeacherConnection>();

    private TeacherProfile() { }

    public static TeacherProfile Create(
        Guid userId,
        string fullName,
        Guid subjectId,
        string phoneNumber,
        string qualification,
        string? bio,
        string teacherCode)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FullName = fullName,
            SubjectId = subjectId,
            PhoneNumber = phoneNumber,
            Qualification = qualification,
            Bio = bio,
            TeacherCode = teacherCode,
            Status = TeacherRegistrationStatus.Pending
        };

    // ── Domain Methods ────────────────────────────────────────────────────────

    public void Approve()
    {
        if (Status != TeacherRegistrationStatus.Pending)
            throw new DomainException("Only pending teacher profiles can be approved.");

        Status = TeacherRegistrationStatus.Accepted;
        ReviewedAt = DateTime.UtcNow;

        Raise(new TeacherApprovedDomainEvent(Guid.NewGuid(), DateTime.UtcNow, Id, UserId));
    }

    public void Reject(string reason)
    {
        if (Status != TeacherRegistrationStatus.Pending)
            throw new DomainException("Only pending teacher profiles can be rejected.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Rejection reason is required.");

        Status = TeacherRegistrationStatus.Rejected;
        RejectionReason = reason;
        ReviewedAt = DateTime.UtcNow;

        Raise(new TeacherRejectedDomainEvent(Guid.NewGuid(), DateTime.UtcNow, Id, UserId, reason));
    }

    public void Update(string fullName, string phoneNumber, string qualification, string? bio)
    {
        FullName = fullName;
        PhoneNumber = phoneNumber;
        Qualification = qualification;
        Bio = bio;
    }
}
