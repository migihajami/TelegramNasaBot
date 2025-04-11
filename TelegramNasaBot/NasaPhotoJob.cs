using Quartz;
using Serilog;
using System;
using System.Threading.Tasks;

namespace TelegramNasaBot
{
    public class NasaPhotoJob : IJob
    {
        private readonly IPhotoFetcher _photoFetcher;
        private readonly IQrCodeGenerator _qrCodeGenerator;
        private readonly IPublisher _publisher;
        private readonly TelegramSettings _telegramSettings;
        private readonly ILogger _logger;

        public NasaPhotoJob(
            IPhotoFetcher photoFetcher,
            IQrCodeGenerator qrCodeGenerator,
            IPublisher publisher,
            TelegramSettings telegramSettings,
            ILogger logger)
        {
            _photoFetcher = photoFetcher;
            _qrCodeGenerator = qrCodeGenerator;
            _publisher = publisher;
            _telegramSettings = telegramSettings;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.Information("Starting daily NASA photo posting job...");

                // Fetch photo
                var (url, imageData) = await _photoFetcher.FetchNasaPhotoAsync();

                // Add QR code
                var modifiedImage = await _qrCodeGenerator.AddQrCodeToImageAsync(imageData, $"https://t.me/{_telegramSettings.ChannelId.Remove(0, 1)}");

                // Publish to Telegram
                var caption = $"NASA Astronomy Picture of the Day: {url}";
                await _publisher.PublishPhotoAsync(modifiedImage, caption);

                _logger.Information("Daily NASA photo job completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in daily NASA photo job.");
                throw new JobExecutionException(ex);
            }
        }
    }
}