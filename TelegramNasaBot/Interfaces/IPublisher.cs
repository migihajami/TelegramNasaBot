namespace TelegramNasaBot.Interfaces
{
    public interface IPublisher
    {
        Task PublishPhotoAsync(byte[] imageData, string caption);
    }
}