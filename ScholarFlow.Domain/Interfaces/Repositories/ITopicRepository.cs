using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Domain.Interfaces.Repositories;

public interface ITopicRepository
{
    Task<List<Topic>> GetBySubjectIdAsync(Guid subjectId, CancellationToken ct = default);
    Task<Topic?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SubTopic?> GetSubTopicByIdAsync(Guid subTopicId, CancellationToken ct = default);
    Task AddAsync(Topic topic, CancellationToken ct = default);
    void Update(Topic topic);
    void Delete(Topic topic);
    Task AddSubTopicAsync(SubTopic subTopic, CancellationToken ct = default);
    void UpdateSubTopic(SubTopic subTopic);
    void DeleteSubTopic(SubTopic subTopic);
    Task SaveChangesAsync(CancellationToken ct = default);
}
