using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SmartFeedbackCollector.Models.Configuration;
using SmartFeedbackCollector.Services.Interfaces;

namespace SmartFeedbackCollector.Services
{
    public class AzureKeyVaultService : ISecretService
    {
        private readonly SecretClient? _client;
        private readonly IMemoryCache _cache;
        private readonly bool _isEnabled;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public bool IsKeyVaultEnabled => _isEnabled;

        public AzureKeyVaultService(IOptions<KeyVaultOptions> options, IMemoryCache cache)
        {
            _cache = cache;
            _isEnabled = options.Value.Enabled;

            if (_isEnabled && !string.IsNullOrEmpty(options.Value.VaultUri))
            {
                _client = new SecretClient(new Uri(options.Value.VaultUri), new DefaultAzureCredential());
            }
        }

        public async Task<string?> GetSecretAsync(string secretName)
        {
            if (!_isEnabled || _client == null)
            {
                return null;
            }

            var cacheKey = $"keyvault_{secretName}";
            if (_cache.TryGetValue(cacheKey, out string? cachedValue))
            {
                return cachedValue;
            }

            try
            {
                var secret = await _client.GetSecretAsync(secretName);
                var value = secret.Value.Value;
                _cache.Set(cacheKey, value, CacheDuration);
                return value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Key Vault Error for '{secretName}': {ex.Message}");
                return null;
            }
        }
    }
}
