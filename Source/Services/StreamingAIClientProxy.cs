using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RimTalk.Client;
using RimTalk.Client.Gemini;
using RimTalk.Client.OpenAI;
using RimTalk.Data;
using UnityEngine.Networking;
using Verse;

namespace RimTalkQuests.Services
{
    /// <summary>
    /// Proxy wrapper for IAIClient that fixes streaming callback issues.
    ///
    /// Problem: RimTalk's streaming implementation passes plain text content to callbacks,
    /// but then tries to parse it as JSON using JsonStreamParser, causing all callbacks to fail.
    ///
    /// Solution: This proxy bypasses JsonStreamParser and passes stream chunks directly to callbacks.
    /// </summary>
    public class StreamingAIClientProxy : IAIClient
    {
        private readonly IAIClient _innerClient;

        public StreamingAIClientProxy(IAIClient innerClient)
        {
            _innerClient = innerClient;
        }

        public Task<Payload> GetChatCompletionAsync(
            string instruction,
            List<(Role role, string message)> messages
        )
        {
            // Pass through to original implementation
            return _innerClient.GetChatCompletionAsync(instruction, messages);
        }

        public async Task<Payload> GetStreamingChatCompletionAsync<T>(
            string instruction,
            List<(Role role, string message)> messages,
            Action<T> onResponseParsed
        ) where T : class
        {
            // Handle different client types
            if (_innerClient is OpenAIClient openAIClient)
            {
                return await GetStreamingForOpenAI(
                    openAIClient,
                    instruction,
                    messages,
                    onResponseParsed
                );
            }
            else if (_innerClient is GeminiClient geminiClient)
            {
                return await GetStreamingForGemini(
                    geminiClient,
                    instruction,
                    messages,
                    onResponseParsed
                );
            }
            else
            {
                // Fallback: use original implementation
                Log.Warning(
                    $"[RimTalk-Quests] Unknown client type: {_innerClient.GetType().Name}, using original streaming"
                );
                return await _innerClient.GetStreamingChatCompletionAsync(
                    instruction,
                    messages,
                    onResponseParsed
                );
            }
        }

        private async Task<Payload> GetStreamingForOpenAI<T>(
            OpenAIClient client,
            string instruction,
            List<(Role role, string message)> messages,
            Action<T> onResponseParsed
        ) where T : class
        {
            // Use reflection to access private methods
            var clientType = typeof(OpenAIClient);
            var buildRequestJsonMethod = clientType.GetMethod(
                "BuildRequestJson",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            var sendRequestAsyncMethod = clientType.GetMethod(
                "SendRequestAsync",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            var endpointUrlField = clientType.GetField(
                "_endpointUrl",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            var modelField = clientType.GetField(
                "model",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            string jsonContent = (string)
                buildRequestJsonMethod.Invoke(client, new object[] { instruction, messages, true });
            string endpointUrl = (string)endpointUrlField.GetValue(client);
            string model = (string)modelField.GetValue(client);

            // Create StreamHandler that directly invokes callback (bypass JsonStreamParser)
            var streamHandler = new OpenAIStreamHandler(chunk =>
            {
                // Convert plain text chunk to T
                if (typeof(T) == typeof(string))
                {
                    onResponseParsed?.Invoke(chunk as T);
                }
            });

            await (Task<string>)
                sendRequestAsyncMethod.Invoke(client, new object[] { jsonContent, streamHandler });

            return new Payload(
                endpointUrl,
                model,
                jsonContent,
                streamHandler.GetFullText(),
                streamHandler.GetTotalTokens()
            );
        }

        private async Task<Payload> GetStreamingForGemini<T>(
            IAIClient client,
            string instruction,
            List<(Role role, string message)> messages,
            Action<T> onResponseParsed
        ) where T : class
        {
            // Use reflection to access GeminiClient private methods
            var clientType = client.GetType();
            var buildRequestJsonMethod = clientType.GetMethod(
                "BuildRequestJson",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            var sendRequestAsyncMethod = clientType.GetMethod(
                "SendRequestAsync",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            var endpointUrlField = clientType.GetField(
                "_endpointUrl",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            var modelField = clientType.GetField(
                "_model",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            string jsonContent = (string)
                buildRequestJsonMethod.Invoke(client, new object[] { instruction, messages, true });
            string endpointUrl = (string)endpointUrlField.GetValue(client);
            string model = (string)modelField.GetValue(client);

            // Use GeminiStreamHandler
            var geminiStreamHandlerType = clientType.Assembly.GetType(
                "RimTalk.Client.Gemini.GeminiStreamHandler"
            );
            var streamHandler = (DownloadHandlerScript)
                Activator.CreateInstance(
                    geminiStreamHandlerType,
                    new Action<string>(chunk =>
                    {
                        if (typeof(T) == typeof(string))
                        {
                            onResponseParsed?.Invoke(chunk as T);
                        }
                    })
                );

            await (Task<string>)
                sendRequestAsyncMethod.Invoke(client, new object[] { jsonContent, streamHandler });

            var getFullTextMethod = geminiStreamHandlerType.GetMethod("GetFullText");
            var getTotalTokensMethod = geminiStreamHandlerType.GetMethod("GetTotalTokens");

            string fullText = (string)getFullTextMethod.Invoke(streamHandler, null);
            int totalTokens = (int)getTotalTokensMethod.Invoke(streamHandler, null);

            return new Payload(endpointUrl, model, jsonContent, fullText, totalTokens);
        }
    }
}
