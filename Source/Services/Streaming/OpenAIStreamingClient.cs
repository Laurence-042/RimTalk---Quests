using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimTalk;
using RimTalk.Client;
using RimTalk.Client.OpenAI;
using RimTalk.Data;
using RimTalk.Util;
using UnityEngine.Networking;
using Verse;

namespace RimTalkQuests.Services.Streaming
{
    /// <summary>
    /// Plain text streaming client for OpenAI-compatible APIs
    /// </summary>
    public static class OpenAIStreamingClient
    {
        /// <summary>
        /// Stream chat completion using settings from RimTalk configuration
        /// </summary>
        public static async Task<Payload> StreamFromSettingsAsync(
            string instruction,
            List<(Role role, string message)> messages,
            Action<string> onTextChunkReceived
        )
        {
            var settings = Settings.Get();
            var config = settings.GetActiveConfig();
            var model = settings.GetCurrentModel();

            string baseUrl = config?.BaseUrl ?? "";
            string apiKey = config?.ApiKey ?? "";

            // Get extra headers from provider registry if available
            Dictionary<string, string> extraHeaders = null;
            if (AIProviderRegistry.Defs.TryGetValue(config.Provider, out var def))
            {
                extraHeaders = def.ExtraHeaders;
            }

            return await StreamAsync(
                baseUrl,
                model,
                apiKey,
                extraHeaders,
                instruction,
                messages,
                onTextChunkReceived
            );
        }

        /// <summary>
        /// Stream chat completion from OpenAI-compatible APIs with explicit parameters
        /// </summary>
        public static async Task<Payload> StreamAsync(
            string baseUrl,
            string model,
            string apiKey,
            Dictionary<string, string> extraHeaders,
            string instruction,
            List<(Role role, string message)> messages,
            Action<string> onTextChunkReceived
        )
        {
            // Build endpoint URL
            string endpointUrl = FormatEndpointUrl(baseUrl);

            // Build request JSON
            string jsonContent = BuildRequestJson(instruction, messages, model, stream: true);

            // Create stream handler with callback
            var streamHandler = new OpenAIStreamHandler(
                chunk => onTextChunkReceived?.Invoke(chunk)
            );

            // Send request
            await SendRequestAsync(
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

        #region Private Helper Methods

        private static string FormatEndpointUrl(string baseUrl)
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

        private static string BuildRequestJson(
            string instruction,
            List<(Role role, string message)> messages,
            string model,
            bool stream
        )
        {
            var allMessages = new List<RimTalk.Client.OpenAI.Message>();

            if (!string.IsNullOrEmpty(instruction))
            {
                allMessages.Add(
                    new RimTalk.Client.OpenAI.Message { Role = "system", Content = instruction }
                );
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

        private static async Task<string> SendRequestAsync(
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

            // Timeout logic - longer timeout for local endpoints
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

            if (
                webRequest.result == UnityWebRequest.Result.ConnectionError
                || webRequest.result == UnityWebRequest.Result.ProtocolError
            )
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
