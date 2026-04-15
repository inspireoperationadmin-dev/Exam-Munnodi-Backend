using ScholarFlow.Domain.Entities.Base;

namespace ScholarFlow.Domain.Entities;

/// <summary>
/// A/L academic stream — e.g. Physical Science, Bio Science, Commerce, Arts, Technology.
/// </summary>
public class AcademicStream : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public ICollection<SubjectStream> SubjectStreams { get; set; } = new List<SubjectStream>();

    private AcademicStream() { }

    public static AcademicStream Create(string name, string? description = null)
        => new() { Id = Guid.NewGuid(), Name = name, Description = description };

    public void Update(string name, string? description)
    {
        Name        = name;
        Description = description;
    }
}
