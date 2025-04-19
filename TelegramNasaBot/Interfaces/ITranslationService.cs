namespace TelegramNasaBot.Interfaces
{
    public interface ITranslationService
    {
        Task<string> TranslateToLanguageAsync(string text, string languageCode);
    }
}