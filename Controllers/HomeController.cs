using Microsoft.AspNetCore.Mvc;
using SmartFeedbackCollector.Models;
using System.Diagnostics;

namespace SmartFeedbackCollector.Controllers
{
    /// <summary>
    /// Główny kontroler aplikacji, odpowiedzialny za serwowanie stron statycznych,
    /// takich jak strona główna, polityka prywatności oraz strona błędu.
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// Wyświetla stronę główną aplikacji.
        /// </summary>
        /// <returns>Widok strony głównej.</returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Wyświetla stronę polityki prywatności.
        /// </summary>
        /// <returns>Widok polityki prywatności.</returns>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Wyświetla stronę błędu aplikacji.
        /// Strona ta nie jest cachowana po stronie klienta.
        /// </summary>
        /// <returns>Widok błędu z identyfikatorem bieżącego żądania.</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult ConfigurationError()
        {
            var model = HttpContext.Items["ConfigurationError"] as SmartFeedbackCollector.Models.ViewModels.ConfigurationErrorViewModel;
            if (model == null)
            {
                model = new SmartFeedbackCollector.Models.ViewModels.ConfigurationErrorViewModel
                {
                    ErrorMessage = "Couldn't load configuration. Please check your credentials if they are valid.",
                    IsDevelopment = false
                };
            }
            return View(model);
        }
    }
}
