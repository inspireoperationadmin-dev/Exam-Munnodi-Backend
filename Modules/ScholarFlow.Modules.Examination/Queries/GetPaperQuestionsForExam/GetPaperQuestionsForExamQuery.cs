using MediatR;
using ScholarFlow.Modules.Examination.DTOs;

namespace ScholarFlow.Modules.Examination.Queries.GetPaperQuestionsForExam;

public sealed record GetPaperQuestionsForExamQuery(Guid PaperId) : IRequest<List<ExamQuestionDto>>;
