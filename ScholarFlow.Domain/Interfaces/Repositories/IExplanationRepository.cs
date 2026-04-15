using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Domain.Interfaces.Repositories;

public interface IExplanationRepository
{
    Task<Explanation?> GetByQuestionIdAsync(Guid questionId, CancellationToken ct = default);
    Task<Explanation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ExplanationSection?> GetSectionByIdAsync(Guid sectionId, CancellationToken ct = default);
    Task AddAsync(Explanation explanation, CancellationToken ct = default);
    void Update(Explanation explanation);
    void Delete(Explanation explanation);
    Task AddSectionAsync(ExplanationSection section, CancellationToken ct = default);
    void UpdateSection(ExplanationSection section);
    void DeleteSection(ExplanationSection section);
    Task SaveChangesAsync(CancellationToken ct = default);
}
