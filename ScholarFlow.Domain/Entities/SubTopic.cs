using ScholarFlow.Domain.Entities.Base;

namespace ScholarFlow.Domain.Entities;

public class SubTopic : AuditableEntity
{
    public string NameEnglish { get; private set; } = string.Empty;
    public string? NameTamil { get; private set; }
    public string? NameSinhala { get; private set; }
    public Guid TopicId { get; private set; }
    public int OrderIndex { get; private set; }

    public Topic Topic { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();

    private SubTopic() { }

    public static SubTopic Create(
        Guid topicId,
        string nameEnglish,
        int orderIndex = 0,
        string? nameTamil = null,
        string? nameSinhala = null)
        => new()
        {
            Id = Guid.NewGuid(),
            TopicId = topicId,
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
        NameEnglish  = nameEnglish;
        NameTamil    = nameTamil;
        NameSinhala  = nameSinhala;
        OrderIndex   = orderIndex;
    }
}
