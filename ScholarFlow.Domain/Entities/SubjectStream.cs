namespace ScholarFlow.Domain.Entities;

/// <summary>
/// Many-to-many join — which subjects belong to which academic streams.
/// </summary>
public class SubjectStream
{
    public Guid SubjectId { get; set; }
    public Guid StreamId { get; set; }

    public Subject Subject { get; set; } = null!;
    public AcademicStream Stream { get; set; } = null!;
}
