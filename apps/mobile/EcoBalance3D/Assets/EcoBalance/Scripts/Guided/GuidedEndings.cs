// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5)
using UnityEngine;

/// <summary>
/// The five ecosystem endings (research doc, "Ecosystem Outcomes &amp; Endings" and "Ending UI Content
/// Reference"). One entry per Ending_ID with the doc's authoritative text, reused by the ending screen and the
/// Ending Gallery so the wording is never duplicated. The array order is the classification priority (and the
/// gallery's saved bit order, so existing unlocks stay valid):
///   0 END05 Ecosystem Collapse, 1 END03 Predator-Free Highlands, 2 END04 Disappearing Herd,
///   3 END01 Thriving Highlands, 4 END02 Fragile Balance.
/// </summary>
public static class GuidedEndings
{
    public sealed class Ending
    {
        public string id, title, description, meaning, takeaway, card;
        /// <summary>Colours of the code-drawn illustration: sky, hills, ground.</summary>
        public Color sky, hills, ground;
    }

    public static readonly Ending[] All =
    {
        new Ending {
            id = "END05", title = "Ecosystem Collapse",
            description = "Neither Blue Sheep nor Snow Leopards remain in the highlands. The animal food web shown in this story has collapsed, even if some vegetation is still growing.",
            meaning = "Both animal populations are zero at evaluation. Changes in vegetation, grazing, hunting and animal survival can combine over the seasons. This outcome necessarily means the selected focal animal has died and its storyline has ended early.",
            takeaway = "When plants, prey and predators are connected, the loss of one population can affect the survival of others.",
            card = "No animals are left in the highlands.",
            sky = new Color32(0x8E, 0x8A, 0x9E, 0xFF), hills = new Color32(0x6B, 0x64, 0x72, 0xFF), ground = new Color32(0xA8, 0x93, 0x6E, 0xFF) },
        new Ending {
            id = "END03", title = "Predator-Free Highlands",
            description = "Blue Sheep still graze in the highlands, but no Snow Leopards remain. The herd no longer faces Snow Leopard hunting, yet it still depends on enough vegetation and water to survive.",
            meaning = "At least one Blue Sheep survives while the Snow Leopard population is zero. Predation pressure from Snow Leopards has disappeared, but grazing pressure and other survival needs continue. This outcome can occur even if Milo survives his story.",
            takeaway = "Removing predators changes the food web; prey survival still depends on available resources.",
            card = "The herd lives on, but the hunters are gone.",
            sky = new Color32(0xF6, 0xC9, 0x7A, 0xFF), hills = new Color32(0x7C, 0x9A, 0x6A, 0xFF), ground = new Color32(0xB9, 0xC4, 0x6A, 0xFF) },
        new Ending {
            id = "END04", title = "Disappearing Herd",
            description = "No Blue Sheep remain in the highlands, although Snow Leopards are still alive. Snowy's family may have survived this far, but without Blue Sheep to hunt, future feeding opportunities are uncertain.",
            meaning = "The Blue Sheep population is zero while at least one Snow Leopard survives. Hunting success and prey availability can change throughout the story; once the herd is gone, the Snow Leopards lose their represented prey source. This outcome can occur while Snowy is still alive.",
            takeaway = "Predators need a continuing prey population; catching more food now can reduce food opportunities later.",
            card = "The hunters remain, but the herd is gone.",
            sky = new Color32(0xB8, 0xC6, 0xD8, 0xFF), hills = new Color32(0x7E, 0x8C, 0x9C, 0xFF), ground = new Color32(0xC9, 0xB8, 0x8E, 0xFF) },
        new Ending {
            id = "END01", title = "Thriving Highlands",
            description = "Blue Sheep and Snow Leopards are still living in the highlands, and enough vegetation remains to support grazing. Milo's herd and Snowy's family are part of a food web that has continued through the changing seasons.",
            meaning = "At the evaluation point, at least five Blue Sheep and one Snow Leopard remain, with vegetation at 50% or higher. Grazing resources and predator–prey relationships remain present, although individual animals may have died and future survival is not guaranteed.",
            takeaway = "Plants support herbivores, and herbivores support predators. Protecting one part of the food web can affect the others.",
            card = "Plants, prey and predators all made it!",
            sky = new Color32(0x7F, 0xC8, 0xF0, 0xFF), hills = new Color32(0x4E, 0x9A, 0x5C, 0xFF), ground = new Color32(0x6C, 0xC0, 0x4A, 0xFF) },
        new Ending {
            id = "END02", title = "Fragile Balance",
            description = "Both Blue Sheep and Snow Leopards remain, but the ecosystem is under pressure. Vegetation may be low, or too few Blue Sheep remain to support a more stable food web. The next season could bring further challenges.",
            meaning = "Both species survive at the evaluation point, but either the Blue Sheep population is below five or vegetation is below 50%, or both. Choices affecting grazing, energy use and hunting may have contributed to these conditions, alongside autonomous animal behaviour.",
            takeaway = "The survival of both species does not mean their food supply or long-term balance is secure.",
            card = "Both animals survive, but it's a close call.",
            sky = new Color32(0xF2, 0xB6, 0x8A, 0xFF), hills = new Color32(0x8A, 0x9A, 0x5E, 0xFF), ground = new Color32(0xC8, 0xB4, 0x5A, 0xFF) },
    };

    public static int Count => All.Length;

    /// <summary>Gallery order: best to worst, as the children would read it (END01, END02, END03, END04, END05).</summary>
    public static readonly int[] GalleryOrder = { 3, 4, 1, 2, 0 };

    /// <summary>
    /// Ending Priority &amp; Classification (doc): Collapse, Predator-Free, Disappearing Herd, Thriving, Fragile.
    /// Counts are surviving animals (an individual-animal simulation), so zero is exactly zero.
    /// </summary>
    public static int Classify(int sheep, int leopards, float vegetation)
    {
        if (sheep <= 0 && leopards <= 0) return 0;
        if (leopards <= 0) return 1;
        if (sheep <= 0) return 2;
        if (sheep >= 5 && vegetation >= 50f) return 3;
        return 4;
    }

    // ---------- Gallery unlocks (kept across runs; never cleared by Replay or Restart) ----------

    public static int Mask => PlayerPrefs.GetInt(GuidedChallenge.GalleryKey, 0);
    public static bool IsUnlocked(int i) => (Mask & (1 << i)) != 0;

    /// <summary>Unlocks ending <paramref name="i"/>. True when it is new (no duplicate entries are created).</summary>
    public static bool Unlock(int i)
    {
        int mask = Mask;
        if ((mask & (1 << i)) != 0) return false;
        PlayerPrefs.SetInt(GuidedChallenge.GalleryKey, mask | (1 << i));
        PlayerPrefs.Save();
        return true;
    }

    public static int UnlockedCount
    {
        get { int n = 0; for (int i = 0; i < All.Length; i++) if (IsUnlocked(i)) n++; return n; }
    }

    /// <summary>"3 / 5 Endings Discovered" (doc, Collection Progress).</summary>
    public static string Progress() => $"{UnlockedCount} / {All.Length} Endings Discovered";
}
