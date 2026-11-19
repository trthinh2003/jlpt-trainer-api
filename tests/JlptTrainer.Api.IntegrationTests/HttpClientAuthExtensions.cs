using JlptTrainer.Application.Auth.Commands.Register;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using JlptTrainer.Domain.Enums;

namespace JlptTrainer.Api.IntegrationTests
{
    public static class HttpClientAuthExtensions
    {
        public static async Task<AuthResult> AuthenticateAsync(this HttpClient client)
        {
            var command = new RegisterCommand(
                Email: $"{Guid.NewGuid()}@example.com",
                Password: "Test1234",
                DisplayName: "Integration Test User");

            var response = await client.PostAsJsonAsync("/api/auth/register", command);
            response.EnsureSuccessStatusCode();

            var result = (await response.Content.ReadFromJsonAsync<AuthResult>())!;

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
            return result;
        }

        public static async Task AuthenticateAsCatalogManagerAsync(this HttpClient client, CustomWebApplicationFactory factory)
        {
            var account = await client.AuthenticateAsync();
            await factory.SetUserRoleAsync(account.UserId, UserRole.CatalogManager);
        }
    }
}
