using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Serilog;
using TelegramNasaBot.Interfaces;
using TelegramNasaBot.Models;

namespace TelegramNasaBot.Services
{
    public class OpenAiTranslationService : ITranslationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly OpenAiSettings _settings;
        private readonly ILogger _logger;

        public OpenAiTranslationService(
            IHttpClientFactory httpClientFactory,
            IOptions<OpenAiSettings> settings,
            ILogger logger)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<string> TranslateToLanguageAsync(string text, string languageCode)
        {
            if (languageCode != "ru")
            {
                throw new NotSupportedException($"This language '{languageCode}' doesn't support at the moment");
            }

            _logger.Information("Translating text to Russian via OpenAI Assistants API.");

            try
            {
                var httpClient = _httpClientFactory.CreateClient("OpenAiClient");
                var threadId = await CreateThreadAsync(httpClient);
                await AddMessageToThreadAsync(httpClient, threadId, text);
                var runId = await StartRunAsync(httpClient, threadId);
                await WaitForRunCompletionAsync(httpClient, threadId, runId);
                var translatedText = await GetTranslatedTextAsync(httpClient, threadId);

                _logger.Information("Translation successful.");
                return translatedText;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to translate text via OpenAI Assistants API.");
                throw;
            }
        }

        // Creates a new thread
        private async Task<string> CreateThreadAsync(HttpClient httpClient)
        {
            _logger.Information("Creating new thread.");

            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await SendRequestAsync(httpClient, HttpMethod.Post, $"{_settings.ApiUrl}/threads", content);

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
            var threadId = result.GetProperty("id").GetString()
                ?? throw new InvalidOperationException("Failed to get thread ID.");

            _logger.Information("Thread created: {ThreadId}.", threadId);
            return threadId;
        }

        // Adds a message to the thread
        private async Task AddMessageToThreadAsync(HttpClient httpClient, string threadId, string text)
        {
            _logger.Information("Adding message to thread {ThreadId}.", threadId);

            var requestBody = new
            {
                role = "user",
                content = text
            };
            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            await SendRequestAsync(httpClient, HttpMethod.Post, $"{_settings.ApiUrl}/threads/{threadId}/messages", content);
            _logger.Information("Message added to thread {ThreadId}.", threadId);
        }

        // Starts a run for the thread
        private async Task<string> StartRunAsync(HttpClient httpClient, string threadId)
        {
            _logger.Information("Starting run for thread {ThreadId}.", threadId);

            var requestBody = new
            {
                assistant_id = _settings.AssistantId
            };
            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await SendRequestAsync(httpClient, HttpMethod.Post, $"{_settings.ApiUrl}/threads/{threadId}/runs", content);

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
            var runId = result.GetProperty("id").GetString()
                ?? throw new InvalidOperationException("Failed to get run ID.");

            _logger.Information("Run started: {RunId} for thread {ThreadId}.", runId, threadId);
            return runId;
        }

        // Waits for the run to complete
        private async Task WaitForRunCompletionAsync(HttpClient httpClient, string threadId, string runId)
        {
            _logger.Information("Waiting for run {RunId} completion.", runId);

            var startTime = DateTime.UtcNow;
            var maxWaitTime = TimeSpan.FromSeconds(_settings.RunMaxWaitSeconds);

            while (DateTime.UtcNow - startTime < maxWaitTime)
            {
                var response = await SendRequestAsync(httpClient, HttpMethod.Get, $"{_settings.ApiUrl}/threads/{threadId}/runs/{runId}");
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
                var status = result.GetProperty("status").GetString();

                if (status == "completed")
                {
                    _logger.Information("Run {RunId} completed.", runId);
                    return;
                }
                if (status == "failed" || status == "cancelled" || status == "expired")
                {
                    throw new InvalidOperationException($"Run {runId} failed with status: {status}.");
                }

                await Task.Delay(TimeSpan.FromSeconds(_settings.RunPollingIntervalSeconds));
            }

            throw new TimeoutException($"Run {runId} did not complete within {_settings.RunMaxWaitSeconds} seconds.");
        }

        // Gets the translated text from the thread messages
        private async Task<string> GetTranslatedTextAsync(HttpClient httpClient, string threadId)
        {
            _logger.Information("Retrieving translated text from thread {ThreadId}.", threadId);

            var response = await SendRequestAsync(httpClient, HttpMethod.Get, $"{_settings.ApiUrl}/threads/{threadId}/messages");
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

            var messages = result.GetProperty("data");
            if (messages.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("No messages found in thread.");
            }

            var latestMessage = messages[0];
            var contentArray = latestMessage.GetProperty("content");
            if (contentArray.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("No content found in message.");
            }

            var translatedText = contentArray[0].GetProperty("text").GetProperty("value").GetString()
                ?? throw new InvalidOperationException("Failed to get translated text.");

            _logger.Information("Translated text retrieved from thread {ThreadId}.", threadId);
            return translatedText;
        }

        // Sends an HTTP request with authorization header
        private async Task<HttpResponseMessage> SendRequestAsync(HttpClient httpClient, HttpMethod method, string url, HttpContent? content = null)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("Authorization", $"Bearer {_settings.ApiKey}");
            request.Headers.Add("OpenAI-Beta", "assistants=v2");
            if (content != null)
            {
                request.Content = content;
            }

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return response;
        }
    }
}