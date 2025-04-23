using Microsoft.Extensions.Options;
using QRCoder;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TelegramNasaBot.Interfaces;
using TelegramNasaBot.Models;

namespace TelegramNasaBot.Services
{
    public class QrCodeGenerator : IQrCodeGenerator
    {
        private readonly TelegramSettings _telegramSettings;
        private readonly ILogger _logger;

        public QrCodeGenerator(IOptions<TelegramSettings> telegramSettings, ILogger logger)
        {
            _telegramSettings = telegramSettings.Value ?? throw new ArgumentNullException(nameof(telegramSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Validate QrCodeMaxSizePercentage
            if (_telegramSettings.QrCodeMaxSizePercentage < 5.0 || _telegramSettings.QrCodeMaxSizePercentage > 50.0)
            {
                _logger.Warning("QrCodeMaxSizePercentage {Percentage}% is out of valid range (5-50%). Using default 20%.",
                    _telegramSettings.QrCodeMaxSizePercentage);
                _telegramSettings.QrCodeMaxSizePercentage = 20.0;
            }
        }

        public async Task<byte[]> AddQrCodeToImageAsync(byte[] imageData, string channelLink)
        {
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("Image data cannot be null or empty.", nameof(imageData));
            if (string.IsNullOrWhiteSpace(channelLink))
                throw new ArgumentException("Channel link cannot be null or empty.", nameof(channelLink));

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
            var qrCodeBytes = qrCode.GetGraphic(_telegramSettings.QrCodeModuleSize);

            _logger.Information("QR code generated, size: {Size} bytes", qrCodeBytes.Length);
            return qrCodeBytes;
        }

        private void ResizeImageIfNeeded(Image<Rgba32> image)
        {
            _logger.Information("Original image dimensions: {Width}x{Height}", image.Width, image.Height);

            if (image.Width > _telegramSettings.MaxImageDimension || image.Height > _telegramSettings.MaxImageDimension)
            {
                _logger.Information("Resizing image to fit within {MaxDimension}x{MaxDimension}", _telegramSettings.MaxImageDimension);
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(_telegramSettings.MaxImageDimension, _telegramSettings.MaxImageDimension)
                }));
                _logger.Information("Resized image dimensions: {Width}x{Height}", image.Width, image.Height);
            }
        }

        private void OverlayQrCode(Image<Rgba32> image, Image<Rgba32> qrImage)
        {
            // Calculate QR code size based on percentage of the smaller dimension
            var smallerDimension = Math.Min(image.Width, image.Height);
            var qrSize = (int)(smallerDimension * (_telegramSettings.QrCodeMaxSizePercentage / 100.0));

            // Ensure QR code is at least readable (optional minimum size)
            qrSize = Math.Max(qrSize, 50); // Minimum 50 pixels for readability
            qrImage.Mutate(x => x.Resize(qrSize, qrSize));

            // Calculate position (bottom-right corner with padding)
            var x = image.Width - qrImage.Width - _telegramSettings.QrCodePadding;
            var y = image.Height - qrImage.Height - _telegramSettings.QrCodePadding;

            _logger.Information("Overlaying QR code at position ({X}, {Y}) with size {Size}px (based on {Percentage}%)",
                x, y, qrSize, _telegramSettings.QrCodeMaxSizePercentage);

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