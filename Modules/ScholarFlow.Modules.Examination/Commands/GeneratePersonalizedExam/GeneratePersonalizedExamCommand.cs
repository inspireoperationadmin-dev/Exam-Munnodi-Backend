using MediatR;
using ScholarFlow.Modules.Examination.DTOs;

namespace ScholarFlow.Modules.Examination.Commands.GeneratePersonalizedExam;

public sealed record GeneratePersonalizedExamCommand(
    Guid SubjectId,
    int QuestionCount = 50) : IRequest<StartSessionResultDto>;
