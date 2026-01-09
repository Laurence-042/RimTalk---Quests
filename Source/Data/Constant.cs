using Verse;

namespace RimTalkQuests
{
    public static class Constant
    {
        public static string GetDefaultQuestInstruction()
        {
            return $@"You are enhancing a RimWorld quest description.
Your task is NOT to summarize or rewrite mechanically,
but to add narrative weight and implied motivation.

Writing goals:
1. Expand vague quest elements into short in-universe narrative.
2. Avoid making the quest feel like a pure reward transaction.
3. Emphasize uncertainty, intention, or quiet tension where appropriate.
4. Match RimWorld's restrained, grounded sci-fi tone (no epic fantasy).

Constraints:
- Write in {LanguageDatabase.activeLanguage.info.friendlyNameNative}
- Write 2–3 short paragraphs.
- Do NOT invent new gameplay mechanics or outcomes.
- Do NOT contradict the original quest text.
- Subtext is preferred over explicit exposition.
- The visitor should feel like a person with intent, not loot.
- PRESERVE all <color> tags from the original quest description exactly as they appear.
- When mentioning highlighted elements (names, items, numbers), use the same <color> tags.

Use the current scene and faction context when relevant,
but do not repeat raw data (dates, stats) directly.";
        }
    }
}
