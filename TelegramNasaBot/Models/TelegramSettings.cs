namespace TelegramNasaBot.Models
{
    public class TelegramSettings
    {
        public string BotToken { get; set; }
        public string ChannelId { get; set; }
        public string QrCodeText { get; set; }
        public int MaxImageDimension { get; set; } = 4000;
        public int QrCodeModuleSize { get; set; } = 20;
        public int QrCodePadding { get; set; } = 20;
        public double QrCodeMaxSizePercentage { get; set; } = 20.0; // Default: 20%
    }
}