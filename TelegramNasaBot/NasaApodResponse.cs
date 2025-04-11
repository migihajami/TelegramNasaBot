namespace TelegramNasaBot
{
    // NASA APOD API response model
    public class NasaApodResponse
    {
        public string HdUrl { get; set; }
        public string Title { get; set; }
        public string Explanation { get; set; }
    }
}