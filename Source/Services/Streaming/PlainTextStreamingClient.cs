using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimTalk.Client;
using RimTalk.Client.OpenAI;
using RimTalk.Client.Gemini;
using RimTalk.Client.Player2;
using RimTalk.Data;

namespace RimTalkQuests.Services.Streaming
{
    /// <summary>
    /// Provides unified plain text streaming for AI clients.
    /// Routes to the appropriate streaming client based on the IAIClient type.
    /// </summary>
    public static class PlainTextStreamingClient
    {
        /// <summary>
        /// Extension method that routes to the appropriate streaming client based on IAIClient type.
        /// Each client handles its own configuration retrieval from RimTalk settings.
        /// </summary>
        public static async Task<Payload> StreamPlainTextAsync(
            this IAIClient client,
            string instruction,
            List<(Role role, string message)> messages,
            Action<string> onTextChunkReceived
        )
        {
            if (client is OpenAIClient)
            {
                return await OpenAIStreamingClient.StreamFromSettingsAsync(
                    instruction,
                    messages,
                    onTextChunkReceived
                );
            }

            if (client is GeminiClient)
            {
                return await GeminiStreamingClient.StreamFromSettingsAsync(
                    instruction,
                    messages,
                    onTextChunkReceived
                );
            }

            if (client is Player2Client)
            {
                return await Player2StreamingClient.StreamFromSettingsAsync(
                    instruction,
                    messages,
                    onTextChunkReceived
                );
            }

            throw new NotSupportedException(
                $"Client type {client.GetType().Name} is not supported for streaming"
            );
        }
    }
}
