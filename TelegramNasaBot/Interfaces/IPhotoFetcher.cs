using TelegramNasaBot.Models;

namespace TelegramNasaBot.Interfaces
{
    public interface IPhotoFetcher
        {
        Task<NasaPhotoData> FetchNasaPhotoAsync();
    }
    
}