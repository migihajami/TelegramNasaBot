namespace TelegramNasaBot.Models
{
    public class OpenAiSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string AssistantId { get; set; } = string.Empty;
        public string ApiUrl { get; set; } = string.Empty;
        public int RunPollingIntervalSeconds { get; set; } = 1;
        public int RunMaxWaitSeconds { get; set; } = 30;
    }
}