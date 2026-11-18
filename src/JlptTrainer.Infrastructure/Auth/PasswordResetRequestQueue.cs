using System.Threading.Channels;
using JlptTrainer.Application.Auth.Commands.SendPasswordResetLink;
using JlptTrainer.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JlptTrainer.Infrastructure.Auth;

internal sealed class PasswordResetRequestQueue(
    IOptions<PasswordResetSettings> options,
    IServiceScopeFactory scopeFactory,
    ILogger<PasswordResetRequestQueue> logger) : BackgroundService, IPasswordResetRequestQueue
{
    private readonly Channel<string> _requests = Channel.CreateBounded<string>(new BoundedChannelOptions(100)
    {
        SingleReader = true,
        FullMode = BoundedChannelFullMode.Wait
    });

    public bool TryEnqueue(string email) => options.Value.Enabled && _requests.Writer.TryWrite(email);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;

        await foreach (var email in _requests.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                await sender.Send(new SendPasswordResetLinkCommand(email), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning("Password reset request could not be processed ({ErrorType}).", exception.GetType().Name);
            }
        }
    }
}
