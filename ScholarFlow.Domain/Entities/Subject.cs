using ScholarFlow.Domain.Entities.Base;

namespace ScholarFlow.Domain.Entities;

/// <summary>
/// Academic subject — e.g. Physics, Chemistry, Combined Maths.
/// One subject can belong to multiple streams (via SubjectStream).
/// </summary>
public class Subject : AuditableEntity
{
    public string NameEnglish { get; private set; } = string.Empty;
    public string? NameTamil { get; private set; }
    public string? NameSinhala { get; private set; }
    public string? Description { get; private set; }

    public ICollection<SubjectStream> SubjectStreams { get; set; } = new List<SubjectStream>();
    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
    public ICollection<Paper> Papers { get; set; } = new List<Paper>();

    private Subject() { }

    public static Subject Create(
        string nameEnglish,
        string? description = null,
        string? nameTamil = null,
        string? nameSinhala = null)
        => new()
        {
            Id = Guid.NewGuid(),
            NameEnglish = nameEnglish,
            NameTamil = nameTamil,
            NameSinhala = nameSinhala,
            Description = description
        };

    public void Update(
        string nameEnglish,
        string? description,
        string? nameTamil = null,
        string? nameSinhala = null)
    {
        NameEnglish = nameEnglish;
        NameTamil   = nameTamil;
        NameSinhala = nameSinhala;
        Description = description;
    }
}
