# Telegram NASA Bot

A .NET Core bot that fetches NASA's Astronomy Picture of the Day (APOD), translates its title and description into Russian using OpenAI Assistants API, and publishes it to a Telegram channel.

## Requirements
- **.NET SDK**: 6.0 or later.
- **API Keys**:
  - NASA API key ([NASA API](https://api.nasa.gov/)).
  - OpenAI API key and Assistant ID ([OpenAI Platform](https://platform.openai.com/)).
  - Telegram Bot Token ([BotFather](https://t.me/BotFather)).

## Setup
1. **Clone the Repository**:
   ```bash
   git clone https://github.com/your-repo/telegram-nasa-bot.git
   cd telegram-nasa-bot
   ```

2. **Configure `appsettings.json`**:
   Create `appsettings.json` in the project root:
   ```json
   {
     "Telegram": {
       "BotToken": "your-telegram-bot-token",
       "ChannelId": "@YourChannelName"
     },
     "Nasa": {
       "ApiKey": "your-nasa-api-key"
     },
     "OpenAi": {
       "ApiKey": "your-openai-api-key",
       "AssistantId": "your-assistant-id"
     }
   }
   ```
   Replace placeholders with your API keys and channel name.

3. **Install Dependencies**:
   ```bash
   dotnet restore
   ```

## Running the Bot
Run the bot:
```bash
dotnet run
```
The bot fetches NASA's APOD, translates it, and posts to Telegram daily at 12:10 UTC.

## License
MIT License.
