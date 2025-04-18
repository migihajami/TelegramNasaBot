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
        private readonly TelegramSettings _telegramSettings;
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger _logger;

        public Publisher(IOptions<TelegramSettings> telegramSettings, ITelegramBotClient botClient, ILogger logger)
        {
            _telegramSettings = telegramSettings.Value;
            _botClient = botClient;
            _logger = logger;
        }

        public async Task PublishPhotoAsync(byte[] imageData, string caption)
        {
            try
            {
                _logger.Information("Publishing photo to Telegram channel: {ChannelId}", _telegramSettings.ChannelId);

                using var stream = new MemoryStream(imageData);
                var file = new InputFileStream(stream, "nasa-photo.jpg");

                await _botClient.SendPhoto(
                    chatId: _telegramSettings.ChannelId,
                    photo: file,
                    caption: caption);

                _logger.Information("Photo published successfully to {ChannelId}", _telegramSettings.ChannelId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error publishing photo to Telegram.");
                throw;
            }
        }
    }
}