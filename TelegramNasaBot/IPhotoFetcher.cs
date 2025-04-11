namespace TelegramNasaBot
{
    public interface IPhotoFetcher
        {
            Task<(string Url, byte[] ImageData)> FetchNasaPhotoAsync();
        }
    
}