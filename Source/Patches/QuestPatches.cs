using System;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace RimTalkQuests.Patches
{
    /// <summary>
    /// Harmony patch to intercept quest generation and replace descriptions with AI-generated content.
    /// 
    /// This patch hooks into RimWorld's quest generation system to provide dynamic, context-aware
    /// quest descriptions using RimTalk's AI integration.
    /// </summary>
    [HarmonyPatch(typeof(Quest))]
    [HarmonyPatch(nameof(Quest.GenerateQuestAndMakeAvailable))]
    public static class QuestGenerationPatch
    {
        /// <summary>
        /// Postfix patch that runs after a quest is generated
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(Quest __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                Log.Message($"[RimTalk-Quests] Quest generated: {__instance.name}");
                
                // TODO: Implement AI description generation
                // This will call RimTalk's AI service to generate a custom description
                // based on the quest context, colony state, and quest parameters
                
                // Example structure (to be implemented):
                // 1. Extract quest context (type, rewards, challenges, etc.)
                // 2. Build prompt using RimTalk's ContextBuilder
                // 3. Call AI service (reusing RimTalk's AIService)
                // 4. Replace quest description with AI-generated text
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-Quests] Error in quest generation patch: {ex}");
            }
        }
    }

    /// <summary>
    /// Patch to intercept quest description display
    /// </summary>
    [HarmonyPatch(typeof(Quest))]
    [HarmonyPatch(nameof(Quest.PartsListForReading), MethodType.Getter)]
    public static class QuestDescriptionPatch
    {
        /// <summary>
        /// Prefix patch that can modify quest content before it's displayed
        /// </summary>
        [HarmonyPrefix]
        public static void Prefix(Quest __instance)
        {
            try
            {
                // TODO: Implement description modification logic
                // This is where we can inject AI-generated descriptions
                // into the quest's display text
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-Quests] Error in quest description patch: {ex}");
            }
        }
    }
}
