namespace TelegramNasaBot.Models
{
    public class ProxySettings
    {
        public bool UseProxy { get; set; } = false;
        public string ProxyAddress { get; set; } = string.Empty;
    }
}