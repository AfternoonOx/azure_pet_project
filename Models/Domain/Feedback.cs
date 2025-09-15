using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SmartFeedbackCollector.Models.Domain
{
    public class Feedback
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string Content { get; set; }
        public DateTime SubmissionTime { get; set; }
        public double SentimentScore { get; set; }
        public string SentimentCategory { get; set; }
        public List<string> KeyPhrases { get; set; }
        public string Language { get; set; }
        public bool IsContentSafe { get; set; } = true;
        public string ModerationCategory { get; set; } = string.Empty;
        public int SeverityLevel { get; set; } = 0;
        public List<string> FlaggedCategories { get; set; } = new List<string>();
        public bool RequiresReview { get; set; } = false;
        public bool IsApproved { get; set; } = true;
        public string ReviewNotes { get; set; } = string.Empty;
    }
}
