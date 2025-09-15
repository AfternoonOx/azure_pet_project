using SmartFeedbackCollector.Models.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartFeedbackCollector.Services.Interfaces
{
    public interface IFeedbackService
    {
        Task<Feedback> SubmitFeedbackAsync(string content);
        Task<List<Feedback>> GetAllFeedbackAsync();
        Task<Feedback> GetFeedbackByIdAsync(string id);
    }
}
