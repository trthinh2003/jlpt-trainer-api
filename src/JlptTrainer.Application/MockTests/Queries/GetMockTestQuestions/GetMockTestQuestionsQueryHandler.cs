using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.MockTests.Queries.GetMockTestQuestions
{
    public sealed class GetMockTestQuestionsQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetMockTestQuestionsQuery, List<MockTestQuestionDto>>
    {
        private const int ChoicesPerQuestion = 4;

        public async Task<List<MockTestQuestionDto>> Handle(GetMockTestQuestionsQuery request, CancellationToken cancellationToken)
        {
            var pool = await dbContext.Vocabs
                .Where(v => v.Level == request.Level)
                .Select(v => new { v.Id, v.Word, v.Reading, v.Meaning })
                .ToListAsync(cancellationToken);

            // cần tối thiểu 4 từ: 1 làm câu hỏi + 3 làm nhiễu, nếu không đủ -> ko tạo đc bộ test
            if (pool.Count < ChoicesPerQuestion)
            {
                throw new InsufficientDataException(
                    $"Cần ít nhất {ChoicesPerQuestion} từ vựng ở level {request.Level} để tạo đề " +
                    $"(hiện có {pool.Count}). Hãy thêm từ vựng hoặc import Excel trước.");
            }

            var random = Random.Shared;
            var actualQuestionCount = Math.Min(request.QuestionCount, pool.Count);

            var questionVocabs = pool
                .OrderBy(_ => random.Next())
                .Take(actualQuestionCount)
                .ToList();

            var questions = new List<MockTestQuestionDto>();

            foreach (var vocab in questionVocabs)
            {
                var distractors = pool
                    .Where(v => v.Id != vocab.Id)
                    .OrderBy(_ => random.Next())
                    .Take(ChoicesPerQuestion - 1)
                    .Select(v => v.Meaning)
                    .ToList();

                var choices = distractors.Append(vocab.Meaning)
                    .OrderBy(_ => random.Next()) // xào câu hỏi phát để đáp án đúng không luôn nằm cuối :))
                    .ToList();

                questions.Add(new MockTestQuestionDto(vocab.Id, vocab.Word, vocab.Reading, choices));
            }

            return questions;
        }
    }
}
