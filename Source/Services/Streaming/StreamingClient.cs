using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimTalk.Client;
using RimTalk.Data;

namespace RimTalkQuests.Services.Streaming
{
    /// <summary>
    /// Base class for provider-specific streaming clients.
    /// </summary>
    public abstract class StreamingClient
    {
        protected StreamingClient(IAIClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        protected IAIClient Client { get; }

        public abstract Task<Payload> StreamFromSettingsAsync(
            string instruction,
            List<(Role role, string message)> messages,
            Action<string> onTextChunkReceived
        );

        protected static Action<string> SafeChunkCallback(Action<string> callback)
        {
            return chunk =>
            {
                if (!string.IsNullOrEmpty(chunk))
                {
                    callback?.Invoke(chunk);
                }
            };
        }
    }
}
