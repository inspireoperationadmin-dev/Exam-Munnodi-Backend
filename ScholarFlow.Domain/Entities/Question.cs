using ScholarFlow.Domain.Entities.Base;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Domain.Entities;

/// <summary>
/// A single MCQ question. Sri Lanka A/L format: 5 options (1)(2)(3)(4)(5).
/// Belongs to a Paper and a SubTopic.
/// </summary>
public class Question : AuditableAggregateRoot
{
    public Guid PaperId { get; private set; }
    public Guid SubTopicId { get; private set; }
    public string QuestionText { get; private set; } = string.Empty;
    public string? QuestionImageUrl { get; private set; }
    public int OrderIndex { get; private set; }

    /// <summary>Cognitive nature set manually by Admin/Teacher.</summary>
    public DifficultyLevel? ManualDifficulty { get; private set; }

    /// <summary>Performance-based difficulty auto-calculated by the system. Requires ≥5 attempts.</summary>
    public SystemDifficultyLevel? SystemDifficulty { get; private set; }

    public Paper Paper { get; set; } = null!;
    public SubTopic SubTopic { get; set; } = null!;
    public ICollection<Option> Options { get; set; } = new List<Option>();
    public Explanation? Explanation { get; set; }
    public ICollection<UserResponse> UserResponses { get; set; } = new List<UserResponse>();

    private Question() { }

    public void Update(Guid subTopicId, string questionText, string? questionImageUrl, int orderIndex, DifficultyLevel? manualDifficulty)
    {
        SubTopicId       = subTopicId;
        QuestionText     = questionText;
        QuestionImageUrl = questionImageUrl;
        OrderIndex       = orderIndex;
        ManualDifficulty = manualDifficulty;
    }

    /// <summary>Called by Analytics module after accumulating enough student attempt data.</summary>
    public void UpdateSystemDifficulty(SystemDifficultyLevel level)
        => SystemDifficulty = level;

    public static Question Create(
        Guid paperId,
        Guid subTopicId,
        string questionText,
        int orderIndex,
        string? questionImageUrl = null,
        DifficultyLevel? manualDifficulty = null)
        => new()
        {
            Id               = Guid.NewGuid(),
            PaperId          = paperId,
            SubTopicId       = subTopicId,
            QuestionText     = questionText,
            OrderIndex       = orderIndex,
            QuestionImageUrl = questionImageUrl,
            ManualDifficulty = manualDifficulty
        };
}
