using Microsoft.AspNetCore.Mvc;
using SmartFeedbackCollector.Services.Interfaces;
using System.Threading.Tasks;

namespace SmartFeedbackCollector.Controllers
{
    /// <summary>
    /// Kontroler odpowiedzialny za wyświetlanie pulpitu nawigacyjnego (dashboard) z agregowanymi danymi o opiniach.
    /// </summary>
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Wyświetla główny widok pulpitu nawigacyjnego.
        /// Pobiera przetworzone dane, takie jak analiza sentymentu i liczba opinii,
        /// a następnie przekazuje je do widoku.
        /// </summary>
        /// <returns>Widok pulpitu nawigacyjnego z modelem zawierającym zagregowane dane.</returns>
        public async Task<IActionResult> Index()
        {
            var model = await _dashboardService.GetDashboardDataAsync();
            return View(model);
        }
    }
}
