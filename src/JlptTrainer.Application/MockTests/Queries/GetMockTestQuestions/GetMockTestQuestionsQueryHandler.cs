using JlptTrainer.Application.Common.Interfaces;
using MediatR;

namespace JlptTrainer.Application.MockTests.Queries.GetMockTestQuestions;

public sealed class GetMockTestQuestionsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetMockTestQuestionsQuery, List<MockTestQuestionDto>>
{
    public async Task<List<MockTestQuestionDto>> Handle(GetMockTestQuestionsQuery request, CancellationToken cancellationToken)
    {
        var questions = await MockTestQuestionGenerator.GenerateAsync(dbContext, request.Level, request.QuestionCount, cancellationToken);
        return questions.Select(question => new MockTestQuestionDto(
            question.VocabId, question.Word, question.Reading, question.Choices.ToList())).ToList();
    }
}
