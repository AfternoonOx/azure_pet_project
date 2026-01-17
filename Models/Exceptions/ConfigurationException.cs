namespace SmartFeedbackCollector.Models.Exceptions
{
    public class ConfigurationException : Exception
    {
        public List<string> MissingConfigurations { get; }
        public string UserFriendlyMessage { get; }
        public bool TriedKeyVault { get; }
        public bool TriedAppSettings { get; }

        public ConfigurationException(
            string userFriendlyMessage, 
            List<string> missingConfigurations,
            bool triedKeyVault = false,
            bool triedAppSettings = false) 
            : base(userFriendlyMessage)
        {
            UserFriendlyMessage = userFriendlyMessage;
            MissingConfigurations = missingConfigurations ?? new List<string>();
            TriedKeyVault = triedKeyVault;
            TriedAppSettings = triedAppSettings;
        }

        public ConfigurationException(string userFriendlyMessage) 
            : this(userFriendlyMessage, new List<string>())
        {
        }
    }
}
