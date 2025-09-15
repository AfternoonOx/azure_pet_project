namespace SmartFeedbackCollector.Models.Configuration
{
    public class AzureStorageOptions
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string ContainerName { get; set; }
    }
}
