using Microsoft.AspNetCore.Mvc;
using SmartFeedbackCollector.Models.ViewModels;
using SmartFeedbackCollector.Services.Interfaces;
using System.Threading.Tasks;

namespace SmartFeedbackCollector.Controllers
{
    /// <summary>
    /// Kontroler obsługujący proces przesyłania i wyświetlania opinii przez użytkowników.
    /// </summary>
    public class FeedbackController : Controller
    {
        private readonly IFeedbackService _feedbackService;

        public FeedbackController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        /// <summary>
        /// Wyświetla formularz do przesyłania nowej opinii.
        /// </summary>
        /// <returns>Widok formularza opinii.</returns>
        public IActionResult Submit()
        {
            return View();
        }

        /// <summary>
        /// Przetwarza przesłany formularz opinii.
        /// Waliduje model, a następnie przekazuje treść opinii do serwisu w celu analizy i zapisu.
        /// Wyświetla odpowiedni komunikat w zależności od wyniku moderacji treści.
        /// </summary>
        /// <param name="model">Model widoku zawierający treść opinii.</param>
        /// <returns>Przekierowanie do akcji Submit z odpowiednim komunikatem.</returns>
        [HttpPost]
        public async Task<IActionResult> Submit(FeedbackViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var feedback = await _feedbackService.SubmitFeedbackAsync(model.Content);

            /*
             * W zależności od wyniku automatycznej moderacji treści (usługa Azure Content Safety),
             * użytkownik otrzymuje inny komunikat.
             * Jeśli treść jest niebezpieczna, opinia trafia do ręcznej weryfikacji.
             * Jeśli jest bezpieczna, zostaje od razu zatwierdzona.
             */
            if (!feedback.IsContentSafe)
            {
                TempData["ErrorMessage"] = "Your feedback contains content that requires review. It has been submitted for moderation.";
            }
            else
            {
                TempData["SuccessMessage"] = "Your feedback has been submitted and analyzed. Thank you!";
            }
            
            return RedirectToAction(nameof(Submit));
        }

        /// <summary>
        /// Wyświetla listę wszystkich zatwierdzonych opinii.
        /// </summary>
        /// <returns>Widok z listą zatwierdzonych opinii.</returns>
        public async Task<IActionResult> List()
        {
            var allFeedbacks = await _feedbackService.GetAllFeedbackAsync();
            var approvedFeedbacks = allFeedbacks.Where(f => f.IsApproved).ToList();
            var model = new FeedbackListViewModel { Feedbacks = approvedFeedbacks };
            return View(model);
        }

        /// <summary>
        /// Wyświetla szczegóły pojedynczej opinii.
        /// </summary>
        /// <param name="id">Identyfikator opinii.</param>
        /// <returns>Widok ze szczegółami opinii lub przekierowanie, jeśli opinia nie zostanie znaleziona.</returns>
        public async Task<IActionResult> Detail(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction(nameof(List));
            }

            var feedback = await _feedbackService.GetFeedbackByIdAsync(id);
            if (feedback == null)
            {
                TempData["ErrorMessage"] = "Feedback not found.";
                return RedirectToAction(nameof(List));
            }

            var model = FeedbackDetailViewModel.FromFeedback(feedback);
            return View(model);
        }
    }
}
