using Microsoft.Extensions.Options;
using Serilog;
using System.Net.Http.Json;
using TelegramNasaBot.Interfaces;
using TelegramNasaBot.Models;

namespace TelegramNasaBot.Services
{
    public class PhotoFetcher : IPhotoFetcher
    {
        private readonly HttpClient _httpClient;
        private readonly NasaSettings _nasaSettings;
        private readonly ILogger _logger;

        public PhotoFetcher(HttpClient httpClient, IOptions<NasaSettings> nasaSettings, ILogger logger)
        {
            _httpClient = httpClient;
            _nasaSettings = nasaSettings.Value;
            _logger = logger;
        }

        public async Task<(string Url, byte[] ImageData)> FetchNasaPhotoAsync()
        {
            try
            {
                _logger.Information("Fetching NASA Astronomy Picture of the Day...");

                // Call NASA APOD API
                var url = $"{_nasaSettings.ApiUrl}?api_key={_nasaSettings.ApiKey}";
                var response = await _httpClient.GetFromJsonAsync<NasaApodResponse>(url);

                if (response == null || string.IsNullOrEmpty(response.HdUrl))
                {
                    _logger.Error("Failed to retrieve valid NASA APOD data.");
                    throw new Exception("No valid photo data returned from NASA API.");
                }

                _logger.Information("NASA photo URL retrieved: {Url}", response.HdUrl);

                // Download the image
                var imageData = await _httpClient.GetByteArrayAsync(response.HdUrl);
                _logger.Information("NASA photo downloaded successfully, size: {Size} bytes", imageData.Length);

                return (response.HdUrl, imageData);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error fetching NASA photo.");
                throw;
            }
        }
    }
}