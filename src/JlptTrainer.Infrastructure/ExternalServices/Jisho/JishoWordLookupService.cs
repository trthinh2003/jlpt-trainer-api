using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.Common.Interfaces;
using System.Net.Http.Json;

namespace JlptTrainer.Infrastructure.ExternalServices.Jisho
{
    public class JishoWordLookupService(HttpClient httpClient) : IWordLookupService
    {
        public async Task<List<WordLookupResult>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
        {
            JishoApiResponse? response;

            try
            {
                response = await httpClient.GetFromJsonAsync<JishoApiResponse>(
                    $"search/words?keyword={Uri.EscapeDataString(keyword)}",
                    cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new ExternalServiceException("Jisho", ex.Message);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ExternalServiceException("Jisho", "Yêu cầu quá thời gian chờ (timeout).");
            }

            if (response is null || response.Data.Count == 0)
            {
                return [];
            }

            var results = new List<WordLookupResult>();

            foreach (var item in response.Data)
            {
                // 1 entry Jisho có thể có nhiều cách viết (japanese[]) - lấy cách viết đầu tiên
                // làm đại diện chính, đủ dùng cho mục đích gợi ý nhanh.
                var japanese = item.Japanese.FirstOrDefault();
                if (japanese is null)
                {
                    continue;
                }

                var meanings = item.Senses
                    .SelectMany(s => s.EnglishDefinitions)
                    .Distinct()
                    .Take(5) // giới hạn 5 nghĩa đầu
                    .ToList();

                results.Add(new WordLookupResult(
                    Word: japanese.Word ?? japanese.Reading ?? keyword, 
                    Reading: japanese.Reading ?? string.Empty,
                    Meanings: meanings,
                    IsCommon: item.IsCommon ?? false));
            }

            return results;
        }
    }
}
