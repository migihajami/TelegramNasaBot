using Microsoft.Extensions.Options;
using Serilog;
using System.Text.Json;
using TelegramNasaBot.Interfaces;
using TelegramNasaBot.Models;

namespace TelegramNasaBot.Services
{
    public class PhotoFetcher : IPhotoFetcher
    {
        private readonly HttpClient _httpClient;
        private readonly NasaSettings _settings;
        private readonly ILogger _logger;

        public PhotoFetcher(
            HttpClient httpClient,
            IOptions<NasaSettings> settings,
            ILogger logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<NasaPhotoData> FetchNasaPhotoAsync()
        {
            _logger.Information("Fetching NASA photo of the day.");

            try
            {
                var response = await _httpClient.GetAsync(
                    $"{_settings.ApiUrl}?api_key={_settings.ApiKey}");

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(json);

                var root = document.RootElement;
                var url = root.GetProperty("url").GetString() ?? string.Empty;
                var title = root.GetProperty("title").GetString() ?? string.Empty;
                var explanation = root.GetProperty("explanation").GetString() ?? string.Empty;

                _logger.Information("Downloading NASA photo from {Url}", url);
                var imageResponse = await _httpClient.GetAsync(url);
                imageResponse.EnsureSuccessStatusCode();

                var imageData = await imageResponse.Content.ReadAsByteArrayAsync();

                _logger.Information("NASA photo fetched successfully.");
                return new NasaPhotoData
                {
                    Url = url,
                    ImageData = imageData,
                    Title = title,
                    Explanation = explanation
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to fetch NASA photo.");
                throw;
            }
        }
    }
}