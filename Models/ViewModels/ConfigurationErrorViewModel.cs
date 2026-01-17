namespace SmartFeedbackCollector.Models.ViewModels
{
    public class ConfigurationErrorViewModel
    {
        public List<string> MissingConfigurations { get; set; } = new List<string>();
        public bool IsDevelopment { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public bool TriedKeyVault { get; set; }
        public bool TriedAppSettings { get; set; }
    }
}
