using JlptTrainer.Application.Common.Interfaces;
using Npgsql;
using System.Data;

namespace JlptTrainer.Infrastructure.Persistence
{
    public class DapperContext(string connectionString) : IDapperContext
    {
        public IDbConnection CreateConnection() => new NpgsqlConnection(connectionString);
    }
}
