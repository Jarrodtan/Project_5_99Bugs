// [AI-GENERATED] Claude Code (Claude Opus 5.5, claude-opus-5-5) - SCRUM-380

using UnityEngine;
using UnityEngine.UI;

// Family picker (research doc, Guided Exploration core loop: "Choose Animal: Select a Blue Sheep or Snow Leopard
// family to follow"). Opens over the Mode Selection screen when Guided Exploration is tapped. Picking a family
// sets FamilyManager.SelectedStory (which also sets the starting populations) and starts the run.
// Part of ScreenFlow so it shares its card style and helpers.
public static partial class ScreenFlow
{
    static GameObject familyPicker;

    static void ShowFamilyPicker()
    {
        if (familyPicker != null) return;
        Mascot.HoldForLoading();   // the Mode Selection mascot stops talking and hides while the picker is open
        // Recorded VO (UXGD, VoiceOver_M3/Scripts/Animal Selection.wav), voice only.
        Mascot.Speak("Every animal has a story to tell. Would you like to follow Milo the Blue Sheep, or Snowy the Snow Leopard, on their adventure?");

        var canvasGo = new GameObject("[Family Picker]", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        familyPicker = canvasGo;
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;   // above the menu and its mascot, under the volume box (100)
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1200);
        scaler.matchWidthOrHeight = 0.5f;

        var dim = Rect(canvasGo.transform, "Dim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.55f));
        var card = Rect(dim.transform, "Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 900f), Cream);

        Text(card.transform, "Whose story will you follow?", 64f, Ink, new Vector2(0f, 370f), new Vector2(1400f, 90f));
        Text(card.transform, "Help them survive a year in the Himalayan Highlands.", 34f, Ink, new Vector2(0f, 300f), new Vector2(1400f, 60f));
        StyleLikeShumin(card, "");   // cream card, brown edge, Chewy font (before the choices, so they keep their own look)

        // Back: plain outlined button, so the two green Follow buttons are the obvious choices.
        var back = Btn(card.transform, "Back", new Vector2(0f, -385f), Cream, CloseFamilyPicker);
        ((RectTransform)back.transform).sizeDelta = new Vector2(260f, 80f);
        var backEdge = back.AddComponent<Outline>();
        backEdge.effectColor = Brown;
        backEdge.effectDistance = new Vector2(3f, -3f);

        FamilyChoice(card.transform, new Vector2(-355f, -20f), FamilyStory.BlueSheep, false, "Miloy", "Blue Sheep",
            "A curious young lamb who lives with his mum and sibling in the herd. He eats grass and must watch out for Snow Leopards.");
        FamilyChoice(card.transform, new Vector2(355f, -20f), FamilyStory.SnowLeopard, true, "Snowy", "Snow Leopard",
            "A young cub who lives with her mother and sibling. Her mum hunts Blue Sheep to feed the family.");

        var font = ShuminFont("Chewy");
        foreach (var t in card.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
        {
            if (font != null) t.font = font;
            if (t.transform.parent == back.transform) t.color = Ink;
        }
    }

    static void CloseFamilyPicker()
    {
        if (familyPicker != null) Object.Destroy(familyPicker);
        familyPicker = null;
        Mascot.ShowPendingMenuLine();   // Back: the mascot and its line come back
    }

    // One big tappable card per family: picture, name, species, a short story line, the starting animals,
    // and a green "Follow ..." button. Tapping anywhere on the card picks it.
    static void FamilyChoice(Transform parent, Vector2 pos, FamilyStory story, bool leopard, string name, string species, string blurb)
    {
        var choice = Rect(parent, $"Choose {name}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(640f, 560f), Color.white);
        var edge = choice.AddComponent<Outline>();
        edge.effectColor = Brown;
        edge.effectDistance = new Vector2(4f, -4f);
        UnityEngine.Events.UnityAction pick = () => PickFamily(story);
        choice.AddComponent<Button>().onClick.AddListener(pick);

        var icon = new GameObject("Picture", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(choice.transform, false);
        var irt = (RectTransform)icon.transform;
        irt.anchoredPosition = new Vector2(0f, 165f);
        irt.sizeDelta = new Vector2(190f, 190f);
        SimHud.SetIcon(icon.transform, SimHud.AnimalIcon(leopard));

        var (sheep, leopards) = FamilyManager.StartingPopulation(story);
        Text(choice.transform, name, 60f, Ink, new Vector2(0f, 30f), new Vector2(600f, 80f));
        Text(choice.transform, $"the {species}", 32f, Brown, new Vector2(0f, -25f), new Vector2(600f, 50f));
        Text(choice.transform, blurb, 27f, Ink, new Vector2(0f, -105f), new Vector2(560f, 110f));
        Text(choice.transform, $"Starts with {sheep} Blue Sheep and {leopards} Snow Leopard{(leopards == 1 ? "" : "s")}",
             24f, Brown, new Vector2(0f, -178f), new Vector2(600f, 36f));

        var follow = Btn(choice.transform, $"Follow {name}", new Vector2(0f, -235f), new Color(0.3f, 0.62f, 0.38f), pick);
        ((RectTransform)follow.transform).sizeDelta = new Vector2(360f, 84f);
        var green = ShuminSprite(GreenButtons);
        if (green != null && follow.TryGetComponent(out Image fi)) { fi.sprite = green; fi.type = Image.Type.Sliced; fi.color = Color.white; }
        // (StyleLikeShumin leaves text inside a button alone, so the card keeps these colours.)
    }

    static void PickFamily(FamilyStory story)
    {
        FamilyManager.SelectedStory = story;
        CloseFamilyPicker();
        StartSimulation(true);
    }
}
