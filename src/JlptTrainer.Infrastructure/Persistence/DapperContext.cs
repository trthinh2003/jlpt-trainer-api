using Npgsql;
using System.Data;

namespace JlptTrainer.Infrastructure.Persistence
{
    internal sealed class DapperContext(string connectionString)
    {
        public IDbConnection CreateConnection() => new NpgsqlConnection(connectionString);
    }
}
