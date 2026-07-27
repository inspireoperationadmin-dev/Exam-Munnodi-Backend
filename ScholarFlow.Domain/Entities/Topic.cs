using ScholarFlow.Domain.Entities.Base;

namespace ScholarFlow.Domain.Entities;

public class Topic : AuditableEntity
{
    public string NameEnglish { get; private set; } = string.Empty;
    public string? NameTamil { get; private set; }
    public string? NameSinhala { get; private set; }
    public Guid SubjectId { get; private set; }
    public int OrderIndex { get; private set; }

    public Subject Subject { get; set; } = null!;
    public ICollection<SubTopic> SubTopics { get; set; } = new List<SubTopic>();

    private Topic() { }

    public static Topic Create(
        Guid subjectId,
        string nameEnglish,
        int orderIndex = 0,
        string? nameTamil = null,
        string? nameSinhala = null)
        => new()
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            NameEnglish = nameEnglish,
            NameTamil = nameTamil,
            NameSinhala = nameSinhala,
            OrderIndex = orderIndex
        };

    public void Update(
        string nameEnglish,
        int orderIndex,
        string? nameTamil = null,
        string? nameSinhala = null)
    {
        NameEnglish = nameEnglish;
        NameTamil   = nameTamil;
        NameSinhala = nameSinhala;
        OrderIndex  = orderIndex;
    }
}
