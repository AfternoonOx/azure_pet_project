using Azure;
using Azure.AI.ContentSafety;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SmartFeedbackCollector.Models.Configuration;
using SmartFeedbackCollector.Models.Domain;
using SmartFeedbackCollector.Services.Interfaces;

namespace SmartFeedbackCollector.Services
{
    /// <summary>
    /// Serwis integrujący się z usługą Azure Content Safety.
    /// Odpowiada za analizę tekstu pod kątem potencjalnie niebezpiecznych treści,
    /// takich jak nienawiść, przemoc, treści dla dorosłych itp.
    /// </summary>
    public class AzureContentSafetyService : IContentSafetyService
    {
        private readonly ContentSafetyClient _client;
        private readonly IMemoryCache _cache;
        private readonly ContentSafetyOptions _options;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

        public AzureContentSafetyService(IOptions<ContentSafetyOptions> options, IMemoryCache cache)
        {
            _cache = cache;
            _options = options.Value;
            
            var credentials = new AzureKeyCredential(_options.Key);
            _client = new ContentSafetyClient(new Uri(_options.Endpoint), credentials);
        }

        /// <summary>
        /// Analizuje podany tekst w poszukiwaniu niebezpiecznych treści.
        /// Wyniki są cachowane w pamięci, aby unikać wielokrotnego analizowania tej samej treści.
        /// </summary>
        /// <param name="text">Tekst do analizy.</param>
        /// <returns>Obiekt ContentModerationResult zawierający szczegółowe wyniki analizy.</returns>
        public async Task<ContentModerationResult> AnalyzeTextAsync(string text)
        {
            /*
             * Użycie IMemoryCache do przechowywania wyników analizy dla identycznych tekstów.
             * Klucz jest generowany na podstawie hash code'u tekstu.
             * Zmniejsza to liczbę zapytań do API Azure i obniża koszty oraz opóźnienia.
             */
            var cacheKey = $"content_safety_{text.GetHashCode()}";
            
            if (_cache.TryGetValue(cacheKey, out ContentModerationResult cachedResult))
            {
                return cachedResult;
            }

            try
            {
                var request = new AnalyzeTextOptions(text);
                var response = await _client.AnalyzeTextAsync(request);
                var analysis = response.Value;

                var result = new ContentModerationResult();
                var maxSeverity = 0;

                // Iteracja przez wszystkie kategorie analizy zwrócone przez API
                foreach (var category in analysis.CategoriesAnalysis)
                {
                    var severity = category.Severity ?? 0;
                    var categoryName = category.Category.ToString();
                    
                    result.CategoryScores[categoryName] = severity;
                    
                    if (severity > maxSeverity)
                    {
                        maxSeverity = severity;
                    }
                    
                    // Jeśli poziom "ostrości" (severity) przekracza zdefiniowany próg, kategoria jest oflagowana.
                    if (severity >= _options.SeverityThreshold)
                    {
                        result.FlaggedCategories.Add(categoryName);
                    }
                }

                result.MaxSeverityLevel = maxSeverity;
                result.IsContentSafe = result.FlaggedCategories.Count == 0;
                result.RecommendedAction = result.IsContentSafe ? "Accept" : "Review";

                _cache.Set(cacheKey, result, CacheDuration);
                return result;
            }
            catch (Exception ex)
            {
                // W przypadku błędu komunikacji z API, domyślnie uznajemy treść za bezpieczną,
                // aby nie blokować przesyłania opinii. Logujemy błąd do konsoli.
                Console.WriteLine($"Content Safety Error: {ex.Message}");
                
                return new ContentModerationResult
                {
                    IsContentSafe = true,
                    RecommendedAction = "Accept"
                };
            }
        }

        /// <summary>
        /// Szybko sprawdza, czy treść jest bezpieczna, porównując maksymalny poziom "ostrości"
        /// z podanym progiem.
        /// </summary>
        /// <param name="text">Tekst do sprawdzenia.</param>
        /// <param name="severityThreshold">Próg, powyżej którego treść jest uznawana za niebezpieczną.</param>
        /// <returns>True, jeśli treść jest bezpieczna; w przeciwnym razie false.</returns>
        public async Task<bool> IsContentSafeAsync(string text, int severityThreshold = 4)
        {
            var result = await AnalyzeTextAsync(text);
            return result.MaxSeverityLevel < severityThreshold;
        }
    }
}
