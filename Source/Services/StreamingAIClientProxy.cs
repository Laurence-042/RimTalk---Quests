using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using RimTalk.Client;
using RimTalk.Client.OpenAI;
using RimTalk.Data;
using UnityEngine.Networking;
using Verse;

namespace RimTalkQuests.Services
{
    /// <summary>
    /// Extension methods to fix RimTalk's streaming implementation bug.
    ///
    /// Problem: RimTalk's GetStreamingChatCompletionAsync passes plain text chunks to JsonStreamParser,
    /// which fails to parse non-JSON content, causing callbacks to never fire.
    ///
    /// Solution: Provide fixed streaming methods that bypass JsonStreamParser.
    /// </summary>
    public static class AIClientStreamingFix
    {
        /// <summary>
        /// Fixed streaming method for OpenAIClient that bypasses JsonStreamParser
        /// </summary>
        public static async Task<Payload> GetStreamingChatCompletionAsync_Fixed<T>(
            this OpenAIClient client,
            string instruction,
            List<(Role role, string message)> messages,
            Action<T> onResponseParsed
        ) where T : class
        {
            // Access private members via reflection
            var clientType = typeof(OpenAIClient);
            var buildRequestJson = clientType.GetMethod(
                "BuildRequestJson",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            var sendRequestAsync = clientType.GetMethod(
                "SendRequestAsync",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            var endpointUrl = (string)
                clientType
                    .GetField("_endpointUrl", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(client);
            var model = (string)
                clientType
                    .GetField("model", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(client);

            string jsonContent = (string)
                buildRequestJson?.Invoke(client, new object[] { instruction, messages, true });

            // Create handler that directly invokes callback (bypass JsonStreamParser)
            var streamHandler = new OpenAIStreamHandler(chunk =>
            {
                if (typeof(T) == typeof(string))
                {
                    onResponseParsed?.Invoke(chunk as T);
                }
            });

            await (Task<string>)
                sendRequestAsync?.Invoke(client, new object[] { jsonContent, streamHandler });

            return new Payload(
                endpointUrl,
                model,
                jsonContent,
                streamHandler.GetFullText(),
                streamHandler.GetTotalTokens()
            );
        }

        /// <summary>
        /// Fixed streaming method for GeminiClient that bypasses JsonStreamParser
        /// </summary>
        public static async Task<Payload> GetStreamingChatCompletionAsync_Fixed_Gemini<T>(
            this IAIClient client,
            string instruction,
            List<(Role role, string message)> messages,
            Action<T> onResponseParsed
        ) where T : class
        {
            var clientType = client.GetType();
            var buildRequestJson = clientType.GetMethod(
                "BuildRequestJson",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            var sendRequestAsync = clientType.GetMethod(
                "SendRequestAsync",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            var endpointUrl = (string)
                clientType
                    .GetField("_endpointUrl", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(client);
            var model = (string)
                clientType
                    .GetField("_model", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(client);

            string jsonContent = (string)
                buildRequestJson?.Invoke(client, new object[] { instruction, messages, true });

            // Use GeminiStreamHandler with direct callback
            var handlerType = clientType.Assembly.GetType(
                "RimTalk.Client.Gemini.GeminiStreamHandler"
            );
            var streamHandler = (DownloadHandlerScript)
                Activator.CreateInstance(
                    handlerType,
                    new Action<string>(chunk =>
                    {
                        if (typeof(T) == typeof(string))
                        {
                            onResponseParsed?.Invoke(chunk as T);
                        }
                    })
                );

            await (Task<string>)
                sendRequestAsync?.Invoke(client, new object[] { jsonContent, streamHandler });

            var getFullText = handlerType.GetMethod("GetFullText");
            var getTotalTokens = handlerType.GetMethod("GetTotalTokens");
            string fullText = (string)getFullText?.Invoke(streamHandler, null);
            int totalTokens = (int)(getTotalTokens?.Invoke(streamHandler, null) ?? 0);

            return new Payload(endpointUrl, model, jsonContent, fullText, totalTokens);
        }

        /// <summary>
        /// Unified fixed streaming method that auto-detects client type
        /// </summary>
        public static async Task<Payload> GetFixedStreamingChatCompletionAsync<T>(
            this IAIClient client,
            string instruction,
            List<(Role role, string message)> messages,
            Action<T> onResponseParsed
        ) where T : class
        {
            if (client is OpenAIClient openAIClient)
            {
                return await openAIClient.GetStreamingChatCompletionAsync_Fixed(
                    instruction,
                    messages,
                    onResponseParsed
                );
            }
            else if (client.GetType().Name == "GeminiClient")
            {
                return await client.GetStreamingChatCompletionAsync_Fixed_Gemini(
                    instruction,
                    messages,
                    onResponseParsed
                );
            }
            else
            {
                // Fallback to original (buggy) implementation
                Log.Warning(
                    $"[RimTalk-Quests] Unsupported client type: {client.GetType().Name}, using original streaming"
                );
                return await client.GetStreamingChatCompletionAsync(
                    instruction,
                    messages,
                    onResponseParsed
                );
            }
        }
    }
}
