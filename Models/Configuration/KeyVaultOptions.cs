namespace SmartFeedbackCollector.Models.Configuration
{
    public class KeyVaultOptions
    {
        public bool Enabled { get; set; } = false;
        public string VaultUri { get; set; } = string.Empty;
    }
}
