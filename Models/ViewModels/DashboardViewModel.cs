using System.Collections.Generic;

namespace SmartFeedbackCollector.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalFeedbackCount { get; set; }
        public int PositiveFeedbackCount { get; set; }
        public int NeutralFeedbackCount { get; set; }
        public int NegativeFeedbackCount { get; set; }
        public Dictionary<string, int> SentimentByDay { get; set; }
        public Dictionary<string, int> LanguageDistribution { get; set; }
        public Dictionary<string, int> TopKeyPhrases { get; set; }
    }
}
