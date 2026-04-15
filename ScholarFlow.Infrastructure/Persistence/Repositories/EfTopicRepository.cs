using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Interfaces.Repositories;

namespace ScholarFlow.Infrastructure.Persistence.Repositories;

public sealed class EfTopicRepository(ApplicationDbContext db) : ITopicRepository
{
    public Task<List<Topic>> GetBySubjectIdAsync(Guid subjectId, CancellationToken ct = default)
        => db.Topics
            .Where(t => t.SubjectId == subjectId)
            .Include(t => t.SubTopics)
            .OrderBy(t => t.OrderIndex)
            .ToListAsync(ct);

    public Task<Topic?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Topics.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<SubTopic?> GetSubTopicByIdAsync(Guid subTopicId, CancellationToken ct = default)
        => db.SubTopics.FirstOrDefaultAsync(st => st.Id == subTopicId, ct);

    public async Task AddAsync(Topic topic, CancellationToken ct = default)
        => await db.Topics.AddAsync(topic, ct);

    public void Update(Topic topic)
        => db.Topics.Update(topic);

    public void Delete(Topic topic)
        => db.Topics.Remove(topic);

    public async Task AddSubTopicAsync(SubTopic subTopic, CancellationToken ct = default)
        => await db.SubTopics.AddAsync(subTopic, ct);

    public void UpdateSubTopic(SubTopic subTopic)
        => db.SubTopics.Update(subTopic);

    public void DeleteSubTopic(SubTopic subTopic)
        => db.SubTopics.Remove(subTopic);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
