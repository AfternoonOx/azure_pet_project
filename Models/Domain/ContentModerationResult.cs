namespace SmartFeedbackCollector.Models.Domain
{
    public class ContentModerationResult
    {
        public bool IsContentSafe { get; set; }
        public Dictionary<string, int> CategoryScores { get; set; } = new Dictionary<string, int>();
        public List<string> FlaggedCategories { get; set; } = new List<string>();
        public int MaxSeverityLevel { get; set; }
        public string RecommendedAction { get; set; } = string.Empty;
    }
}
