using SmartFeedbackCollector.Models.Domain;
using SmartFeedbackCollector.Models.ViewModels;
using SmartFeedbackCollector.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartFeedbackCollector.Services
{
    /// <summary>
    /// Serwis odpowiedzialny za agregowanie i przygotowywanie danych
    /// do wyświetlenia na pulpicie nawigacyjnym (dashboard).
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly IFeedbackService _feedbackService;

        public DashboardService(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        /// <summary>
        /// Pobiera wszystkie opinie i przetwarza je w celu utworzenia modelu widoku dla dashboardu.
        /// </summary>
        /// <returns>Obiekt DashboardViewModel zawierający zagregowane dane.</returns>
        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            var feedbackList = await _feedbackService.GetAllFeedbackAsync();

            var dashboard = new DashboardViewModel
            {
                TotalFeedbackCount = feedbackList.Count,
                PositiveFeedbackCount = feedbackList.Count(f => f.SentimentCategory == "Positive"),
                NeutralFeedbackCount = feedbackList.Count(f => f.SentimentCategory == "Neutral"),
                NegativeFeedbackCount = feedbackList.Count(f => f.SentimentCategory == "Negative"),
                SentimentByDay = GetSentimentByDay(feedbackList),
                LanguageDistribution = GetLanguageDistribution(feedbackList),
                TopKeyPhrases = GetTopKeyPhrases(feedbackList, 10)
            };

            return dashboard;
        }

        /// <summary>
        /// Grupuje opinie według daty ich przesłania.
        /// </summary>
        /// <param name="feedbackList">Lista opinii do przetworzenia.</param>
        /// <returns>Słownik, gdzie kluczem jest data (w formacie "yyyy-MM-dd"), a wartością jest liczba opinii z danego dnia.</returns>
        private Dictionary<string, int> GetSentimentByDay(List<Feedback> feedbackList)
        {
            return feedbackList
                .GroupBy(f => f.SubmissionTime.Date)
                .OrderBy(g => g.Key)
                .ToDictionary(
                    g => g.Key.ToString("yyyy-MM-dd"),
                    g => g.Count()
                );
        }
        
        /// <summary>
        /// Oblicza rozkład języków, w których zostały napisane opinie.
        /// </summary>
        /// <param name="feedbackList">Lista opinii do przetworzenia.</param>
        /// <returns>Słownik, gdzie kluczem jest nazwa języka, a wartością jest liczba opinii w tym języku.</returns>
        private Dictionary<string, int> GetLanguageDistribution(List<Feedback> feedbackList)
        {
            return feedbackList
                .Where(f => !string.IsNullOrEmpty(f.Language))
                .GroupBy(f => f.Language)
                .OrderByDescending(g => g.Count())
                .ToDictionary(
                    g => g.Key,
                    g => g.Count()
                );
        }
        
        /// <summary>
        /// Identyfikuje najczęściej występujące frazy kluczowe we wszystkich opiniach.
        /// </summary>
        /// <param name="feedbackList">Lista opinii do przetworzenia.</param>
        /// <param name="count">Liczba najpopularniejszych fraz do zwrócenia.</param>
        /// <returns>Słownik zawierający najpopularniejsze frazy kluczowe i liczbę ich wystąpień.</returns>
        private Dictionary<string, int> GetTopKeyPhrases(List<Feedback> feedbackList, int count)
        {
            var allPhrases = new Dictionary<string, int>();
            
            foreach (var feedback in feedbackList)
            {
                if (feedback.KeyPhrases == null) continue;
                
                foreach (var phrase in feedback.KeyPhrases)
                {
                    if (allPhrases.ContainsKey(phrase))
                    {
                        allPhrases[phrase]++;
                    }
                    else
                    {
                        allPhrases[phrase] = 1;
                    }
                }
            }
            
            return allPhrases
                .OrderByDescending(kv => kv.Value)
                .Take(count)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }
}
