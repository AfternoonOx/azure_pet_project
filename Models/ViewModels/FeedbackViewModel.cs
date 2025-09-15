using System.ComponentModel.DataAnnotations;

namespace SmartFeedbackCollector.Models.ViewModels
{
    public class FeedbackViewModel
    {
        [Required]
        [MinLength(10)]
        [MaxLength(1000)]
        public string Content { get; set; }
    }
}
