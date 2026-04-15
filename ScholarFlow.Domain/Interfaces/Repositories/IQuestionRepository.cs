using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Domain.Interfaces.Repositories;

public interface IQuestionRepository
{
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
