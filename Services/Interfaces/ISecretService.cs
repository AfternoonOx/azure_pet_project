using System.Threading.Tasks;

namespace SmartFeedbackCollector.Services.Interfaces
{
    public interface ISecretService
    {
        Task<string?> GetSecretAsync(string secretName);
        bool IsKeyVaultEnabled { get; }
    }
}
