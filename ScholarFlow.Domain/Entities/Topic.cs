using ScholarFlow.Domain.Entities.Base;

namespace ScholarFlow.Domain.Entities;

public class Topic : AuditableEntity
{
    public string TopicName { get; private set; } = string.Empty;
    public Guid SubjectId { get; private set; }
    public int OrderIndex { get; private set; }

    public Subject Subject { get; set; } = null!;
    public ICollection<SubTopic> SubTopics { get; set; } = new List<SubTopic>();

    private Topic() { }

    public static Topic Create(Guid subjectId, string topicName, int orderIndex = 0)
        => new() { Id = Guid.NewGuid(), SubjectId = subjectId, TopicName = topicName, OrderIndex = orderIndex };

    public void Update(string topicName, int orderIndex)
    {
        TopicName  = topicName;
        OrderIndex = orderIndex;
    }
}
