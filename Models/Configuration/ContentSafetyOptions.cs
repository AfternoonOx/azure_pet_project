namespace SmartFeedbackCollector.Models.Configuration
{
    public class ContentSafetyOptions
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public int SeverityThreshold { get; set; } = 4;
    }
}
