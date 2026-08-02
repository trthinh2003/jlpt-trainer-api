using JlptTrainer.Domain.Enums;
using MediatR;

namespace JlptTrainer.Application.MockTests.Queries.GetMockTestQuestions
{
    public sealed record GetMockTestQuestionsQuery(JlptLevel Level, int QuestionCount = 10) : IRequest<List<MockTestQuestionDto>>;
}
