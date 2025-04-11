using Microsoft.Extensions.Options;
using QRCoder;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using Image = SixLabors.ImageSharp.Image;

namespace TelegramNasaBot
{
    public class QrCodeGenerator : IQrCodeGenerator
    {
        private readonly TelegramSettings _telegramSettings;
        private readonly ILogger _logger;

        public QrCodeGenerator(IOptions<TelegramSettings> telegramSettings, ILogger logger)
        {
            _telegramSettings = telegramSettings.Value;
            _logger = logger;
        }

        public async Task<byte[]> AddQrCodeToImageAsync(byte[] imageData, string channelLink)
        {
            try
            {
                _logger.Information("Generating QR code for channel: {ChannelLink}", channelLink);

                // Generate QR code
                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(channelLink, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeBytes = qrCode.GetGraphic(20); // 20 pixels per module

                _logger.Information("QR code generated, size: {Size} bytes", qrCodeBytes.Length);

                // Load the NASA image
                using var image = Image.Load<Rgba32>(imageData);
                using var qrImage = Image.Load<Rgba32>(qrCodeBytes);

                // Resize QR code to 1/4 of the image's smaller dimension
                var qrSize = Math.Min(image.Width, image.Height) / 4;
                qrImage.Mutate(x => x.Resize(qrSize, qrSize));

                // Calculate position (bottom-right corner with padding)
                var padding = 20;
                var x = image.Width - qrImage.Width - padding;
                var y = image.Height - qrImage.Height - padding;

                _logger.Information("Overlaying QR code at position ({X}, {Y})", x, y);

                // Add a white background for QR code readability
                using var background = new Image<Rgba32>(qrImage.Width + 10, qrImage.Height + 10);
                background.Mutate(ctx => ctx.BackgroundColor(Color.White));
                background.Mutate(ctx => ctx.DrawImage(qrImage, new Point(5, 5), 1f));

                // Overlay QR code on the NASA image
                image.Mutate(ctx => ctx.DrawImage(background, new Point(x, y), 1f));

                // Save the result to a memory stream
                using var outputStream = new MemoryStream();
                await image.SaveAsJpegAsync(outputStream);
                var resultBytes = outputStream.ToArray();

                _logger.Information("QR code added to image, output size: {Size} bytes", resultBytes.Length);

                return resultBytes;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error generating or adding QR code.");
                throw;
            }
        }
    }
}