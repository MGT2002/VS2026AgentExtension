using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;  // Add NuGet: Install-Package Newtonsoft.Json -Version 13.0.3

namespace OllamaCommunicationService
{
    public class OllamaManager
    {
        private readonly HttpClient _client = new HttpClient { BaseAddress = new Uri("http://localhost:11434") };
        private const string Model = "deepseek-coder:latest";

        public async Task<string> ExplainCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return "No code selected.";

            var payload = new
            {
                model = Model,
                prompt = $"Explain this code clearly and concisely:\n\n{code}",
                stream = false
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _client.PostAsync("/api/generate", content);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                return $"Ollama server error: {ex.Message}\nCheck if Ollama is running on http://localhost:11434";
            }

            var resultJson = await response.Content.ReadAsStringAsync();

            try
            {
                var resultObj = JsonConvert.DeserializeObject<dynamic>(resultJson);
                return resultObj?.response?.ToString() ?? "No response from Ollama";
            }
            catch
            {
                return "Failed to parse Ollama response.";
            }
        }
    }
}