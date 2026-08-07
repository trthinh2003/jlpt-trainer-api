using FluentAssertions;
using JlptTrainer.Application.Auth.Commands.Login;
using JlptTrainer.Application.Auth.Commands.Register;
using System.Net;
using System.Net.Http.Json;

namespace JlptTrainer.Api.IntegrationTests
{
    public class AuthEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client = factory.CreateClient();

        [Fact]
        public async Task Register_WithValidData_ShouldReturnTokenAndCreateUser()
        {
            var command = new RegisterCommand(
                Email: $"{Guid.NewGuid()}@example.com", // email random tránh đụng test khác
                Password: "Test1234",
                DisplayName: "Test User");

            var response = await _client.PostAsJsonAsync("/api/auth/register", command);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<AuthResult>();
            result.Should().NotBeNull();
            result!.Token.Should().NotBeNullOrEmpty();
            result.Email.Should().Be(command.Email.ToLowerInvariant());
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_ShouldReturnConflict()
        {
            var email = $"{Guid.NewGuid()}@example.com";
            var command = new RegisterCommand(email, "Test1234", "First User");

            // đănh ký lần đầu -> phải thành công
            var firstResponse = await _client.PostAsJsonAsync("/api/auth/register", command);
            firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // đăng ký lần 2 với cùng email -> phải bị chặn 409, không phải 500
            var duplicateCommand = new RegisterCommand(email, "Different123", "Second User");
            var secondResponse = await _client.PostAsJsonAsync("/api/auth/register", duplicateCommand);

            secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Login_WithCorrectCredentials_ShouldReturnToken()
        {
            var email = $"{Guid.NewGuid()}@example.com";
            var password = "Test1234";

            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterCommand(email, password, "Login Test User"));

            var loginResponse = await _client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginCommand(email, password));

            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await loginResponse.Content.ReadFromJsonAsync<AuthResult>();
            result!.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Login_WithWrongPassword_ShouldReturnUnauthorized()
        {
            var email = $"{Guid.NewGuid()}@example.com";

            await _client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterCommand(email, "CorrectPassword1", "Wrong Password Test"));

            var loginResponse = await _client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginCommand(email, "WrongPassword1"));

            loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
