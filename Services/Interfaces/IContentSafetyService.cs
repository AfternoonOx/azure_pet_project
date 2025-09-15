using SmartFeedbackCollector.Models.Domain;

namespace SmartFeedbackCollector.Services.Interfaces
{
    public interface IContentSafetyService
    {
        Task<ContentModerationResult> AnalyzeTextAsync(string text);
        Task<bool> IsContentSafeAsync(string text, int severityThreshold = 4);
    }
}
