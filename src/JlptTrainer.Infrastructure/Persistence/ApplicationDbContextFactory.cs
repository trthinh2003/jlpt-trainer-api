using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace JlptTrainer.Infrastructure.Persistence
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "JlptTrainer.Api");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.GetFullPath(basePath))
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables() // đọc ConnectionStrings__DefaultConnection
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Không tìm thấy connection string 'DefaultConnection'. " +
                    "Set biến môi trường ConnectionStrings__DefaultConnection hoặc " +
                    "thêm connection string thật vào appsettings.Development.json.");

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            if (args.Contains("--merge-identical-vocabs", StringComparer.Ordinal))
            {
                optionsBuilder.AddInterceptors(new VocabMergeTransactionInterceptor());
            }
            optionsBuilder
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention();

            return new ApplicationDbContext(optionsBuilder.Options);
        }

        private sealed class VocabMergeTransactionInterceptor : DbTransactionInterceptor
        {
            private static DbCommand EnableMerge(DbTransaction transaction)
            {
                var command = transaction.Connection!.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "SET LOCAL jlpt.merge_identical_vocabs = 'on'";
                return command;
            }

            public override DbTransaction TransactionStarted(DbConnection connection,
                TransactionEndEventData eventData, DbTransaction result)
            {
                using var command = EnableMerge(result);
                command.ExecuteNonQuery();
                return result;
            }

            public override async ValueTask<DbTransaction> TransactionStartedAsync(DbConnection connection,
                TransactionEndEventData eventData, DbTransaction result, CancellationToken cancellationToken = default)
            {
                await using var command = EnableMerge(result);
                await command.ExecuteNonQueryAsync(cancellationToken);
                return result;
            }
        }
    }
}
