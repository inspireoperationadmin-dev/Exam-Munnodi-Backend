using ScholarFlow.Domain.Entities.Base;

namespace ScholarFlow.Domain.Entities;

/// <summary>
/// Academic subject — e.g. Physics, Chemistry, Combined Maths.
/// One subject can belong to multiple streams (via SubjectStream).
/// </summary>
public class Subject : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public ICollection<SubjectStream> SubjectStreams { get; set; } = new List<SubjectStream>();
    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
    public ICollection<Paper> Papers { get; set; } = new List<Paper>();

    private Subject() { }

    public static Subject Create(string name, string? description = null)
        => new() { Id = Guid.NewGuid(), Name = name, Description = description };

    public void Update(string name, string? description)
    {
        Name        = name;
        Description = description;
    }
}
