using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using RimTalk.Client;
using RimTalk.Data;
using RimTalk.Util;
using Verse;

namespace RimTalkQuests.Services
{
    /// <summary>
    /// Service for generating AI-powered quest descriptions.
    /// This service integrates with RimTalk's AI functionality to create
    /// dynamic, context-aware quest narratives.
    /// </summary>
    public static class QuestDescriptionGenerator
    {
        private static readonly Dictionary<int, QuestAIData> _questCache =
            new Dictionary<int, QuestAIData>();
        private static readonly HashSet<int> _processingQuests = new HashSet<int>();

        private class QuestAIData
        {
            public TaggedString Description;
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
                {
                    Log.Message(
                        $"[RimTalk-Quests] Generating AI description for quest: {quest.name}"
                    );
                }

                // Build the prompt
                string prompt = BuildQuestPrompt(quest);
                string instruction = BuildSystemInstruction();

                if (Prefs.DevMode)
                {
                    var config = RimTalk.Settings.Get().GetActiveConfig();
                    var model = config?.SelectedModel ?? "Unknown";
                    Log.Message($"[RimTalk-Quests] Using model: {model}");
                    Log.Message($"[RimTalk-Quests] Instruction:\n{instruction}");
                    Log.Message($"[RimTalk-Quests] Prompt:\n{prompt}");
                }

                // Call RimTalk's AI service
                var result = await CallRimTalkAI(instruction, prompt);

                if (Prefs.DevMode && result != null)
                {
                    Log.Message($"[RimTalk-Quests] AI Response:\n{result}");
                }

                if (result != null)
                {
                    // Parse the result and directly update quest fields
                    var aiData = ParseAIResponse(result, quest);

                    // Directly modify quest fields instead of caching
                    if (!aiData.Description.NullOrEmpty())
                    {
                        quest.description = aiData.Description;
                    }

                    if (
                        !string.IsNullOrEmpty(aiData.Name)
                        && RimTalkQuestsMod.Settings.enableAIDescriptions
                    )
                    {
                        quest.name = aiData.Name;
                    }

                    if (Prefs.DevMode)
                        Log.Message($"[RimTalk-Quests] Successfully updated quest: {quest.name}");
                }
                else
                {
                    if (Prefs.DevMode)
                    {
                        Log.Warning(
                            $"[RimTalk-Quests] Failed to generate description for quest: {quest.name}"
                        );
                    }
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
        /// Builds the system instruction for the AI based on RimTalk's configuration
        /// </summary>
        private static string BuildSystemInstruction()
        {
            var settings = RimTalk.Settings.Get();
            var lang = Constant.Lang;
            
            // Get base instruction from RimTalk (respects user customization)
            var baseInstruction = string.IsNullOrWhiteSpace(settings.CustomInstruction)
                ? Constant.DefaultInstruction
                : settings.CustomInstruction;

            // Quest-specific instruction
            var questInstruction = $@"

QUEST REWRITING TASK:
Rewrite quest descriptions to be more engaging and narrative-driven in {lang}.

RULES:
- Preserve <color=...>tags</color> and <b>bold tags</b> EXACTLY
- Keep numerical values and proper names unchanged
- Keep core information intact, add narrative flair
- Output JSON: {{""name"": ""title in {lang}"", ""description"": ""description in {lang} with preserved tags""}}";

            return baseInstruction + questInstruction;
        }

        /// <summary>
        /// Builds the prompt for AI quest description generation
        /// </summary>
        private static string BuildQuestPrompt(Quest quest)
        {
            var sb = new StringBuilder();

            // Quest basic info
            sb.AppendLine($"Quest Title: {quest.name}");
            sb.AppendLine($"Quest Description: {quest.description}");
            sb.AppendLine();

            // Quest metadata
            if (quest.root != null)
            {
                sb.AppendLine($"Type: {quest.root.defName}");
            }

            if (quest.challengeRating > 0)
            {
                sb.AppendLine($"Challenge: {quest.challengeRating}");
            }

            // Scene information (reusing RimTalk's mechanism)
            AppendSceneContext(sb);

            // Faction history context
            AppendFactionContext(sb, quest);

            return sb.ToString();
        }

        /// <summary>
        /// Appends scene context (time, weather, location, wealth) like RimTalk
        /// </summary>
        private static void AppendSceneContext(StringBuilder sb)
        {
            var gameData = CommonUtil.GetInGameData();
            var currentMap = Find.CurrentMap;

            if (currentMap == null)
                return;

            sb.AppendLine();
            sb.AppendLine("--- Current Scene ---");

            // Time and date
            sb.AppendLine($"Time: {gameData.Hour12HString}");
            sb.AppendLine($"Date: {gameData.DateString}");
            sb.AppendLine($"Season: {gameData.SeasonString}");
            sb.AppendLine($"Weather: {gameData.WeatherString}");

            // Colony info
            var colonists = currentMap.mapPawns.FreeColonistsSpawnedCount;
            sb.AppendLine($"Colony: {colonists} colonists");

            var wealth = currentMap.wealthWatcher.WealthTotal;
            sb.AppendLine($"Wealth: {Describer.Wealth(wealth)}");

            // Location
            if (currentMap.Parent != null)
            {
                sb.AppendLine($"Location: {currentMap.Parent.Label}");
            }
        }

        /// <summary>
        /// Appends faction history and relationship context
        /// </summary>
        private static void AppendFactionContext(StringBuilder sb, Quest quest)
        {
            // Get involved factions
            var factions = quest.InvolvedFactions?.ToList();
            if (factions == null || factions.Count == 0)
                return;

            var mainFaction = factions.FirstOrDefault();
            if (mainFaction == null)
                return;

            sb.AppendLine();
            sb.AppendLine("--- Faction Context ---");
            sb.AppendLine($"From: {mainFaction.Name}");

            // Faction relationship
            var playerFaction = Faction.OfPlayer;
            if (playerFaction != null)
            {
                var relation = mainFaction.RelationKindWith(playerFaction);
                var goodwill = mainFaction.GoodwillWith(playerFaction);
                sb.AppendLine($"Relationship: {relation} (goodwill: {goodwill})");
            }

            // Historical quests from this faction
            AppendFactionQuestHistory(sb, mainFaction);
        }

        /// <summary>
        /// Appends recent quest history with this faction
        /// </summary>
        private static void AppendFactionQuestHistory(StringBuilder sb, Faction faction)
        {
            var questManager = Find.QuestManager;
            if (questManager == null)
                return;

            // Get recent completed/failed quests from this faction
            var recentQuests = questManager.QuestsListForReading
                .Where(
                    q =>
                        q.Historical
                        && q.InvolvedFactions.Contains(faction)
                        && q.TicksSinceCleanup < GenDate.TicksPerQuadrum
                ) // Last quadrum
                .OrderByDescending(q => q.cleanupTick)
                .Take(3)
                .ToList();

            if (recentQuests.Any())
            {
                sb.AppendLine($"Recent history with {faction.Name}:");
                foreach (var q in recentQuests)
                {
                    var outcome = q.State switch
                    {
                        QuestState.EndedSuccess => "succeeded",
                        QuestState.EndedFailed => "failed",
                        _ => "ended"
                    };
                    sb.AppendLine($"  - {q.name} ({outcome})");
                }
            }
        }

        /// <summary>
        /// Calls RimTalk's AI service to generate quest description
        /// </summary>
        private static async Task<string> CallRimTalkAI(string instruction, string prompt)
        {
            // Get AI client from RimTalk
            var client = await AIClientFactory.GetAIClientAsync();
            if (client == null)
            {
                Log.Warning(
                    "[RimTalk-Quests] Failed to get AI client - check RimTalk configuration"
                );
                return null;
            }

            // Build message list
            var messages = new List<(Role, string)> { (Role.User, prompt) };

            // Call AI service
            var payload = await client.GetChatCompletionAsync(instruction, messages);

            return payload?.Response;
        }

        /// <summary>
        /// Parses AI response and extracts name and description
        /// </summary>
        private static QuestAIData ParseAIResponse(string response, Quest quest)
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
                    Description = !string.IsNullOrEmpty(description)
                        ? new TaggedString(description)
                        : new TaggedString(response)
                };
            }

            // Fallback: use the entire response as description
            return new QuestAIData { Name = quest.name, Description = new TaggedString(response) };
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
            if (!ModsConfig.IsActive("cj.rimtalk"))
                return false;

            // Check if RimTalk has an active configuration
            var settings = RimTalk.Settings.Get();
            var activeConfig = settings?.GetActiveConfig();

            return activeConfig != null;
        }
    }
}
