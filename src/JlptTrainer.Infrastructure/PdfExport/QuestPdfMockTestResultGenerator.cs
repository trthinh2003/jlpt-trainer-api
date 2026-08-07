using JlptTrainer.Application.Common.Interfaces;
using QuestPDF.Helpers;
using System.Reflection.Metadata;

namespace JlptTrainer.Infrastructure.PdfExport
{
    public class QuestPdfMockTestResultGenerator : IMockTestPdfGenerator
    {
        public byte[] Generate(MockTestPdfData data)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("Kết quả bài thi thử JLPT")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Darken2);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(10);

                            column.Item().Text($"Người thi: {data.UserDisplayName}");
                            column.Item().Text($"Cấp độ: {data.Level}");
                            column.Item().Text(
                                $"Thời gian làm bài: {data.TakenAt:dd/MM/yyyy HH:mm}");
                            column.Item().Text($"Thời lượng: {FormatDuration(data.Duration)}");

                            column.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                            column.Item().PaddingTop(15).Row(row =>
                            {
                                row.RelativeItem().Column(inner =>
                                {
                                    inner.Item().Text("Tổng số câu").FontColor(Colors.Grey.Darken1);
                                    inner.Item().Text($"{data.TotalQuestions}").FontSize(24).SemiBold();
                                });

                                row.RelativeItem().Column(inner =>
                                {
                                    inner.Item().Text("Số câu đúng").FontColor(Colors.Grey.Darken1);
                                    inner.Item().Text($"{data.CorrectAnswers}").FontSize(24).SemiBold()
                                        .FontColor(Colors.Green.Darken1);
                                });

                                row.RelativeItem().Column(inner =>
                                {
                                    inner.Item().Text("Điểm số").FontColor(Colors.Grey.Darken1);
                                    inner.Item().Text($"{data.ScorePercentage:0.##}%").FontSize(24).SemiBold()
                                        .FontColor(ScoreColor(data.ScorePercentage));
                                });
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("JlptTrainer - Tự tạo lúc ");
                            text.Span($"{DateTimeOffset.UtcNow:dd/MM/yyyy HH:mm} UTC");
                        });
                });
            });

            return document.GeneratePdf();
        }

        private static string FormatDuration(TimeSpan duration) =>
            duration.TotalHours >= 1
                ? $"{duration:hh\\:mm\\:ss}"
                : $"{duration:mm\\:ss}";

        private static string ScoreColor(double score) => score switch
        {
            >= 80 => Colors.Green.Darken1,
            >= 50 => Colors.Orange.Darken1,
            _ => Colors.Red.Darken1
        };
    }
}
