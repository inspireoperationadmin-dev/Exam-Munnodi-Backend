using ScholarFlow.Domain.Interfaces;

namespace ScholarFlow.Domain.Entities.Base;

/// <summary>
/// Base for entities that need audit trail + soft delete but do NOT raise domain events.
/// e.g. Subject, Topic, SubTopic, AcademicStream
/// </summary>
public abstract class AuditableEntity : IAuditable, ISoftDeletable
{
    public Guid Id { get; protected set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
