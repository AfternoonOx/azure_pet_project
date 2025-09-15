using SmartFeedbackCollector.Models.Domain;
using SmartFeedbackCollector.Repositories.Interfaces;
using SmartFeedbackCollector.Services.Interfaces;

namespace SmartFeedbackCollector.Services
{
    /// <summary>
    /// Serwis implementujący logikę biznesową dla panelu administracyjnego.
    /// Odpowiada za operacje związane z moderacją opinii, takie jak ich zatwierdzanie, odrzucanie
    /// oraz pobieranie list opinii o określonym statusie.
    /// </summary>
    public class AdminService : IAdminService
    {
        private readonly IFeedbackRepository _repository;
        private readonly ISentimentService _sentimentService;

        public AdminService(IFeedbackRepository repository, ISentimentService sentimentService)
        {
            _repository = repository;
            _sentimentService = sentimentService;
        }

        /// <summary>
        /// Pobiera listę opinii, które zostały oznaczone jako wymagające ręcznej moderacji.
        /// </summary>
        /// <returns>Lista opinii oczekujących na weryfikację.</returns>
        public async Task<List<Feedback>> GetFeedbackAwaitingReviewAsync()
        {
            var allFeedback = await _repository.GetAllFeedbackAsync();
            return allFeedback.Where(f => f.RequiresReview && !f.IsApproved).ToList();
        }

        /// <summary>
        /// Zatwierdza opinię o podanym identyfikatorze.
        /// Zmienia status opinii na zatwierdzony i opcjonalnie dodaje notatki moderatora.
        /// Jeśli opinia nie miała wcześniej przeprowadzonej analizy sentymentu
        /// analiza jest wykonywana w tym momencie.
        /// </summary>
        /// <param name="id">Identyfikator opinii.</param>
        /// <param name="reviewNotes">Opcjonalne notatki od moderatora.</param>
        /// <returns>Zaktualizowany obiekt opinii.</returns>
        public async Task<Feedback> ApproveFeedbackAsync(string id, string reviewNotes = "")
        {
            var feedback = await _repository.GetFeedbackByIdAsync(id);
            if (feedback != null)
            {
                feedback.IsApproved = true;
                feedback.RequiresReview = false;
                feedback.ReviewNotes = reviewNotes;


                if (string.IsNullOrEmpty(feedback.SentimentCategory))
                {
                    var (score, category) = await _sentimentService.AnalyzeSentimentAsync(feedback.Content);
                    var keyPhrases = await _sentimentService.ExtractKeyPhrasesAsync(feedback.Content);
                    var language = await _sentimentService.DetectLanguageAsync(feedback.Content);

                    feedback.SentimentScore = score;
                    feedback.SentimentCategory = category;
                    feedback.KeyPhrases = keyPhrases;
                    feedback.Language = language;
                }

                await _repository.UpdateFeedbackAsync(feedback);
            }
            return feedback;
        }

        /// <summary>
        /// Odrzuca opinię o podanym identyfikatorze.
        /// Zmienia status opinii na "niezatwierdzony" i zapisuje notatki moderatora.
        /// </summary>
        /// <param name="id">Identyfikator opinii.</param>
        /// <param name="reviewNotes">Notatki moderatora, np. powód odrzucenia.</param>
        /// <returns>Zaktualizowany obiekt opinii.</returns>
        public async Task<Feedback> RejectFeedbackAsync(string id, string reviewNotes = "")
        {
            var feedback = await _repository.GetFeedbackByIdAsync(id);
            if (feedback != null)
            {
                feedback.IsApproved = false;
                feedback.RequiresReview = false;
                feedback.ReviewNotes = reviewNotes;
                await _repository.UpdateFeedbackAsync(feedback);
            }
            return feedback;
        }

        /// <summary>
        /// Pobiera listę wszystkich odrzuconych opinii.
        /// </summary>
        /// <returns>Lista odrzuconych opinii.</returns>
        public async Task<List<Feedback>> GetRejectedFeedbackAsync()
        {
            var allFeedback = await _repository.GetAllFeedbackAsync();
            // Opinia jest odrzucona, jeśli nie jest zatwierdzona i nie oczekuje już na moderację.
            return allFeedback.Where(f => !f.IsApproved && !f.RequiresReview).ToList();
        }

        /// <summary>
        /// Zwraca liczbę opinii oczekujących na moderację.
        /// </summary>
        /// <returns>Liczba opinii do weryfikacji.</returns>
        public async Task<int> GetPendingReviewCountAsync()
        {
            var pending = await GetFeedbackAwaitingReviewAsync();
            return pending.Count;
        }
    }
}
