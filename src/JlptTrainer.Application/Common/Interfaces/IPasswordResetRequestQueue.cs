namespace JlptTrainer.Application.Common.Interfaces;

public interface IPasswordResetRequestQueue
{
    bool TryEnqueue(string email);
}
