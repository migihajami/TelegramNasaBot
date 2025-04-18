# Telegram NASA Bot

## Overview
This project is a Telegram bot that fetches the NASA Astronomy Picture of the Day (APOD), adds a QR code linking to a Telegram channel, and posts the image to the channel daily at 12:10 GMT. The bot is built in C# using .NET 8, runs in a Docker container on Linux, and uses Serilog for logging.

### Features
- Fetches daily images from NASA's APOD API.
- Generates a QR code linking to the Telegram channel and overlays it on the image.
- Resizes images to meet Telegram's dimension requirements.
- Posts the modified image to a specified Telegram channel.
- Scheduled to run daily at 12:10 GMT using Quartz.NET.
- Logs all operations using Serilog (console and file output).

## Prerequisites
- **.NET 8 SDK**: Required to build and run the application.
- **Docker**: To containerize and run the bot on Linux.
- **NASA API Key**: Get a free key from [NASA API](https://api.nasa.gov/). The demo key (`DEMO_KEY`) can be used for testing but is rate-limited.
- **Telegram Bot Token**: Create a bot via `@BotFather` on Telegram and get the token.
- **Telegram Channel**: A public or private channel where the bot will post (e.g., `@space_explorer_nasa`).

## Setup

### 1. Clone the Repository
Clone this project to your local machine:
```bash
git clone <repository-url>
cd TelegramNasaBot
```

### 2. Configure Settings
Edit `appsettings.json` with your NASA API key, Telegram bot token, and channel ID:
```json
{
  "Telegram": {
    "BotToken": "your-telegram-bot-token",
    "ChannelId": "@space_explorer_nasa"
  },
  "Nasa": {
    "ApiKey": "your-nasa-api-key",
    "ApiUrl": "https://api.nasa.gov/planetary/apod"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```
Optionally, create `appsettings.Development.json` for local development overrides (e.g., a different bot token).

### 3. Build the Project
Build the project using .NET:
```bash
dotnet build
```

### 4. Run Locally (Optional)
Test the bot locally:
```bash
dotnet run
```
To test more frequently, temporarily modify the cron schedule in `Program.cs` to run every minute:
```csharp
.WithCronSchedule("0 * * * * ?", x => x.InTimeZone(TimeZoneInfo.Utc)) // Every minute
```
Check your Telegram channel for posts and the `logs/` directory for log files.

### 5. Build and Run in Docker
Build the Docker image:
```bash
docker build -t telegram-nasa-boot .
```
Run the container:
```bash
docker run --rm telegram-nasa-boot
```
To persist logs outside the container, mount a volume:
```bash
docker run --rm -v $(pwd)/logs:/app/logs telegram-nasa-boot
```

## Project Structure
- `Program.cs`: Entry point, sets up DI, configuration, and Quartz scheduling.
- `PhotoFetcher.cs`: Fetches images from the NASA APOD API.
- `QrCodeGenerator.cs`: Generates QR codes and overlays them on images.
- `Publisher.cs`: Posts images to the Telegram channel.
- `NasaPhotoJob.cs`: Quartz job that orchestrates the daily posting.
- `appsettings.json`: Configuration file for Telegram, NASA API, and Serilog.
- `Dockerfile`: Defines the Docker build and runtime environment.

## Dependencies
- **.NET 8**: Core framework.
- **Telegram.Bot**: For interacting with the Telegram API.
- **QRCoder**: For generating QR codes.
- **SixLabors.ImageSharp**: For image processing (resizing and overlaying QR codes).
- **Quartz.NET**: For scheduling daily posts.
- **Serilog**: For logging.

## Troubleshooting
- **PHOTO_INVALID_DIMENSIONS Error**: Ensure the image is resized correctly in `QrCodeGenerator.cs`. The max dimension is set to 4000x4000.
- **API Errors**: Check your NASA API key and Telegram bot token in `appsettings.json`.
- **Scheduling Issues**: Verify the cron schedule in `Program.cs` and ensure the system time zone doesn’t interfere (runs in UTC).
- **Logs**: Check `logs/log-*.txt` for detailed error messages.

## Future Improvements
- Add developer mode for testing without waiting for the scheduled time.
- Implement retry logic for API failures.
- Add support for posting image descriptions or titles in the caption.
- Translate captions to multiple languages based on user preferences.

## License
This project is licensed under the MIT License.