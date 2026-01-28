using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimTalk;
using RimTalk.Client;
using RimTalk.Client.Gemini;
using RimTalk.Data;
using RimTalk.Util;
using UnityEngine.Networking;
using Verse;

namespace RimTalkQuests.Services.Streaming
{
    /// <summary>
    /// Plain text streaming client for Gemini API
    /// </summary>
    public static class GeminiStreamingClient
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

            string baseUrl = AIProvider.Google.GetEndpointUrl();
            string apiKey = config?.ApiKey ?? "";

            return await StreamAsync(
                baseUrl,
                model,
                apiKey,
                instruction,
                messages,
                onTextChunkReceived
            );
        }

        /// <summary>
        /// Stream chat completion from Gemini API with explicit parameters
        /// </summary>
        public static async Task<Payload> StreamAsync(
            string baseUrl,
            string model,
            string apiKey,
            string instruction,
            List<(Role role, string message)> messages,
            Action<string> onTextChunkReceived
        )
        {
            // Build endpoint URL with SSE streaming
            string streamEndpoint =
                $"{baseUrl}/models/{model}:streamGenerateContent?alt=sse&key={apiKey}";

            // Build request JSON
            string jsonContent = BuildRequestJson(instruction, messages, model);

            // Create stream handler with callback
            var streamHandler = new GeminiStreamHandler(
                chunk => onTextChunkReceived?.Invoke(chunk)
            );

            // Send request
            await SendRequestAsync(streamEndpoint, jsonContent, streamHandler);

            return new Payload(
                baseUrl,
                model,
                jsonContent,
                streamHandler.GetFullText(),
                streamHandler.GetTotalTokens()
            );
        }

        #region Private Helper Methods

        private static string BuildRequestJson(
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

        private static async Task<string> SendRequestAsync(
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
