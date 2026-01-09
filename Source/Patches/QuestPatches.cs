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
    [HarmonyPatch("AddToWorld")]
    public static class QuestGenerationPatch
    {
        /// <summary>
        /// Postfix patch that runs after a quest is added to the world
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(Quest __instance)
        {
            try
            {
                if (__instance == null || !RimTalkQuestsMod.Settings.enableAIDescriptions)
                    return;

                // Check if RimTalk is properly configured
                if (!Services.QuestDescriptionGenerator.IsAIServiceAvailable())
                {
                    if (Prefs.DevMode)
                        Log.Warning("[RimTalk-Quests] AI service not available. Make sure RimTalk is configured with an API key.");
                    return;
                }

                // Generate AI description asynchronously
                Services.QuestDescriptionGenerator.GenerateQuestDescriptionAsync(__instance);
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-Quests] Error in quest generation patch: {ex}");
            }
        }
    }

    /// <summary>
    /// Patch to intercept quest description access
    /// This allows us to provide the AI-generated description when the quest is displayed
    /// </summary>
    [HarmonyPatch(typeof(Quest))]
    [HarmonyPatch("get_description")]
    public static class QuestDescriptionGetterPatch
    {
        /// <summary>
        /// Postfix to replace description with AI-generated one if available
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(Quest __instance, ref string __result)
        {
            try
            {
                if (__instance == null || !RimTalkQuestsMod.Settings.enableAIDescriptions)
                    return;

                // Try to get cached AI description
                string aiDescription = Services.QuestDescriptionGenerator.GetCachedDescription(__instance);
                if (!string.IsNullOrEmpty(aiDescription))
                {
                    __result = aiDescription;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-Quests] Error in quest description getter patch: {ex}");
            }
        }
    }

    /// <summary>
    /// Patch to intercept quest name access for more creative quest titles
    /// </summary>
    [HarmonyPatch(typeof(Quest))]
    [HarmonyPatch("get_name")]
    public static class QuestNameGetterPatch
    {
        /// <summary>
        /// Postfix to optionally replace quest name with AI-generated one
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(Quest __instance, ref string __result)
        {
            try
            {
                if (__instance == null || !RimTalkQuestsMod.Settings.enableAIDescriptions)
                    return;

                // Try to get cached AI name
                string aiName = Services.QuestDescriptionGenerator.GetCachedName(__instance);
                if (!string.IsNullOrEmpty(aiName))
                {
                    __result = aiName;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-Quests] Error in quest name getter patch: {ex}");
            }
        }
    }
}
