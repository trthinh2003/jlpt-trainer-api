using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using JlptTrainer.Application.Auth.Commands.Login;
using JlptTrainer.Application.Auth.Commands.Register;
using JlptTrainer.Application.Auth.Commands.ResetPassword;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JlptTrainer.Api.IntegrationTests;

public sealed class PasswordResetEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task ForgotAndReset_ShouldUseOneTimeTokenAndInvalidateOldSession()
    {
        using var client = factory.CreateClient();
        var mail = factory.Services.GetRequiredService<TestPasswordResetEmailSender>();
        var email = $"{Guid.NewGuid()}@example.com";
        var unknownEmail = $"{Guid.NewGuid()}@example.com";
        var oldSession = await RegisterAsync(client, email);

        var unknown = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = unknownEmail });
        var known = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        Assert.Equal(HttpStatusCode.Accepted, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, known.StatusCode);
        Assert.Equal(await unknown.Content.ReadAsStringAsync(), await known.Content.ReadAsStringAsync());

        var token = await mail.ReceiveAsync(email);
        Assert.False(mail.WasSentTo(unknownEmail));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.SingleAsync(user => user.Email == email);
            Assert.NotNull(user.PasswordResetTokenHash);
            Assert.NotEqual(token, user.PasswordResetTokenHash);
        }

        var reset = new ResetPasswordCommand(token, "Updated456");
        var attempts = await Task.WhenAll(
            client.PostAsJsonAsync("/api/auth/reset-password", reset),
            client.PostAsJsonAsync("/api/auth/reset-password", reset));
        Assert.Single(attempts, response => response.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(attempts, response => response.StatusCode == HttpStatusCode.BadRequest);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oldSession.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/reviewcards/due")).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(email, "Initial123"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(email, "Updated456"))).StatusCode);
    }

    [Fact]
    public async Task Reset_WithExpiredLink_ShouldKeepCurrentPassword()
    {
        using var client = factory.CreateClient();
        var email = $"{Guid.NewGuid()}@example.com";
        await RegisterAsync(client, email);
        var request = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
        var token = await factory.Services.GetRequiredService<TestPasswordResetEmailSender>().ReceiveAsync(email);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Users.Where(user => user.Email == email).ExecuteUpdateAsync(
            setters => setters.SetProperty(user => user.PasswordResetExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)));

        var response = await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordCommand(token, "Updated456"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new LoginCommand(email, "Initial123"))).StatusCode);
    }

    private static async Task<AuthResult> RegisterAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterCommand(email, "Initial123", "Password Reset Test"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResult>())!;
    }
}

internal sealed class TestPasswordResetEmailSender : IPasswordResetEmailSender
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _messages = new();

    public Task SendAsync(string email, string token, CancellationToken cancellationToken)
    {
        GetMessage(email).TrySetResult(token);
        return Task.CompletedTask;
    }

    public Task<string> ReceiveAsync(string email) => GetMessage(email).Task.WaitAsync(TimeSpan.FromSeconds(10));

    public bool WasSentTo(string email) => _messages.TryGetValue(email, out var message) && message.Task.IsCompletedSuccessfully;

    private TaskCompletionSource<string> GetMessage(string email) => _messages.GetOrAdd(
        email, _ => new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously));
}
