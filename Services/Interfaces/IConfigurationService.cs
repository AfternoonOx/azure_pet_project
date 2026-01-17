namespace SmartFeedbackCollector.Services.Interfaces
{
    public interface IConfigurationService
    {
        Task<T> GetConfigurationAsync<T>(string sectionName) where T : class, new();
        Task<bool> ValidateAllConfigurationsAsync();
    }
}
