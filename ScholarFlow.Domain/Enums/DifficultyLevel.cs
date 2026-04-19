namespace ScholarFlow.Domain.Enums;

/// <summary>
/// Question type / cognitive nature — set manually by Admin or Teacher.
/// Based on the cognitive demand required to answer the question.
/// </summary>
public enum DifficultyLevel
{
    /// <summary>நேரடியாக விடை அளிக்கக் கூடியவை — Direct recall, definition, fact.</summary>
    DirectRecall = 1,

    /// <summary>சிந்தித்து விடை அளிக்கக் கூடியவை — Conceptual understanding, reasoning.</summary>
    Conceptual   = 2,

    /// <summary>கணித்தல் மூலம் விடை அளிக்கக் கூடியவை — Formula application, calculation.</summary>
    Calculation  = 3,

    /// <summary>உயர் சிந்தனை — Multi-step reasoning, analysis, application.</summary>
    Analytical   = 4
}
