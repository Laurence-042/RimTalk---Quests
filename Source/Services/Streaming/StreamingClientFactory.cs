using System;
using RimTalk.Client;
using RimTalk.Client.OpenAI;
using RimTalk.Client.Player2;

namespace RimTalkQuests.Services.Streaming
{
    public static class StreamingClientFactory
    {
        public static StreamingClient Create(IAIClient client)
        {
            if (client is OpenAIClient)
            {
                return new OpenAIStreamingClient(client);
            }

            if (client is Player2Client)
            {
                return new Player2StreamingClient(client);
            }

            throw new NotSupportedException(
                $"Client type {client?.GetType().Name ?? "Unknown"} is not supported for streaming"
            );
        }
    }
}
