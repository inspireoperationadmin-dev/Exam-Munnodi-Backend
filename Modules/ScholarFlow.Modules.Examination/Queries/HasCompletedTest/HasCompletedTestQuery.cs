using MediatR;

namespace ScholarFlow.Modules.Examination.Queries.HasCompletedTest;

public sealed record HasCompletedTestQuery(Guid PaperId) : IRequest<bool>;
