namespace TelegramNasaBot.Interfaces
{
    public interface IPhotoFetcher
        {
            Task<(string Url, byte[] ImageData)> FetchNasaPhotoAsync();
        }
    
}