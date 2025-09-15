using SmartFeedbackCollector.Models.ViewModels;
using System.Threading.Tasks;

namespace SmartFeedbackCollector.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync();
    }
}
