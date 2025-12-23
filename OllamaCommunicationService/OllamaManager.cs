using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;  // Add NuGet: Install-Package Newtonsoft.Json -Version 13.0.3

namespace OllamaCommunicationService
{
    public class OllamaManager
    {
        public const string ModelWeak = "deepseek-coder:latest";
        public const string ModelSmart = "llama3.1:latest";
        private readonly HttpClient client = new HttpClient { BaseAddress = new Uri("http://localhost:11434") };

        private readonly ResponseQuality defaultResponseQuality;

        public string Model { get; set; }

        public OllamaManager(ResponseQuality defaultResponseQuality = null, string model = null)
        {
            this.defaultResponseQuality = defaultResponseQuality ?? ResponseQuality.VeryShort;
            Model = model ?? ModelSmart;
        }

        public async Task<string> ExplainCodeAsync(string code, ResponseQuality responseQuality = default)
        {
            if (string.IsNullOrWhiteSpace(code))
                return "No code selected.";

            if (responseQuality is null)
            { 
                responseQuality = defaultResponseQuality;
            }

            var payload = new
            {
                model = Model,
                prompt = $"Explain this code.(Give me answer {responseQuality.AiAnswerTypeMessage}):\n\n{code}",
                stream = false
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync("/api/generate", content);
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

    public class ResponseQuality
    {
        public static ResponseQuality VeryShort = new ResponseQuality("in 2 words");
        public static ResponseQuality Brief = new ResponseQuality("in 10 words");
        public static ResponseQuality Detailed = new ResponseQuality("with details");

        public string AiAnswerTypeMessage { get; }
        private ResponseQuality(string aiMessage)
        {
            AiAnswerTypeMessage = aiMessage;
        }
    }
}