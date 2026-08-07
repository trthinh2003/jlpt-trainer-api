using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Application.MockTests.Queries.GetMockTestResultPdf
{
    public sealed class GetMockTestResultPdfQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IMockTestPdfGenerator pdfGenerator
    ) : IRequestHandler<GetMockTestResultPdfQuery, byte[]>
    {
        public async Task<byte[]> Handle(GetMockTestResultPdfQuery request, CancellationToken cancellationToken)
        {
            var mockTest = await dbContext.MockTests
                .FirstOrDefaultAsync(m => m.Id == request.MockTestId, cancellationToken);

            if (mockTest is null)
            {
                throw new NotFoundException(nameof(MockTest), request.MockTestId);
            }

            // chặn user A xuất PDF bài test của user B
            if (mockTest.UserId != currentUser.UserId)
            {
                throw new ForbiddenAccessException();
            }

            var displayName = await dbContext.Users
                  .Where(u => u.Id == mockTest.UserId)
                  .Select(u => u.DisplayName)
                  .FirstOrDefaultAsync(cancellationToken) ?? "Người dùng";

            var data = new MockTestPdfData(
                UserDisplayName: displayName,
                Level: mockTest.Level.ToString(),
                TotalQuestions: mockTest.TotalQuestions,
                CorrectAnswers: mockTest.CorrectAnswers,
                ScorePercentage: mockTest.ScorePercentage,
                Duration: mockTest.Duration,
                TakenAt: mockTest.TakenAt);

            return pdfGenerator.Generate(data);
        }
    }
}
