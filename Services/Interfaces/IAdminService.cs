using SmartFeedbackCollector.Models.Domain;

namespace SmartFeedbackCollector.Services.Interfaces
{
    public interface IAdminService
    {
        Task<List<Feedback>> GetFeedbackAwaitingReviewAsync();
        Task<Feedback> ApproveFeedbackAsync(string id, string reviewNotes = "");
        Task<Feedback> RejectFeedbackAsync(string id, string reviewNotes = "");
        Task<List<Feedback>> GetRejectedFeedbackAsync();
        Task<int> GetPendingReviewCountAsync();
    }
}
