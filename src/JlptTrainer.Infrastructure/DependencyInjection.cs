using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Infrastructure.Auth;
using JlptTrainer.Infrastructure.ExcelImport;
using JlptTrainer.Infrastructure.ExternalServices.Jisho;
using JlptTrainer.Infrastructure.PdfExport;
using JlptTrainer.Infrastructure.Persistence;
using JlptTrainer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;
using QuestPDF.Infrastructure;

namespace JlptTrainer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        QuestPDF.Settings.License = LicenseType.Community;

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Thiếu connection string 'DefaultConnection' trong appsettings.json");

        services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseNpgsql(connectionString, npgsqlOptions =>
                    npgsqlOptions.MigrationsAssembly(
                        typeof(ApplicationDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IApplicationDbContext>(
            provider => provider.GetRequiredService<ApplicationDbContext>());

        // dùng chung 1 connection string cho cả EF Core (write) và Dapper (read nặng)
        services.AddSingleton<IDapperContext>(_ => new DapperContext(connectionString));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenGenerator, JwtTokenGenerator>();

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
}