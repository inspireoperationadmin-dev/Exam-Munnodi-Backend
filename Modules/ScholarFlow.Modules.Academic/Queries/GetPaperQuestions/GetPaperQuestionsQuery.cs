using MediatR;
using ScholarFlow.Modules.Academic.DTOs;

namespace ScholarFlow.Modules.Academic.Queries.GetPaperQuestions;

public sealed record GetPaperQuestionsQuery(Guid PaperId) : IRequest<List<QuestionWithOptionsDto>>;
