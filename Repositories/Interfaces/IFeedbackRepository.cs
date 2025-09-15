using SmartFeedbackCollector.Models.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartFeedbackCollector.Repositories.Interfaces
{
    public interface IFeedbackRepository
    {
        Task<Feedback> AddFeedbackAsync(Feedback feedback);
        Task<List<Feedback>> GetAllFeedbackAsync();
        Task<Feedback> GetFeedbackByIdAsync(string id);
        Task<Feedback> UpdateFeedbackAsync(Feedback feedback);
    }
}
