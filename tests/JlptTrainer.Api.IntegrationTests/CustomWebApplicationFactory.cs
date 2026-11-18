using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace JlptTrainer.Api.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("jlpttrainer_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _dbContainer.GetConnectionString(),
                    ["Jwt:Secret"] = "IntegrationTest_SecretKey_AtLeast32CharsLong_1234567890",
                    ["Jwt:Issuer"] = "JlptTrainer.Api.Tests",
                    ["Jwt:Audience"] = "JlptTrainer.Client.Tests",
                    ["Jwt:ExpiryMinutes"] = "60",
                    ["PasswordReset:Enabled"] = "true",
                    ["PasswordReset:GmailAddress"] = "fixture@example.invalid",
                    ["PasswordReset:GmailAppPassword"] = "fixture-only"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<TestPasswordResetEmailSender>();
                services.AddSingleton<IPasswordResetEmailSender>(provider => provider.GetRequiredService<TestPasswordResetEmailSender>());
            });
        }

        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();

            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        public new async Task DisposeAsync()
        {
            await _dbContainer.DisposeAsync();
        }
    }
}
