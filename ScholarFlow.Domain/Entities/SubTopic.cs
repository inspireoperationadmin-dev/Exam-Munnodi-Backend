using ScholarFlow.Domain.Entities.Base;

namespace ScholarFlow.Domain.Entities;

public class SubTopic : AuditableEntity
{
    public string SubTopicName { get; private set; } = string.Empty;
    public Guid TopicId { get; private set; }
    public int OrderIndex { get; private set; }

    public Topic Topic { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();

    private SubTopic() { }

    public static SubTopic Create(Guid topicId, string subTopicName, int orderIndex = 0)
        => new() { Id = Guid.NewGuid(), TopicId = topicId, SubTopicName = subTopicName, OrderIndex = orderIndex };

    public void Update(string subTopicName, int orderIndex)
    {
        SubTopicName = subTopicName;
        OrderIndex   = orderIndex;
    }
}
