using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Infrastructure.Auth;
using JlptTrainer.Infrastructure.ExcelImport;
using JlptTrainer.Infrastructure.ExternalServices.Jisho;
using JlptTrainer.Infrastructure.PdfExport;
using JlptTrainer.Infrastructure.Persistence;
using JlptTrainer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OfficeOpenXml;
using QuestPDF.Infrastructure;

namespace JlptTrainer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        QuestPDF.Settings.License = LicenseType.Community;

        services.AddOptions<DatabaseConnectionSettings>()
            .BindConfiguration("ConnectionStrings")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.DefaultConnection),
                "Thiếu connection string 'DefaultConnection'.")
            .ValidateOnStart();

        services.AddDbContext<ApplicationDbContext>((provider, options) =>
            options
                .UseNpgsql(GetConnectionString(provider), npgsqlOptions =>
                    npgsqlOptions.MigrationsAssembly(
                        typeof(ApplicationDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IApplicationDbContext>(
            provider => provider.GetRequiredService<ApplicationDbContext>());

        // dùng chung 1 connection string cho cả EF Core (write) và Dapper (read nặng)
        services.AddSingleton(provider => new DapperContext(GetConnectionString(provider)));
        services.AddSingleton<IDashboardReadService, DashboardReadService>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddOptions<JwtSettings>()
            .BindConfiguration(JwtSettings.SectionName)
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Issuer)
                && !string.IsNullOrWhiteSpace(settings.Audience)
                && !string.IsNullOrWhiteSpace(settings.Secret)
                && System.Text.Encoding.UTF8.GetByteCount(settings.Secret) >= 32
                && settings.ExpiryMinutes > 0, "Cấu hình Jwt không hợp lệ.")
            .ValidateOnStart();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenGenerator, JwtTokenGenerator>();

        services.AddOptions<PasswordResetSettings>()
            .BindConfiguration(PasswordResetSettings.SectionName)
            .Validate(settings => settings.IsValid(), "Cấu hình PasswordReset không hợp lệ.")
            .ValidateOnStart();
        services.AddScoped<IPasswordResetEmailSender, GmailPasswordResetEmailSender>();
        services.AddSingleton<PasswordResetRequestQueue>();
        services.AddSingleton<IPasswordResetRequestQueue>(provider => provider.GetRequiredService<PasswordResetRequestQueue>());
        services.AddHostedService(provider => provider.GetRequiredService<PasswordResetRequestQueue>());

        services.AddScoped<IExcelReader, EPPlusExcelReader>();
        services.AddScoped<IExcelTemplateGenerator, EPPlusExcelTemplateGenerator>();
        services.AddScoped<IMockTestPdfGenerator, QuestPdfMockTestResultGenerator>();

        services.AddHttpClient<IWordLookupService, JishoWordLookupService>(client =>
        {
            client.BaseAddress = new Uri("https://jisho.org/api/v1/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddSingleton(TimeProvider.System);

        return services;
    }

    private static string GetConnectionString(IServiceProvider provider) =>
        provider.GetRequiredService<IOptions<DatabaseConnectionSettings>>().Value.DefaultConnection;
}

internal sealed class DatabaseConnectionSettings
{
    public string DefaultConnection { get; set; } = string.Empty;
}
