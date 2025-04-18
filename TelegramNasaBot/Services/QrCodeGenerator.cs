using Microsoft.Extensions.Options;
using QRCoder;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Threading.Tasks;
using TelegramNasaBot.Interfaces;
using TelegramNasaBot.Models;

namespace TelegramNasaBot.Services
{
    public class QrCodeGenerator : IQrCodeGenerator
    {
        private readonly TelegramSettings _telegramSettings;
        private readonly ILogger _logger;
        private const int MaxDimension = 4000; // Safe max dimension for Telegram

        public QrCodeGenerator(IOptions<TelegramSettings> telegramSettings, ILogger logger)
        {
            _telegramSettings = telegramSettings.Value;
            _logger = logger;
        }

        public async Task<byte[]> AddQrCodeToImageAsync(byte[] imageData, string channelLink)
        {
            try
            {
                // Step 1: Generate QR code
                var qrCodeBytes = GenerateQrCode(channelLink);

                // Step 2: Load and resize the NASA image if needed
                using var image = Image.Load<Rgba32>(imageData);
                ResizeImageIfNeeded(image);

                // Step 3: Load QR code image and overlay it
                using var qrImage = Image.Load<Rgba32>(qrCodeBytes);
                OverlayQrCode(image, qrImage);

                // Step 4: Save the result to bytes
                return await SaveImageToBytes(image);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error generating or adding QR code.");
                throw;
            }
        }

        private byte[] GenerateQrCode(string channelLink)
        {
            _logger.Information("Generating QR code for channel: {ChannelLink}", channelLink);

            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(channelLink, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20); // 20 pixels per module

            _logger.Information("QR code generated, size: {Size} bytes", qrCodeBytes.Length);
            return qrCodeBytes;
        }

        private void ResizeImageIfNeeded(Image<Rgba32> image)
        {
            _logger.Information("Original image dimensions: {Width}x{Height}", image.Width, image.Height);

            if (image.Width > MaxDimension || image.Height > MaxDimension)
            {
                _logger.Information("Resizing image to fit within {MaxDimension}x{MaxDimension}", MaxDimension, MaxDimension);
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(MaxDimension, MaxDimension)
                }));
                _logger.Information("Resized image dimensions: {Width}x{Height}", image.Width, image.Height);
            }
        }

        private void OverlayQrCode(Image<Rgba32> image, Image<Rgba32> qrImage)
        {
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
        }

        private async Task<byte[]> SaveImageToBytes(Image<Rgba32> image)
        {
            using var outputStream = new MemoryStream();
            await image.SaveAsJpegAsync(outputStream);
            var resultBytes = outputStream.ToArray();

            _logger.Information("QR code added to image, output size: {Size} bytes", resultBytes.Length);
            return resultBytes;
        }
    }
}