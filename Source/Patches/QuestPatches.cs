using System;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace RimTalkQuests.Patches
{
    /// <summary>
    /// Harmony patch to intercept quest acceptance and generate AI descriptions.
    /// 
    /// This patch hooks into RimWorld's quest system to provide dynamic, context-aware
    /// quest descriptions using RimTalk's AI integration.
    /// </summary>
    [HarmonyPatch(typeof(Quest), nameof(Quest.Accept))]
    public static class QuestAcceptPatch
    {
        /// <summary>
        /// Postfix patch that runs after a quest is accepted
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

                // Generate AI description and directly modify the quest fields
                Services.QuestDescriptionGenerator.GenerateQuestDescriptionAsync(__instance);
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-Quests] Error in quest accept patch: {ex}");
            }
        }
    }

    /// <summary>
    /// Patch to intercept quest when added to the world
    /// </summary>
    [HarmonyPatch(typeof(Quest), nameof(Quest.PostAdded))]
    public static class QuestPostAddedPatch
    {
        /// <summary>
        /// Postfix patch that runs after a quest is added
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

                // Generate AI description for new quests
                Services.QuestDescriptionGenerator.GenerateQuestDescriptionAsync(__instance);
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-Quests] Error in quest post added patch: {ex}");
            }
        }
    }
}
