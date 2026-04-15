using MediatR;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Modules.Academic.Commands.Papers.CreatePaper;

public sealed record CreatePaperCommand(
    string Title,
    Guid? SubjectId,
    PaperType Type,
    PaperMedium Medium,
    int Year,
    ExamSitting? Sitting,
    decimal NegativeMarkValue,
    bool IsPublic,
    string? OfficialPaperCode) : IRequest<Guid>;
