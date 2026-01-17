using Microsoft.Extensions.Caching.Memory;
using SmartFeedbackCollector.Models.Exceptions;
using SmartFeedbackCollector.Services.Interfaces;
using System.Reflection;

namespace SmartFeedbackCollector.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly ISecretService _secretService;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public ConfigurationService(
            ISecretService secretService, 
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _secretService = secretService;
            _configuration = configuration;
            _cache = cache;
        }

        public async Task<T> GetConfigurationAsync<T>(string sectionName) where T : class, new()
        {
            var cacheKey = $"config_{sectionName}";
            if (_cache.TryGetValue(cacheKey, out T? cachedConfig) && cachedConfig != null)
            {
                return cachedConfig;
            }

            var config = new T();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var missingKeys = new List<string>();
            var triedKeyVault = _secretService.IsKeyVaultEnabled;

            foreach (var prop in properties)
            {
                if (!prop.CanWrite) continue;

                var keyName = $"{sectionName}--{prop.Name}";
                object? value = null;

                if (_secretService.IsKeyVaultEnabled)
                {
                    try
                    {
                        var secretValue = await _secretService.GetSecretAsync(keyName);
                        if (!string.IsNullOrEmpty(secretValue))
                        {
                            value = ConvertValue(secretValue, prop.PropertyType);
                            Console.WriteLine($"✓ Loaded '{keyName}' from KeyVault");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠ KeyVault fetch failed for '{keyName}': {ex.Message}");
                    }
                }

                if (value == null)
                {
                    var configValue = _configuration[$"{sectionName}:{prop.Name}"];
                    if (!string.IsNullOrEmpty(configValue))
                    {
                        value = ConvertValue(configValue, prop.PropertyType);
                        Console.WriteLine($"→ Loaded '{prop.Name}' from appsettings.json (fallback)");
                    }
                }

                if (value != null)
                {
                    prop.SetValue(config, value);
                }
                else if (prop.PropertyType == typeof(string) || !prop.PropertyType.IsValueType)
                {
                    missingKeys.Add($"{sectionName}:{prop.Name}");
                }
            }

            if (missingKeys.Count > 0)
            {
                var sources = new List<string>();
                if (triedKeyVault) sources.Add("KeyVault");
                sources.Add("appsettings.json");

                throw new ConfigurationException(
                    "Couldn't load configuration. Please check your credentials if they are valid.",
                    missingKeys,
                    triedKeyVault,
                    true
                );
            }

            _cache.Set(cacheKey, config, CacheDuration);
            return config;
        }

        public async Task<bool> ValidateAllConfigurationsAsync()
        {
            try
            {
                var tasks = new List<Task>
                {
                    ValidateConfiguration<Models.Configuration.AzureStorageOptions>("AzureStorage"),
                    ValidateConfiguration<Models.Configuration.CognitiveServicesOptions>("CognitiveServices"),
                    ValidateConfiguration<Models.Configuration.ContentSafetyOptions>("ContentSafety")
                };

                await Task.WhenAll(tasks);
                return true;
            }
            catch (ConfigurationException)
            {
                return false;
            }
        }

        private async Task ValidateConfiguration<T>(string sectionName) where T : class, new()
        {
            await GetConfigurationAsync<T>(sectionName);
        }

        private object? ConvertValue(string value, Type targetType)
        {
            if (targetType == typeof(string))
                return value;

            if (targetType == typeof(int))
                return int.TryParse(value, out var intVal) ? intVal : null;

            if (targetType == typeof(bool))
                return bool.TryParse(value, out var boolVal) ? boolVal : null;

            if (targetType == typeof(double))
                return double.TryParse(value, out var doubleVal) ? doubleVal : null;

            return value;
        }
    }
}
