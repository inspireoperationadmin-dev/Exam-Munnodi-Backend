namespace ScholarFlow.Domain.Enums;

public enum ResponseStatus
{
    Unvisited = 1,
    Visited   = 2,  // Seen but not answered
    Answered  = 3,
    Flagged   = 4   // Marked for review
}
