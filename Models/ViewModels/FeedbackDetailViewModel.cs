using SmartFeedbackCollector.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartFeedbackCollector.Models.ViewModels
{
    public class FeedbackDetailViewModel
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public DateTime SubmissionTime { get; set; }
        public double SentimentScore { get; set; }
        public string SentimentCategory { get; set; }
        public List<string> KeyPhrases { get; set; }
        public string Language { get; set; }
        public bool IsContentSafe { get; set; }
        public string ModerationCategory { get; set; }
        public int SeverityLevel { get; set; }
        public List<string> FlaggedCategories { get; set; }
        public bool RequiresReview { get; set; }
        public bool IsApproved { get; set; }
        public string ReviewNotes { get; set; }
        
        // Additional visual properties for the detail view
        public string SentimentColorClass => SentimentCategory == "Positive" ? "text-success" : 
                                           SentimentCategory == "Negative" ? "text-danger" : "text-warning";
        
        public string SentimentDescription => SentimentCategory == "Positive" ? "This feedback expresses a positive sentiment." : 
                                            SentimentCategory == "Negative" ? "This feedback expresses a negative sentiment." : 
                                            "This feedback expresses a neutral sentiment.";
        
        public string FormattedTime => SubmissionTime.ToString("dddd, MMMM d, yyyy 'at' h:mm tt");
        
        public string FormattedScore => SentimentScore.ToString("P0");
        
        public string FormattedLanguage => string.IsNullOrEmpty(Language) ? "Unknown" : 
                                          Language == "English" ? "English" : $"{Language}";
        
        public bool HasKeyPhrases => KeyPhrases != null && KeyPhrases.Any();
        
        public bool HasModerationIssues => !IsContentSafe || FlaggedCategories?.Any() == true;
        
        public string ModerationStatusClass => IsContentSafe ? "text-success" : "text-danger";
        
        public string ModerationStatusText => IsContentSafe ? "Safe" : "Flagged";
        
        public string ApprovalStatusClass => IsApproved ? "text-success" : RequiresReview ? "text-warning" : "text-danger";
        
        public string ApprovalStatusText => IsApproved ? "Approved" : RequiresReview ? "Pending Review" : "Rejected";
        
        // Factory method to create from Feedback entity
        public static FeedbackDetailViewModel FromFeedback(Feedback feedback)
        {
            if (feedback == null) return null;
            
            return new FeedbackDetailViewModel
            {
                Id = feedback.Id,
                Content = feedback.Content,
                SubmissionTime = feedback.SubmissionTime,
                SentimentScore = feedback.SentimentScore,
                SentimentCategory = feedback.SentimentCategory,
                KeyPhrases = feedback.KeyPhrases,
                Language = feedback.Language,
                IsContentSafe = feedback.IsContentSafe,
                ModerationCategory = feedback.ModerationCategory,
                SeverityLevel = feedback.SeverityLevel,
                FlaggedCategories = feedback.FlaggedCategories,
                RequiresReview = feedback.RequiresReview,
                IsApproved = feedback.IsApproved,
                ReviewNotes = feedback.ReviewNotes
            };
        }
    }
}
