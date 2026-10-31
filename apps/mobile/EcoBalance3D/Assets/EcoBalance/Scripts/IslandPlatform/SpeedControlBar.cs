using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Two bottom buttons: a play/pause icon, and a fast-forward icon that gains an arrow at each step from 1× to 8×.
/// </summary>
public class SpeedControlBar : MonoBehaviour
{
    [SerializeField]
    SimulationSpeedController controller;

    static readonly float[] Speeds = { 1f, 2f, 4f, 8f };
    static readonly Color Rest = new Color(0.22f, 0.24f, 0.28f, 1f);
    const int IconSize = 64;

    Image playIcon;
    Image speedIcon;
    Sprite playSprite;
    Sprite pauseSprite;
    Sprite[] speedSprites;
    int speedIndex;

    void Start()
    {
        if (controller == null)
            controller = GetComponent<SimulationSpeedController>();

        if (controller == null)
        {
            Debug.LogError($"{nameof(SpeedControlBar)} requires a {nameof(SimulationSpeedController)}.", this);
            return;
        }

        playSprite = TriangleSprite();
        pauseSprite = PauseSprite();
        speedSprites = new Sprite[Speeds.Length];
        for (var i = 0; i < Speeds.Length; i++)
            speedSprites[i] = FastForwardSprite(i + 1);

        speedIndex = IndexOf(controller.CurrentSpeed);
        Build();
        RefreshIcons();
    }

    void OnDestroy()
    {
        DestroySprite(playSprite);
        DestroySprite(pauseSprite);
        if (speedSprites == null)
            return;

        for (var i = 0; i < speedSprites.Length; i++)
            DestroySprite(speedSprites[i]);
    }

    void Build()
    {
        var canvasGo = new GameObject("SpeedControlCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var bar = CreateRect("Speed Bar", canvasGo.transform);
        bar.anchorMin = new Vector2(0.5f, 0f);
        bar.anchorMax = new Vector2(0.5f, 0f);
        bar.pivot = new Vector2(0.5f, 0f);
        bar.anchoredPosition = new Vector2(0f, 20f);
        bar.sizeDelta = new Vector2(280f, 88f);

        var background = bar.gameObject.AddComponent<Image>();
        background.color = new Color(0.1f, 0.12f, 0.14f, 0.92f);

        var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        playIcon = AddButton(bar, "Play", TogglePlay);
        speedIcon = AddButton(bar, "Speed", CycleSpeed);
    }

    void TogglePlay()
    {
        if (controller.IsRunning)
            controller.Pause();
        else
            controller.Play();

        RefreshIcons();
    }

    void CycleSpeed()
    {
        speedIndex = (speedIndex + 1) % Speeds.Length;
        ApplySpeed(Speeds[speedIndex]);
        RefreshIcons();
    }

    void ApplySpeed(float speed)
    {
        if (speed >= 8f)
            controller.SetSpeed8x();
        else if (speed >= 4f)
            controller.SetSpeed4x();
        else if (speed >= 2f)
            controller.SetSpeed2x();
        else
            controller.SetSpeed1x();
    }

    void RefreshIcons()
    {
        if (playIcon != null)
            playIcon.sprite = controller.IsRunning ? pauseSprite : playSprite;

        if (speedIcon != null)
            speedIcon.sprite = speedSprites[speedIndex];
    }

    static int IndexOf(float speed)
    {
        for (var i = 0; i < Speeds.Length; i++)
        {
            if (Mathf.Approximately(Speeds[i], speed))
                return i;
        }

        return 0;
    }

    static Image AddButton(RectTransform parent, string name, UnityEngine.Events.UnityAction onClick)
    {
        var rect = CreateRect(name, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = Rest;

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        var iconRect = CreateRect("Icon", rect);
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(6f, 4f);
        iconRect.offsetMax = new Vector2(-6f, -4f);
        var icon = iconRect.gameObject.AddComponent<Image>();
        icon.color = Color.white;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        return icon;
    }

    static Sprite TriangleSprite()
    {
        var pixels = NewPixels();
        FillTriangle(pixels, new Vector2(8f, 6f), new Vector2(8f, 58f), new Vector2(56f, 32f));
        return ToSprite(pixels);
    }

    static Sprite PauseSprite()
    {
        var pixels = NewPixels();
        FillRect(pixels, 10, 6, 26, 58);
        FillRect(pixels, 38, 6, 54, 58);
        return ToSprite(pixels);
    }

    static Sprite FastForwardSprite(int arrows)
    {
        var pixels = NewPixels();
        const float width = 14f;
        const float gap = 2f;
        var total = arrows * width + (arrows - 1) * gap;
        var start = (IconSize - total) * 0.5f;
        for (var i = 0; i < arrows; i++)
        {
            var x = start + i * (width + gap);
            FillTriangle(pixels, new Vector2(x, 8f), new Vector2(x, 56f), new Vector2(x + width, 32f));
        }

        return ToSprite(pixels);
    }

    static Color32[] NewPixels()
    {
        var pixels = new Color32[IconSize * IconSize];
        var clear = new Color32(0, 0, 0, 0);
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = clear;
        return pixels;
    }

    static Sprite ToSprite(Color32[] pixels)
    {
        var texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, IconSize, IconSize), new Vector2(0.5f, 0.5f), 100f);
    }

    static void FillRect(Color32[] pixels, int x0, int y0, int x1, int y1)
    {
        var ink = new Color32(255, 255, 255, 255);
        for (var y = y0; y < y1; y++)
        {
            for (var x = x0; x < x1; x++)
                pixels[y * IconSize + x] = ink;
        }
    }

    static void FillTriangle(Color32[] pixels, Vector2 a, Vector2 b, Vector2 c)
    {
        var ink = new Color32(255, 255, 255, 255);
        var minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), 0, IconSize - 1);
        var maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))), 0, IconSize - 1);
        var minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), 0, IconSize - 1);
        var maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))), 0, IconSize - 1);

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                if (InsideTriangle(new Vector2(x + 0.5f, y + 0.5f), a, b, c))
                    pixels[y * IconSize + x] = ink;
            }
        }
    }

    static bool InsideTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        var d1 = Edge(p, a, b);
        var d2 = Edge(p, b, c);
        var d3 = Edge(p, c, a);
        var hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
        var hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(hasNeg && hasPos);
    }

    static float Edge(Vector2 p, Vector2 a, Vector2 b)
    {
        return (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
    }

    static void DestroySprite(Sprite sprite)
    {
        if (sprite == null)
            return;

        if (sprite.texture != null)
            Destroy(sprite.texture);
        Destroy(sprite);
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }
}
