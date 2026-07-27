using ScholarFlow.Domain.Entities.Base;

namespace ScholarFlow.Domain.Entities;

/// <summary>
/// A/L academic stream — e.g. Physical Science, Bio Science, Commerce, Arts, Technology.
/// </summary>
public class AcademicStream : AuditableEntity
{
    public string NameEnglish { get; private set; } = string.Empty;
    public string? NameTamil { get; private set; }
    public string? NameSinhala { get; private set; }
    public string? Description { get; private set; }

    public ICollection<SubjectStream> SubjectStreams { get; set; } = new List<SubjectStream>();

    private AcademicStream() { }

    public static AcademicStream Create(
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
