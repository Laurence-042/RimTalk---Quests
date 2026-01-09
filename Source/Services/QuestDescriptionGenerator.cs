using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace RimTalkQuests.Services
{
    /// <summary>
    /// Service for generating AI-powered quest descriptions.
    /// 
    /// This service integrates with RimTalk's AI functionality to create
    /// dynamic, context-aware quest narratives.
    /// </summary>
    public static class QuestDescriptionGenerator
    {
        private static Dictionary<int, QuestAIData> _questCache = new Dictionary<int, QuestAIData>();
        private static HashSet<int> _processingQuests = new HashSet<int>();

        private class QuestAIData
        {
            public string Description;
            public string Name;
        }

        public static int CacheSize => _questCache.Count;

        public static void ClearCache()
        {
            _questCache.Clear();
            _processingQuests.Clear();
        }

        /// <summary>
        /// Generates an AI-powered description for a quest asynchronously
        /// </summary>
        public static async void GenerateQuestDescriptionAsync(Quest quest)
        {
            try
            {
                if (quest == null)
                    return;

                int questId = quest.id;

                // Skip if already cached or processing
                if (_questCache.ContainsKey(questId) || _processingQuests.Contains(questId))
                    return;

                _processingQuests.Add(questId);

                if (Prefs.DevMode)
                    Log.Message($"[RimTalk-Quests] Generating AI description for quest: {quest.name}");

                // Build the prompt
                string prompt = BuildQuestPrompt(quest);
                string instruction = BuildSystemInstruction();

                // Call RimTalk's AI service
                var result = await CallRimTalkAI(instruction, prompt);

                if (result != null)
                {
                    // Parse the result
                    var aiData = ParseAIResponse(result, quest);
                    
                    if (RimTalkQuestsMod.Settings.cacheDescriptions)
                    {
                        _questCache[questId] = aiData;
                    }

                    if (Prefs.DevMode)
                        Log.Message($"[RimTalk-Quests] Successfully generated AI description for: {quest.name}");
                }
                else
                {
                    if (Prefs.DevMode)
                        Log.Warning($"[RimTalk-Quests] Failed to generate description for quest: {quest.name}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-Quests] Error generating quest description: {ex}");
            }
            finally
            {
                _processingQuests.Remove(quest.id);
            }
        }

        /// <summary>
        /// Gets cached AI description for a quest
        /// </summary>
        public static string GetCachedDescription(Quest quest)
        {
            if (quest == null)
                return null;

            if (_questCache.TryGetValue(quest.id, out var data))
            {
                return data.Description;
            }

            return null;
        }

        /// <summary>
        /// Gets cached AI name for a quest
        /// </summary>
        public static string GetCachedName(Quest quest)
        {
            if (quest == null)
                return null;

            if (_questCache.TryGetValue(quest.id, out var data))
            {
                return data.Name;
            }

            return null;
        }

        /// <summary>
        /// Builds the system instruction for the AI
        /// </summary>
        private static string BuildSystemInstruction()
        {
            return @"You are a creative quest writer for RimWorld, a sci-fi colony simulation game. 
Your task is to rewrite quest descriptions to make them more engaging, narrative-driven, and immersive.
Keep the core information intact but add flavor, personality, and storytelling.
Respond in this JSON format:
{
  ""name"": ""A creative quest title"",
  ""description"": ""The detailed quest description with narrative flair""
}";
        }

        /// <summary>
        /// Builds the prompt for AI quest description generation
        /// </summary>
        private static string BuildQuestPrompt(Quest quest)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("Rewrite this RimWorld quest with creative narrative flair:");
            sb.AppendLine();
            sb.AppendLine($"Original Title: {quest.name}");
            sb.AppendLine($"Original Description: {quest.description}");
            sb.AppendLine();
            
            // Add quest context
            if (quest.root != null)
            {
                sb.AppendLine($"Quest Type: {quest.root.defName}");
            }

            // Add challenge rating
            if (quest.challengeRating > 0)
            {
                sb.AppendLine($"Challenge Level: {quest.challengeRating}");
            }

            // Add colony context if available
            if (Find.CurrentMap != null)
            {
                var map = Find.CurrentMap;
                var colonists = map.mapPawns.FreeColonistsSpawnedCount;
                sb.AppendLine($"Colony has {colonists} colonists");
            }

            // Check if quest has multiple parts (complex quest)
            if (quest.PartsListForReading != null && quest.PartsListForReading.Count > 1)
            {
                sb.AppendLine($"Quest has {quest.PartsListForReading.Count} objectives");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Calls RimTalk's AI service to generate quest description
        /// </summary>
        private static async Task<string> CallRimTalkAI(string instruction, string prompt)
        {
            try
            {
                // Use reflection to access RimTalk's AIService
                var rimTalkAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "RimTalk");

                if (rimTalkAssembly == null)
                {
                    Log.Warning("[RimTalk-Quests] RimTalk assembly not found");
                    return null;
                }

                // Get AIClientFactory type
                var factoryType = rimTalkAssembly.GetType("RimTalk.Client.AIClientFactory");
                if (factoryType == null)
                {
                    Log.Warning("[RimTalk-Quests] AIClientFactory type not found");
                    return null;
                }

                // Get the GetAIClientAsync method
                var getClientMethod = factoryType.GetMethod("GetAIClientAsync");
                if (getClientMethod == null)
                {
                    Log.Warning("[RimTalk-Quests] GetAIClientAsync method not found");
                    return null;
                }

                // Call GetAIClientAsync
                var clientTask = (Task)getClientMethod.Invoke(null, null);
                await clientTask.ConfigureAwait(false);

                // Get the result (IAIClient)
                var resultProperty = clientTask.GetType().GetProperty("Result");
                var client = resultProperty?.GetValue(clientTask);

                if (client == null)
                {
                    Log.Warning("[RimTalk-Quests] Failed to get AI client - check RimTalk configuration");
                    return null;
                }

                // Get IAIClient.GetChatCompletionAsync method
                var chatMethod = client.GetType().GetMethod("GetChatCompletionAsync");
                if (chatMethod == null)
                {
                    Log.Warning("[RimTalk-Quests] GetChatCompletionAsync method not found");
                    return null;
                }

                // Build message list
                var roleType = rimTalkAssembly.GetType("RimTalk.Data.Role");
                var userRole = Enum.Parse(roleType, "User");

                var tupleType = typeof(ValueTuple<,>).MakeGenericType(roleType, typeof(string));
                var message = Activator.CreateInstance(tupleType, userRole, prompt);

                var listType = typeof(List<>).MakeGenericType(tupleType);
                var messages = Activator.CreateInstance(listType) as System.Collections.IList;
                messages.Add(message);

                // Call GetChatCompletionAsync
                var chatTask = (Task)chatMethod.Invoke(client, new object[] { instruction, messages });
                await chatTask.ConfigureAwait(false);

                // Get the payload result
                var payloadProperty = chatTask.GetType().GetProperty("Result");
                var payload = payloadProperty?.GetValue(chatTask);

                if (payload == null)
                    return null;

                // Extract response from payload
                var responseProperty = payload.GetType().GetProperty("Response");
                var response = responseProperty?.GetValue(payload) as string;

                return response;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-Quests] Error calling RimTalk AI service: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Parses AI response and extracts name and description
        /// </summary>
        private static QuestAIData ParseAIResponse(string response, Quest quest)
        {
            try
            {
                // Try to parse as JSON first
                if (response.Contains("{") && response.Contains("}"))
                {
                    int startIndex = response.IndexOf('{');
                    int endIndex = response.LastIndexOf('}') + 1;
                    string jsonPart = response.Substring(startIndex, endIndex - startIndex);

                    // Simple JSON parsing (avoiding dependencies)
                    string name = ExtractJsonValue(jsonPart, "name");
                    string description = ExtractJsonValue(jsonPart, "description");

                    return new QuestAIData
                    {
                        Name = !string.IsNullOrEmpty(name) ? name : quest.name,
                        Description = !string.IsNullOrEmpty(description) ? description : response
                    };
                }

                // Fallback: use the entire response as description
                return new QuestAIData
                {
                    Name = quest.name,
                    Description = response
                };
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimTalk-Quests] Error parsing AI response: {ex}");
                return new QuestAIData
                {
                    Name = quest.name,
                    Description = response
                };
            }
        }

        /// <summary>
        /// Simple JSON value extraction
        /// </summary>
        private static string ExtractJsonValue(string json, string key)
        {
            try
            {
                string searchKey = $"\"{key}\"";
                int keyIndex = json.IndexOf(searchKey);
                if (keyIndex == -1)
                    return null;

                int colonIndex = json.IndexOf(':', keyIndex);
                if (colonIndex == -1)
                    return null;

                int startQuote = json.IndexOf('"', colonIndex);
                if (startQuote == -1)
                    return null;

                int endQuote = startQuote + 1;
                while (endQuote < json.Length)
                {
                    if (json[endQuote] == '"' && json[endQuote - 1] != '\\')
                        break;
                    endQuote++;
                }

                if (endQuote >= json.Length)
                    return null;

                return json.Substring(startQuote + 1, endQuote - startQuote - 1)
                    .Replace("\\n", "\n")
                    .Replace("\\\"", "\"");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if RimTalk's AI service is available and configured
        /// </summary>
        public static bool IsAIServiceAvailable()
        {
            try
            {
                if (!ModsConfig.IsActive("cj.rimtalk"))
                    return false;

                // Try to access RimTalk.Settings to check if API is configured
                var rimTalkAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "RimTalk");

                if (rimTalkAssembly == null)
                    return false;

                var settingsType = rimTalkAssembly.GetType("RimTalk.Settings");
                if (settingsType == null)
                    return false;

                var getMethod = settingsType.GetMethod("Get");
                if (getMethod == null)
                    return false;

                var settings = getMethod.Invoke(null, null);
                if (settings == null)
                    return false;

                // Check if there's an active config
                var getActiveConfigMethod = settings.GetType().GetMethod("GetActiveConfig");
                if (getActiveConfigMethod == null)
                    return false;

                var activeConfig = getActiveConfigMethod.Invoke(settings, null);
                return activeConfig != null;
            }
            catch (Exception ex)
            {
                if (Prefs.DevMode)
                    Log.Warning($"[RimTalk-Quests] Error checking AI service availability: {ex}");
                return false;
            }
        }
    }
}
