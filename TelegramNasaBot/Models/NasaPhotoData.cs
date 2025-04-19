namespace TelegramNasaBot.Models
{
    public class NasaPhotoData
    {
        public string Url { get; set; } = string.Empty;
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public string Title { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
    }
}