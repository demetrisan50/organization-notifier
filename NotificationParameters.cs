namespace organization_notifier
{
    public class NotificationParameters
    {
        public string Title { get; set; } = "Notification";
        public string Body { get; set; } = "Message content";
        public string Duration { get; set; } = "Short"; // Short or Long
        public string AppId { get; set; } = "Organization Notifier";
        public string IconPath { get; set; } = string.Empty;
    }

    public class AppConfig
    {
        public string ImageCachePath { get; set; } = string.Empty;
        public string ScriptPath { get; set; } = string.Empty;
        public bool IsDebugMode { get; set; } = false;
    }
}
