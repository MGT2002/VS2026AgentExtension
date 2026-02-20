using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OllamaCommunicationService
{
    public class OllamaManager
    {
        public const string ModelWeak = "deepseek-coder:latest";
        public const string ModelSmart = "llama3.1:latest";
        public const string ModelMedium = "phi3:mini";
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
            return await GenerateResponse(payload);
        }

        public Task StreamExplainCodeAsync(
            string code,
            Func<string, Task> onChunkAsync,
            ResponseQuality responseQuality = default,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Task.CompletedTask;
            }

            if (responseQuality is null)
            {
                responseQuality = defaultResponseQuality;
            }

            var explainPrompt = "Explain this code:\n\n" + code;
            return StreamPromptAsync(explainPrompt, onChunkAsync, responseQuality, cancellationToken);
        }

        public async Task StreamPromptAsync(
            string prompt,
            Func<string, Task> onChunkAsync,
            ResponseQuality responseQuality = default,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return;
            }

            if (responseQuality is null)
            {
                responseQuality = defaultResponseQuality;
            }

            var payload = new
            {
                model = Model,
                prompt = prompt,
                system = $"Respond {responseQuality.AiAnswerTypeMessage}.",
                stream = true
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using (var request = new HttpRequestMessage(HttpMethod.Post, "/api/generate"))
            {
                request.Content = content;

                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();

                    using (cancellationToken.Register(() => response.Dispose()))
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var reader = new StreamReader(stream))
                    {
                        while (!reader.EndOfStream)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var line = await reader.ReadLineAsync();
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                continue;
                            }

                            JObject chunkObj;
                            try
                            {
                                chunkObj = JObject.Parse(line);
                            }
                            catch
                            {
                                continue;
                            }

                            var chunk = chunkObj["response"]?.ToString();
                            if (!string.IsNullOrEmpty(chunk) && onChunkAsync != null)
                            {
                                await onChunkAsync(chunk);
                            }

                            var doneToken = chunkObj["done"];
                            if (doneToken != null && doneToken.Type == JTokenType.Boolean && doneToken.Value<bool>())
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        private async Task<string> GenerateResponse(object payload)
        {
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
