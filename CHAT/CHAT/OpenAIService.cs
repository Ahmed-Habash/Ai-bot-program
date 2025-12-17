using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CHAT
{
    public sealed class OpenAIService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public OpenAIService()
        {
            _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set. Set it before running the app.");

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        public async Task<string> SendMessageAsync(string systemPrompt, string userMessage)
        {
            var messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            };

            var body = new
            {
                model = "gpt-4o-mini",
                messages,
                temperature = 0.7,
                max_tokens = 800
            };

            var json = JsonSerializer.Serialize(body);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var respText = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                // Provide helpful debug message
                return $"[API Error: {resp.StatusCode}] {respText}";
            }

            try
            {
                using var doc = JsonDocument.Parse(respText);
                var assistant = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return assistant ?? string.Empty;
            }
            catch (Exception ex)
            {
                return $"[Parse error] {ex.Message}\n{respText}";
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}