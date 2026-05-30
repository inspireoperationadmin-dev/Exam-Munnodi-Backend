using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Domain.Interfaces.Repositories;

public interface IQuestionRepository
{
    // Added: Support bulk insertions for high-throughput admin uploads [1]
    Task AddRangeAsync(IEnumerable<Question> questions, CancellationToken ct = default);
    Task AddOptionsRangeAsync(IEnumerable<Option> options, CancellationToken ct = default);

    Task<List<Question>> GetByPaperIdAsync(Guid paperId, CancellationToken ct = default);
    Task<Question?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Option?> GetOptionByIdAsync(Guid optionId, CancellationToken ct = default);
    Task AddAsync(Question question, CancellationToken ct = default);
    void Update(Question question);
    void Delete(Question question);
    Task AddOptionAsync(Option option, CancellationToken ct = default);
    void UpdateOption(Option option);
    Task SaveChangesAsync(CancellationToken ct = default);
}