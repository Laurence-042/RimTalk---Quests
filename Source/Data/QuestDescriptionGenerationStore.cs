using System.Collections.Generic;
using Verse;

namespace RimTalkQuests.Data
{
    /// <summary>
    /// Persists per-quest data required to regenerate AI quest descriptions.
    ///
    /// It stores the pristine (pre-AI) description for each quest, keyed by quest id,
    /// so that:
    ///  - Regenerating a description always extends the original quest text instead of
    ///    the previously generated narrative.
    ///  - The original text survives save/load, because this component is registered on
    ///    the Game and its data is scribed into the save file.
    /// </summary>
    public class QuestDescriptionGenerationStore : GameComponent
    {
        private Dictionary<int, string> originalDescriptions = new Dictionary<int, string>();

        public QuestDescriptionGenerationStore(Game game) { }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(
                ref originalDescriptions,
                "originalDescriptions",
                LookMode.Value,
                LookMode.Value
            );

            if (Scribe.mode == LoadSaveMode.PostLoadInit && originalDescriptions == null)
            {
                originalDescriptions = new Dictionary<int, string>();
            }
        }

        public bool TryGetOriginal(int questId, out string description)
        {
            if (originalDescriptions == null)
            {
                description = null;
                return false;
            }

            return originalDescriptions.TryGetValue(questId, out description);
        }

        public void SetOriginal(int questId, string description)
        {
            if (originalDescriptions == null)
            {
                originalDescriptions = new Dictionary<int, string>();
            }

            originalDescriptions[questId] = description;
        }

        public void Remove(int questId)
        {
            originalDescriptions?.Remove(questId);
        }
    }
}