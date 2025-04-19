using Quartz;
using Serilog;
using System;
using System.Globalization;
using System.Threading.Tasks;
using TelegramNasaBot.Interfaces;
using TelegramNasaBot.Models;

namespace TelegramNasaBot.Services
{
    public class NasaPhotoJob : IJob
    {
        private const int TELEGRAM_CAPTION_LIMIT = 1024;
        private const string TRUNCATION_SUFFIX = "...";

        private readonly IPhotoFetcher _photoFetcher;
        private readonly IQrCodeGenerator _qrCodeGenerator;
        private readonly IPublisher _publisher;
        private readonly ITranslationService _translationService;
        private readonly ILogger _logger;
        private readonly TelegramSettings _telegramSettings;

        public NasaPhotoJob(
            IPhotoFetcher photoFetcher,
            IQrCodeGenerator qrCodeGenerator,
            IPublisher publisher,
            ITranslationService translationService,
            ILogger logger,
            TelegramSettings telegramSettings)
        {
            _photoFetcher = photoFetcher;
            _qrCodeGenerator = qrCodeGenerator;
            _publisher = publisher;
            _translationService = translationService;
            _logger = logger;
            _telegramSettings = telegramSettings;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.Information("Starting NASA photo job execution.");

            try
            {
                var photoData = await _photoFetcher.FetchNasaPhotoAsync();
                var imageWithQrCode = await _qrCodeGenerator.AddQrCodeToImageAsync(
                    photoData.ImageData,
                    _telegramSettings.ChannelId);

                var caption = await PrepareCaptionAsync(photoData);

                await _publisher.PublishPhotoAsync(imageWithQrCode, caption);

                _logger.Information("NASA photo job completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "NASA photo job failed.");
                throw new JobExecutionException(ex);
            }
        }

        // Prepares the caption by translating title and explanation or using fallback
        private async Task<string> PrepareCaptionAsync(NasaPhotoData photoData)
        {
            var originalText = $"{photoData.Title}\n\n{photoData.Explanation}";

            try
            {
                var translatedText = await _translationService.TranslateToLanguageAsync(originalText, "ru");

                // Check if translated text exceeds Telegram's caption limit
                if (translatedText.Length > TELEGRAM_CAPTION_LIMIT)
                {
                    _logger.Warning(
                        "Translated caption exceeds {Limit} characters (length: {Length}), truncating to {MaxLength}.",
                        TELEGRAM_CAPTION_LIMIT,
                        translatedText.Length,
                        TELEGRAM_CAPTION_LIMIT - TRUNCATION_SUFFIX.Length);
                    var maxLength = TELEGRAM_CAPTION_LIMIT - TRUNCATION_SUFFIX.Length;
                    translatedText = translatedText.Substring(0, maxLength) + TRUNCATION_SUFFIX;
                }

                return translatedText;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to translate caption, using original English text.");
                return originalText;
            }
        }
    }
}