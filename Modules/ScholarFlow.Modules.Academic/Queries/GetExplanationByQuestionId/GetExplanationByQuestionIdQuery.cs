using MediatR;
using ScholarFlow.Modules.Academic.DTOs;

namespace ScholarFlow.Modules.Academic.Queries.GetExplanationByQuestionId;

public sealed record GetExplanationByQuestionIdQuery(Guid QuestionId) : IRequest<ExplanationDto?>;
