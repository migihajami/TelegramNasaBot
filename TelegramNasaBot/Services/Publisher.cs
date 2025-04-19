using Microsoft.Extensions.Options;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using System;
using System.IO;
using System.Threading.Tasks;
using TelegramNasaBot.Interfaces;
using TelegramNasaBot.Models;

namespace TelegramNasaBot.Services
{
    public class Publisher : IPublisher
    {
        private readonly ITelegramBotClient _botClient;
        private readonly TelegramSettings _settings;
        private readonly ILogger _logger;

        public Publisher(
            ITelegramBotClient botClient,
            IOptions<TelegramSettings> settings,
            ILogger logger)
        {
            _botClient = botClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task PublishPhotoAsync(byte[] imageData, string caption)
        {
            _logger.Information("Publishing photo to Telegram channel {ChannelId}.", _settings.ChannelId);

            try
            {
                using var stream = new MemoryStream(imageData);
                var file = new InputFileStream(stream, "photo.jpg");

                await _botClient.SendPhoto(
                    chatId: _settings.ChannelId,
                    photo: file,
                    caption: caption);

                _logger.Information("Photo published successfully.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to publish photo to Telegram.");
                throw;
            }
        }
    }
}