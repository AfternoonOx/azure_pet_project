using Microsoft.AspNetCore.Mvc;
using SmartFeedbackCollector.Services.Interfaces;

namespace SmartFeedbackCollector.Controllers
{
    /// <summary>
    /// Kontroler zarządzający panelem administracyjnym, moderacją opinii i innymi zadaniami administracyjnymi.
    /// </summary>
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        /// <summary>
        /// Wyświetla główną stronę panelu administracyjnego.
        /// Pobiera i przekazuje do widoku liczbę opinii oczekujących na moderację.
        /// </summary>
        /// <returns>Widok panelu administracyjnego.</returns>
        public async Task<IActionResult> Index()
        {
            var pendingCount = await _adminService.GetPendingReviewCountAsync();
            ViewBag.PendingCount = pendingCount;
            return View();
        }

        /// <summary>
        /// Wyświetla listę opinii, które oczekują na ręczną moderację.
        /// </summary>
        /// <returns>Widok z listą opinii do moderacji.</returns>
        public async Task<IActionResult> PendingReview()
        {
            var feedback = await _adminService.GetFeedbackAwaitingReviewAsync();
            return View(feedback);
        }

        /// <summary>
        /// Wyświetla szczegóły pojedynczej opinii oczekującej na moderację.
        /// </summary>
        /// <param name="id">Identyfikator opinii.</param>
        /// <returns>Widok ze szczegółami opinii lub przekierowanie, jeśli opinia nie zostanie znaleziona.</returns>
        public async Task<IActionResult> Detail(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction(nameof(PendingReview));
            }

            var feedback = await _adminService.GetFeedbackAwaitingReviewAsync();
            var item = feedback.FirstOrDefault(f => f.Id == id);
            
            if (item == null)
            {
                TempData["ErrorMessage"] = "Feedback not found.";
                return RedirectToAction(nameof(PendingReview));
            }

            return View(item);
        }

        /// <summary>
        /// Zatwierdza opinię o podanym identyfikatorze.
        /// </summary>
        /// <param name="id">Identyfikator opinii do zatwierdzenia.</param>
        /// <param name="reviewNotes">Opcjonalne notatki moderatora.</param>
        /// <returns>Przekierowanie do listy opinii oczekujących na moderację.</returns>
        [HttpPost]
        public async Task<IActionResult> Approve(string id, string reviewNotes = "")
        {
            if (!string.IsNullOrEmpty(id))
            {
                await _adminService.ApproveFeedbackAsync(id, reviewNotes);
                TempData["SuccessMessage"] = "Feedback approved successfully.";
            }
            return RedirectToAction(nameof(PendingReview));
        }

        /// <summary>
        /// Odrzuca opinię o podanym identyfikatorze.
        /// </summary>
        /// <param name="id">Identyfikator opinii do odrzucenia.</param>
        /// <param name="reviewNotes">Opcjonalne notatki moderatora, np. powód odrzucenia.</param>
        /// <returns>Przekierowanie do listy opinii oczekujących na moderację.</returns>
        [HttpPost]
        public async Task<IActionResult> Reject(string id, string reviewNotes = "")
        {
            if (!string.IsNullOrEmpty(id))
            {
                await _adminService.RejectFeedbackAsync(id, reviewNotes);
                TempData["SuccessMessage"] = "Feedback rejected successfully.";
            }
            return RedirectToAction(nameof(PendingReview));
        }

        /// <summary>
        /// Wyświetla listę wszystkich odrzuconych opinii.
        /// </summary>
        /// <returns>Widok z listą odrzuconych opinii.</returns>
        public async Task<IActionResult> RejectedFeedback()
        {
            var feedback = await _adminService.GetRejectedFeedbackAsync();
            return View(feedback);
        }
    }
}
