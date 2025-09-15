using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartFeedbackCollector.Services.Interfaces
{
    public interface ISentimentService
    {
        Task<(double Score, string Category)> AnalyzeSentimentAsync(string text);
        Task<List<string>> ExtractKeyPhrasesAsync(string text);
        Task<string> DetectLanguageAsync(string text);
    }
}
