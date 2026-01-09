using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace RimTalkQuests
{
    /// <summary>
    /// Main mod class for RimTalk-Quests
    /// 
    /// This mod is a derivative work based on RimTalk by juicy, licensed under CC BY-NC-SA 4.0.
    /// It reuses RimTalk's AI model configuration and API calling functionality to generate
    /// dynamic quest descriptions.
    /// </summary>
    [StaticConstructorOnStartup]
    public class RimTalkQuestsMod : Mod
    {
        public static RimTalkQuestsMod Instance { get; private set; }
        public static Harmony HarmonyInstance { get; private set; }

        public RimTalkQuestsMod(ModContentPack content) : base(content)
        {
            Instance = this;
            
            Log.Message("[RimTalk-Quests] Initializing...");
            
            // Check if RimTalk is loaded
            if (!ModsConfig.IsActive("cj.rimtalk"))
            {
                Log.Error("[RimTalk-Quests] RimTalk is not loaded! This mod requires RimTalk to function.");
                return;
            }
            
            try
            {
                // Apply Harmony patches
                HarmonyInstance = new Harmony("rimtalk.quests");
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                
                Log.Message("[RimTalk-Quests] Successfully initialized with Harmony patches applied.");
                Log.Message("[RimTalk-Quests] Attribution: Based on RimTalk by juicy (CC BY-NC-SA 4.0)");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-Quests] Failed to initialize: {ex}");
            }
        }

        public override string SettingsCategory()
        {
            return "RimTalk - AI Quests";
        }

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            // Settings UI will be implemented here
            Verse.Widgets.Label(inRect, "RimTalk-Quests uses your existing RimTalk AI configuration.\nNo additional settings required.");
        }
    }
}
