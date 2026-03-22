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

    public class Scenario
    {
        public string Name { get; set; } = "Empty Slot";
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Duration { get; set; } = "Long";
        public string AppId { get; set; } = "IT Support Team";
        public string IconPath { get; set; } = string.Empty;
    }

    public class AppConfig
    {
        public string ImageCachePath { get; set; } = string.Empty;
        public string ScriptPath { get; set; } = string.Empty;
        public bool IsDebugMode { get; set; } = false;
        public System.Collections.Generic.List<Scenario> Scenarios { get; set; } = new System.Collections.Generic.List<Scenario>();
    }
}
