using SmartFeedbackCollector.Models.Domain;
using SmartFeedbackCollector.Repositories.Interfaces;
using SmartFeedbackCollector.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartFeedbackCollector.Services
{
    /// <summary>
    /// Główny serwis aplikacji, orkiestrujący proces przetwarzania nowej opinii.
    /// Współpracuje z serwisami do analizy treści i sentymentu oraz z repozytorium do zapisu danych.
    /// </summary>
    public class FeedbackService : IFeedbackService
    {
        private readonly IFeedbackRepository _repository;
        private readonly ISentimentService _sentimentService;
        private readonly IContentSafetyService _contentSafetyService;

        public FeedbackService(IFeedbackRepository repository, ISentimentService sentimentService, IContentSafetyService contentSafetyService)
        {
            _repository = repository;
            _sentimentService = sentimentService;
            _contentSafetyService = contentSafetyService;
        }

        /// <summary>
        /// Pobiera wszystkie opinie z repozytorium.
        /// </summary>
        /// <returns>Lista wszystkich opinii.</returns>
        public async Task<List<Feedback>> GetAllFeedbackAsync()
        {
            return await _repository.GetAllFeedbackAsync();
        }

        /// <summary>
        /// Pobiera pojedynczą opinię na podstawie jej identyfikatora.
        /// </summary>
        /// <param name="id">Identyfikator opinii.</param>
        /// <returns>Obiekt opinii lub null, jeśli nie zostanie znaleziona.</returns>
        public async Task<Feedback> GetFeedbackByIdAsync(string id)
        {
            return await _repository.GetFeedbackByIdAsync(id);
        }

        /// <summary>
        /// Przetwarza i zapisuje nową opinię.
        /// 1. Analizuje treść pod kątem bezpieczeństwa (Azure Content Safety).
        /// 2. Jeśli treść jest bezpieczna, wykonuje analizę sentymentu, ekstrakcję fraz i wykrywanie języka.
        /// 3. Jeśli treść jest niebezpieczna, oznacza ją jako wymagającą ręcznej moderacji.
        /// 4. Zapisuje obiekt opinii w repozytorium.
        /// </summary>
        /// <param name="content">Treść opinii przesłana przez użytkownika.</param>
        /// <returns>Zapisany obiekt opinii z wynikami analizy.</returns>
        public async Task<Feedback> SubmitFeedbackAsync(string content)
        {
            // Krok 1: Analiza bezpieczeństwa treści.
            var moderationResult = await _contentSafetyService.AnalyzeTextAsync(content);
            
            var feedback = new Feedback
            {
                Id = Guid.NewGuid().ToString(),
                Content = content,
                SubmissionTime = DateTime.UtcNow,
                IsContentSafe = moderationResult.IsContentSafe,
                SeverityLevel = moderationResult.MaxSeverityLevel,
                FlaggedCategories = moderationResult.FlaggedCategories,
                // Ustawienie flag w zależności od wyniku moderacji.
                RequiresReview = !moderationResult.IsContentSafe,
                IsApproved = moderationResult.IsContentSafe
            };

            /*
             * Krok 2: Wykonanie pełnej analizy kognitywnej tylko wtedy,
             * gdy treść została uznana za bezpieczną.
             * Oszczędza to zasoby i koszty, nie analizując sentymentu w opiniach,
             * które i tak prawdopodobnie zostaną odrzucone.
             */
            if (moderationResult.IsContentSafe)
            {
                var (score, category) = await _sentimentService.AnalyzeSentimentAsync(content);
                var keyPhrases = await _sentimentService.ExtractKeyPhrasesAsync(content);
                var language = await _sentimentService.DetectLanguageAsync(content);

                feedback.SentimentScore = score;
                feedback.SentimentCategory = category;
                feedback.KeyPhrases = keyPhrases;
                feedback.Language = language;
            }
            else
            {
                // Jeśli treść jest niebezpieczna, zapisujemy tylko kategorie naruszeń.
                feedback.ModerationCategory = string.Join(", ", moderationResult.FlaggedCategories);
            }

            // Krok 3: Zapis w bazie danych.
            return await _repository.AddFeedbackAsync(feedback);
        }
    }
}
