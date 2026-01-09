using System;
using System.Collections.Generic;
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
        /// <summary>
        /// Generates an AI-powered description for a quest
        /// </summary>
        /// <param name="quest">The quest to generate a description for</param>
        /// <returns>AI-generated quest description</returns>
        public static string GenerateQuestDescription(Quest quest)
        {
            try
            {
                if (quest == null)
                    return null;

                // Build context for the AI prompt
                string context = BuildQuestContext(quest);
                
                // TODO: Call RimTalk's AI service
                // This will use RimTalk.AIService or RimTalk.Client.IAIClient
                // to generate the description
                
                Log.Message($"[RimTalk-Quests] Generating AI description for quest: {quest.name}");
                
                // Placeholder return - will be replaced with actual AI call
                return quest.description;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-Quests] Error generating quest description: {ex}");
                return quest.description; // Fallback to original description
            }
        }

        /// <summary>
        /// Builds context information about the quest for the AI prompt
        /// </summary>
        private static string BuildQuestContext(Quest quest)
        {
            var context = new List<string>();
            
            context.Add($"Quest Name: {quest.name}");
            context.Add($"Quest Type: {quest.root?.defName ?? "Unknown"}");
            
            // Add information about quest parts
            if (quest.PartsListForReading != null)
            {
                context.Add($"Quest has {quest.PartsListForReading.Count} parts");
            }
            
            // Add challenge rating if available
            if (quest.challengeRating > 0)
            {
                context.Add($"Challenge Rating: {quest.challengeRating}");
            }
            
            // TODO: Add more context:
            // - Colony state (wealth, population, tech level)
            // - Current events/threats
            // - Quest rewards
            // - Quest objectives
            
            return string.Join("\n", context);
        }

        /// <summary>
        /// Checks if RimTalk's AI service is available and configured
        /// </summary>
        public static bool IsAIServiceAvailable()
        {
            try
            {
                // TODO: Check if RimTalk.AIService is initialized and has a valid API key
                // This will require reflection or direct access to RimTalk's public API
                
                return ModsConfig.IsActive("cj.rimtalk");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-Quests] Error checking AI service availability: {ex}");
                return false;
            }
        }
    }
}
