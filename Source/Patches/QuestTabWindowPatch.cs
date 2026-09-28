using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimTalkQuests.Patches
{
    /// <summary>
    /// Harmony patch that adds a "Regenerate" button to the quest detail panel in
    /// RimWorld's quest tab.
    ///
    /// The button is drawn right after the quest description by postfixing
    /// <see cref="MainTabWindow_Quests.DoDescription"/>, so it scrolls together with
    /// the description and sits directly under the generated narrative.
    /// </summary>
    [HarmonyPatch(typeof(MainTabWindow_Quests))]
    public static class QuestTabWindowPatch
    {
        private const float ButtonWidth = 180f;
        private const float ButtonHeight = 32f;
        private const float GapAbove = 6f;
        private const float GapBelow = 10f;

        private static Quest SelectedQuest(MainTabWindow_Quests instance)
        {
            return AccessTools.Field(typeof(MainTabWindow_Quests), "selected")?.GetValue(instance)
                as Quest;
        }

        [HarmonyPostfix]
        [HarmonyPatch("DoDescription")]
        public static void DoDescriptionPostfix(
            MainTabWindow_Quests __instance,
            Rect innerRect,
            ref float curY
        )
        {
            try
            {
                if (!RimTalkQuestsMod.Settings.enableAIDescriptions)
                    return;

                var quest = SelectedQuest(__instance);
                if (quest == null)
                    return;

                bool available = Services.QuestDescriptionGenerator.IsAIServiceAvailable();
                bool processing = Services.QuestDescriptionGenerator.IsProcessing(quest.id);

                Rect buttonRect = new Rect(
                    innerRect.xMax - ButtonWidth,
                    curY + GapAbove,
                    ButtonWidth,
                    ButtonHeight
                );

                string label;
                if (processing)
                {
                    label = "RimTalkQuests.Regenerate.InProgress".Translate();
                }
                else
                {
                    label = "RimTalkQuests.Regenerate".Translate();
                }

                bool previousEnabled = GUI.enabled;
                GUI.enabled = available && !processing;

                if (Widgets.ButtonText(buttonRect, label))
                {
                    Services.QuestDescriptionGenerator.RegenerateQuestDescription(quest);
                }

                GUI.enabled = previousEnabled;

                if (Mouse.IsOver(buttonRect))
                {
                    string tooltip = available
                        ? "RimTalkQuests.Regenerate.Tooltip".Translate()
                        : "RimTalkQuests.Regenerate.Unavailable".Translate();
                    TooltipHandler.TipRegion(buttonRect, tooltip);
                }

                // Push subsequent content (acceptance info, rewards, ...) below the button.
                curY += GapAbove + ButtonHeight + GapBelow;
            }
            catch (System.Exception ex)
            {
                Log.ErrorOnce(
                    $"[RimTalk-Quests] Error drawing regenerate button: {ex}",
                    "RimTalkQuests.RegenerateButton".GetHashCode()
                );
            }
        }
    }
}