using JlptTrainer.Domain.Entities;

namespace JlptTrainer.Application.Common.Interfaces
{
    public interface ITokenGenerator
    {
        string GenerateToken(User user);
    }
}
