using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimTalk;
using RimTalk.Client;
using RimTalk.Client.OpenAI;
using RimTalk.Client.Gemini;
using RimTalk.Data;
using RimTalk.Util;
using UnityEngine.Networking;
using Verse;

namespace RimTalkQuests.Services
{
    /// <summary>
    /// Provides plain text streaming for AI clients without relying on RimTalk's JsonStreamParser.
    ///
    /// This implementation completely bypasses RimTalk's client methods and directly constructs
    /// HTTP requests using public APIs only - no reflection needed!
    /// </summary>
    public static class PlainTextStreamingClient
    {
        /// <summary>
        /// Plain text streaming for OpenAI-compatible APIs
        /// </summary>
        public static async Task<Payload> StreamOpenAIAsync(
            string baseUrl,
            string model,
            string apiKey,
            Dictionary<string, string> extraHeaders,
            string instruction,
            List<(Role role, string message)> messages,
            Action<string> onTextChunkReceived
        )
        {
            // Build endpoint URL (same logic as OpenAIClient.FormatEndpointUrl)
            string endpointUrl = FormatOpenAIEndpointUrl(baseUrl);

            // Build request JSON (same logic as OpenAIClient.BuildRequestJson)
            string jsonContent = BuildOpenAIRequestJson(instruction, messages, model, stream: true);

            // Create stream handler with callback
            var streamHandler = new OpenAIStreamHandler(
                chunk => onTextChunkReceived?.Invoke(chunk)
            );

            // Send request
            await SendOpenAIRequestAsync(
                endpointUrl,
                jsonContent,
                apiKey,
                extraHeaders,
                streamHandler
            );

            return new Payload(
                endpointUrl,
                model,
                jsonContent,
                streamHandler.GetFullText(),
                streamHandler.GetTotalTokens()
            );
        }

        /// <summary>
        /// Plain text streaming for Gemini API
        /// </summary>
        public static async Task<Payload> StreamGeminiAsync(
            string baseUrl,
            string model,
            string apiKey,
            string instruction,
            List<(Role role, string message)> messages,
            Action<string> onTextChunkReceived
        )
        {
            // Build endpoints
            string streamEndpoint =
                $"{baseUrl}/models/{model}:streamGenerateContent?alt=sse&key={apiKey}";

            // Build request JSON (same logic as GeminiClient.BuildRequestJson)
            string jsonContent = BuildGeminiRequestJson(instruction, messages, model);

            // Create stream handler with callback
            var streamHandler = new GeminiStreamHandler(
                chunk => onTextChunkReceived?.Invoke(chunk)
            );

            // Send request
            await SendGeminiRequestAsync(streamEndpoint, jsonContent, streamHandler);

            return new Payload(
                baseUrl,
                model,
                jsonContent,
                streamHandler.GetFullText(),
                streamHandler.GetTotalTokens()
            );
        }

        /// <summary>
        /// Convenience method that works with existing IAIClient to get config, then streams directly
        /// </summary>
        public static async Task<Payload> StreamPlainTextAsync(
            this IAIClient client,
            string instruction,
            List<(Role role, string message)> messages,
            Action<string> onTextChunkReceived
        )
        {
            var settings = Settings.Get();
            var config = settings.GetActiveConfig();
            var model = settings.GetCurrentModel();

            if (client is OpenAIClient)
            {
                // Get OpenAI config from settings
                string baseUrl = config?.Url ?? "";
                string apiKey = config?.ApiKey ?? "";
                var extraHeaders = config?.ExtraHeaders;

                return await StreamOpenAIAsync(
                    baseUrl,
                    model,
                    apiKey,
                    extraHeaders,
                    instruction,
                    messages,
                    onTextChunkReceived
                );
            }
            else if (client is GeminiClient)
            {
                // Get Gemini config
                string baseUrl = AIProvider.Google.GetEndpointUrl();
                string apiKey = config?.ApiKey ?? "";

                return await StreamGeminiAsync(
                    baseUrl,
                    model,
                    apiKey,
                    instruction,
                    messages,
                    onTextChunkReceived
                );
            }
            else
            {
                throw new NotSupportedException(
                    $"Client type {client.GetType().Name} is not supported"
                );
            }
        }

        #region Private Helper Methods (mirror RimTalk's logic using public APIs)

        private static string FormatOpenAIEndpointUrl(string baseUrl)
        {
            const string defaultPath = "/v1/chat/completions";

            if (string.IsNullOrEmpty(baseUrl))
                return string.Empty;

            var trimmed = baseUrl.Trim().TrimEnd('/');
            var uri = new Uri(trimmed);

            return (uri.AbsolutePath == "/" || string.IsNullOrEmpty(uri.AbsolutePath.Trim('/')))
                ? trimmed + defaultPath
                : trimmed;
        }

        private static string BuildOpenAIRequestJson(
            string instruction,
            List<(Role role, string message)> messages,
            string model,
            bool stream
        )
        {
            var allMessages = new List<RimTalk.Client.OpenAI.Message>();

            if (!string.IsNullOrEmpty(instruction))
            {
                allMessages.Add(new RimTalk.Client.OpenAI.Message { Role = "system", Content = instruction });
            }

            allMessages.AddRange(
                messages.Select(
                    m =>
                        new RimTalk.Client.OpenAI.Message
                        {
                            Role = m.role == Role.User ? "user" : "assistant",
                            Content = m.message
                        }
                )
            );

            var request = new OpenAIRequest
            {
                Model = model,
                Messages = allMessages,
                Stream = stream,
                StreamOptions = stream ? new StreamOptions { IncludeUsage = true } : null
            };

            return JsonUtil.SerializeToJson(request);
        }

        private static string BuildGeminiRequestJson(
            string instruction,
            List<(Role role, string message)> messages,
            string model
        )
        {
            SystemInstruction systemInstruction = null;
            var contents = new List<Content>();

            // Handle Gemma models specially (same as GeminiClient)
            if (model.Contains("gemma"))
            {
                var random = new System.Random();
                contents.Add(
                    new Content
                    {
                        Role = "user",
                        Parts = new List<Part>
                        {
                            new Part { Text = $"{random.Next()} {instruction}" }
                        }
                    }
                );
            }
            else
            {
                systemInstruction = new SystemInstruction
                {
                    Parts = new List<Part> { new Part { Text = instruction } }
                };
            }

            contents.AddRange(
                messages.Select(
                    m =>
                        new Content
                        {
                            Role = m.role == Role.User ? "user" : "model",
                            Parts = new List<Part> { new Part { Text = m.message } }
                        }
                )
            );

            var config = new GenerationConfig();
            if (model.Contains("flash"))
            {
                config.ThinkingConfig = new ThinkingConfig { ThinkingBudget = 0 };
            }

            return JsonUtil.SerializeToJson(
                new GeminiDto
                {
                    SystemInstruction = systemInstruction,
                    Contents = contents,
                    GenerationConfig = config
                }
            );
        }

        private static async Task<string> SendOpenAIRequestAsync(
            string endpointUrl,
            string jsonContent,
            string apiKey,
            Dictionary<string, string> extraHeaders,
            OpenAIStreamHandler streamHandler
        )
        {
            if (string.IsNullOrEmpty(endpointUrl))
            {
                Logger.Error("Endpoint URL is missing.");
                throw new InvalidOperationException("Endpoint URL is missing");
            }

            Logger.Debug($"API request: {endpointUrl}\n{jsonContent}");

            using var webRequest = new UnityWebRequest(endpointUrl, "POST");
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonContent));
            webRequest.downloadHandler = streamHandler;
            webRequest.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(apiKey))
                webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            if (extraHeaders != null)
            {
                foreach (var header in extraHeaders)
                    webRequest.SetRequestHeader(header.Key, header.Value);
            }

            var asyncOp = webRequest.SendWebRequest();

            // Timeout logic (same as OpenAIClient)
            bool isLocal =
                endpointUrl.Contains("localhost")
                || endpointUrl.Contains("127.0.0.1")
                || endpointUrl.Contains("192.168.")
                || endpointUrl.Contains("10.");

            float inactivityTimer = 0f;
            ulong lastBytes = 0;
            float connectTimeout = isLocal ? 300f : 60f;
            float readTimeout = 60f;

            while (!asyncOp.isDone)
            {
                if (Current.Game == null)
                    return null;
                await Task.Delay(100);

                ulong currentBytes = webRequest.downloadedBytes;
                bool hasStartedReceiving = currentBytes > 0;

                if (currentBytes > lastBytes)
                {
                    inactivityTimer = 0f;
                    lastBytes = currentBytes;
                }
                else
                {
                    inactivityTimer += 0.1f;
                }

                if (!hasStartedReceiving && inactivityTimer > connectTimeout)
                {
                    webRequest.Abort();
                    throw new TimeoutException(
                        $"Connection timed out (Waited {connectTimeout}s for first token)"
                    );
                }

                if (hasStartedReceiving && inactivityTimer > readTimeout)
                {
                    webRequest.Abort();
                    throw new TimeoutException(
                        $"Read timed out (Stalled for {readTimeout}s during generation)"
                    );
                }
            }

            if (webRequest.isNetworkError || webRequest.isHttpError)
            {
                string errorMsg = webRequest.error;
                Logger.Error($"Request failed: {webRequest.responseCode} - {errorMsg}");
                throw new Exception($"Request failed: {errorMsg}");
            }

            Logger.Debug($"API response: \n{streamHandler.GetRawJson()}");
            return streamHandler.GetFullText();
        }

        private static async Task<string> SendGeminiRequestAsync(
            string url,
            string jsonContent,
            GeminiStreamHandler streamHandler
        )
        {
            Logger.Debug($"API request: {url}\n{jsonContent}");

            using var webRequest = new UnityWebRequest(url, "POST");
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonContent));
            webRequest.downloadHandler = streamHandler;
            webRequest.SetRequestHeader("Content-Type", "application/json");

            var asyncOp = webRequest.SendWebRequest();

            float inactivityTimer = 0f;
            ulong lastBytes = 0;
            const float connectTimeout = 30f;
            const float readTimeout = 30f;

            while (!asyncOp.isDone)
            {
                if (Current.Game == null)
                    return null;
                await Task.Delay(100);

                ulong currentBytes = webRequest.downloadedBytes;
                bool hasStartedReceiving = currentBytes > 0;

                if (currentBytes > lastBytes)
                {
                    inactivityTimer = 0f;
                    lastBytes = currentBytes;
                }
                else
                {
                    inactivityTimer += 0.1f;
                }

                if (!hasStartedReceiving && inactivityTimer > connectTimeout)
                {
                    webRequest.Abort();
                    throw new TimeoutException($"Connection timed out ({connectTimeout}s)");
                }

                if (hasStartedReceiving && inactivityTimer > readTimeout)
                {
                    webRequest.Abort();
                    throw new TimeoutException($"Read timed out ({readTimeout}s)");
                }
            }

            if (webRequest.isNetworkError || webRequest.isHttpError)
            {
                string errorMsg = webRequest.error;
                Logger.Error($"Request failed: {webRequest.responseCode} - {errorMsg}");
                throw new Exception($"Request failed: {errorMsg}");
            }

            Logger.Debug($"API response: \n{streamHandler.GetRawJson()}");
            return streamHandler.GetFullText();
        }

        #endregion
    }
}
