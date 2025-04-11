namespace TelegramNasaBot
{
    public interface IQrCodeGenerator
    {
        Task<byte[]> AddQrCodeToImageAsync(byte[] imageData, string channelLink);
    }
}