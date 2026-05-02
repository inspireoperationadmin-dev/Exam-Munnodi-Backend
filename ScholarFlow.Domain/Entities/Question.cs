using ScholarFlow.Domain.Entities.Base;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Domain.Entities;

public class Question : AuditableAggregateRoot
{
    public Guid PaperId { get; private set; }
    public Guid SubTopicId { get; private set; }
    public string QuestionText { get; private set; } = string.Empty;
    public string? QuestionImageUrl { get; private set; }
    public int OrderIndex { get; private set; }
    public decimal Marks { get; private set; } = 2m;   // ← added, default 2
    public DifficultyLevel? ManualDifficulty { get; private set; }
    public SystemDifficultyLevel? SystemDifficulty { get; private set; }

    public Paper Paper { get; set; } = null!;
    public SubTopic SubTopic { get; set; } = null!;
    public ICollection<Option> Options { get; set; } = new List<Option>();
    public Explanation? Explanation { get; set; }
    public ICollection<UserResponse> UserResponses { get; set; } = new List<UserResponse>();

    private Question() { }

    public void Update(
        Guid subTopicId,
        string questionText,
        string? questionImageUrl,
        int orderIndex,
        DifficultyLevel? manualDifficulty,
        decimal marks)
    {
        SubTopicId       = subTopicId;
        QuestionText     = questionText;
        QuestionImageUrl = questionImageUrl;
        OrderIndex       = orderIndex;
        ManualDifficulty = manualDifficulty;
        Marks            = marks;
    }

    public void UpdateSystemDifficulty(SystemDifficultyLevel level)
        => SystemDifficulty = level;

    public static Question Create(
        Guid paperId,
        Guid subTopicId,
        string questionText,
        int orderIndex,
        string? questionImageUrl   = null,
        DifficultyLevel? manualDifficulty = null,
        decimal marks              = 2m)
        => new()
        {
            Id               = Guid.NewGuid(),
            PaperId          = paperId,
            SubTopicId       = subTopicId,
            QuestionText     = questionText,
            OrderIndex       = orderIndex,
            QuestionImageUrl = questionImageUrl,
            ManualDifficulty = manualDifficulty,
            Marks            = marks,
        };
}