using MediatR;
using ScholarFlow.Domain.Enums;

namespace ScholarFlow.Modules.Academic.Commands.Papers.UpdatePaper;

public sealed record UpdatePaperCommand(
    Guid Id,
    string Title,
    int Year,
    PaperType Type,
    PaperMedium Medium,
    ExamSitting? Sitting,
    decimal NegativeMarkValue,
    string? OfficialPaperCode) : IRequest;
