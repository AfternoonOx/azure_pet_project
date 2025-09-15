using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SmartFeedbackCollector.Models.Configuration;
using SmartFeedbackCollector.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartFeedbackCollector.Services
{
    /// <summary>
    /// Serwis integrujący się z usługą Azure Cognitive Service for Language.
    /// Wyniki są cachowane w pamięci, aby unikać wielokrotnego analizowania tej samej treści.
    /// Odpowiada za zaawansowaną analizę tekstu, w tym:
    /// - Analizę sentymentu (pozytywny, negatywny, neutralny).
    /// - Ekstrakcję kluczowych fraz.
    /// - Wykrywanie języka.
    /// </summary>
    public class AzureSentimentService : ISentimentService
    {
        private readonly TextAnalyticsClient _client;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

        public AzureSentimentService(IOptions<CognitiveServicesOptions> options, IMemoryCache cache)
        {
            _cache = cache;
            
            var clientOptions = new TextAnalyticsClientOptions 
            { 
                Retry = { Delay = TimeSpan.FromSeconds(2), MaxRetries = 3 }
            };
            
            var credentials = new AzureKeyCredential(options.Value.TextAnalyticsKey);
            _client = new TextAnalyticsClient(new Uri(options.Value.TextAnalyticsEndpoint), credentials, clientOptions);
        }

        /// <summary>
        /// Analizuje sentyment podanego tekstu.
        /// </summary>
        /// <param name="text">Tekst do analizy.</param>
        /// <returns>Krotka zawierająca wynik pewności (score) i kategorię sentymentu (np. "Positive").</returns>
        public async Task<(double Score, string Category)> AnalyzeSentimentAsync(string text)
        {
            var cacheKey = $"sentiment_{text.GetHashCode()}";
            if (_cache.TryGetValue(cacheKey, out (double Score, string Category) result))
            {
                return result;
            }
            
            try
            {
                var response = await _client.AnalyzeSentimentAsync(text);
                var sentiment = response.Value;

                var score = sentiment.ConfidenceScores.Positive;
                string category;

                if (sentiment.Sentiment == TextSentiment.Positive)
                {
                    category = "Positive";
                }
                else if (sentiment.Sentiment == TextSentiment.Negative)
                {
                    category = "Negative";
                    score = sentiment.ConfidenceScores.Negative;
                }
                else
                {
                    category = "Neutral";
                    score = sentiment.ConfidenceScores.Neutral;
                }

                result = (score, category);
                _cache.Set(cacheKey, result, CacheDuration);
                return result;
            }
            catch (Exception ex)
            {
                // W przypadku błędu, zwracamy neutralny sentyment, aby nie przerywać procesu.
                Console.WriteLine($"Text Analytics Error: {ex.Message}");
                return (0.5, "Neutral");
            }
        }

        /// <summary>
        /// Wyodrębnia kluczowe frazy z podanego tekstu.
        /// </summary>
        /// <param name="text">Tekst do analizy.</param>
        /// <returns>Lista zidentyfikowanych fraz kluczowych.</returns>
        public async Task<List<string>> ExtractKeyPhrasesAsync(string text)
        {
            var cacheKey = $"keyphrases_{text.GetHashCode()}";
            if (_cache.TryGetValue(cacheKey, out List<string> cachedKeyPhrases))
            {
                return cachedKeyPhrases;
            }
            
            try
            {
                var response = await _client.ExtractKeyPhrasesAsync(text);
                var keyPhrases = response.Value.ToList();
                _cache.Set(cacheKey, keyPhrases, CacheDuration);
                return keyPhrases;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Key Phrase Extraction Error: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Wykrywa język, w którym napisany jest podany tekst.
        /// </summary>
        /// <param name="text">Tekst do analizy.</param>
        /// <returns>Nazwa wykrytego języka (np. "English") lub "Unknown" w przypadku błędu.</returns>
        public async Task<string> DetectLanguageAsync(string text)
        {
            var cacheKey = $"language_{text.GetHashCode()}";
            if (_cache.TryGetValue(cacheKey, out string cachedLanguage))
            {
                return cachedLanguage;
            }
            
            try
            {
                var response = await _client.DetectLanguageAsync(text);
                var detectedLanguage = response.Value.Name;
                _cache.Set(cacheKey, detectedLanguage, CacheDuration);
                return detectedLanguage;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Language Detection Error: {ex.Message}");
                return "Unknown";
            }
        }
    }
}
