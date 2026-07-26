using System.Data;

namespace JlptTrainer.Application.Common.Interfaces
{
    public interface IDapperContext
    {
        IDbConnection CreateConnection();
    }
}
