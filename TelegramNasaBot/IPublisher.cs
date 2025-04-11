namespace TelegramNasaBot
{
    public interface IPublisher
    {
        Task PublishPhotoAsync(byte[] imageData, string caption);
    }
}