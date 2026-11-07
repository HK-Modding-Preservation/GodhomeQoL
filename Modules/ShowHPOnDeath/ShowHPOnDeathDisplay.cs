using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

internal sealed class ShowHPOnDeathDisplay
{
    internal const string TitleLine = "T";
    internal const string BossLine = "R";
    internal const string NoteLine = "N";
    internal const string HintLine = "H";

    internal static ShowHPOnDeathDisplay? Instance;

    private const float PanelWidth = 480f;
    private const float Padding = 24f;
    private const float RowHeight = 44f;
    private const float ValueWidth = 130f;
    private const float PbWidth = 90f;

    private static readonly Color PlateColor = new(0.02f, 0.015f, 0.01f, 0.84f);
    private static readonly Color GoldColor = new(0.902f, 0.749f, 0.384f, 1f);
    private static readonly Color NameColor = new(0.93f, 0.9f, 0.84f, 1f);
    private static readonly Color HintColor = new(0.66f, 0.64f, 0.6f, 1f);
    private static readonly Color TrackColor = new(1f, 1f, 1f, 0.12f);
    private static readonly Color FillColor = new(0.902f, 0.749f, 0.384f, 0.85f);
    private static readonly Color LineColor = new(0.902f, 0.749f, 0.384f, 0.55f);

    private GameObject? canvas;
    private CanvasGroup? group;
    private Fader? fader;
    private GameObject? panel;

    public ShowHPOnDeathDisplay()
    {
        Instance = this;
        Create();
    }

    private void Create()
    {
        if (canvas != null)
        {
            return;
        }

        canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920, 1080), "ShowHPOnDeathCanvas");
        group = canvas.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        group.alpha = 0f;
        fader = canvas.AddComponent<Fader>();
        fader.Group = group;
        UObject.DontDestroyOnLoad(canvas);
    }

    public void Destroy()
    {
        if (canvas != null)
        {
            UObject.Destroy(canvas);
        }

        canvas = null;
        group = null;
        fader = null;
        panel = null;
        Instance = null;
    }

    public void Display(string value)
    {
        if (canvas == null || fader == null)
        {
            return;
        }

        string text = value?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            fader.Target = 0f;
            fader.OnHidden = ClearPanel;
            return;
        }

        fader.OnHidden = null;
        ClearPanel();
        BuildPanel(text);
        canvas.SetActive(true);
        fader.Target = 1f;
    }

    private void ClearPanel()
    {
        if (panel != null)
        {
            UObject.Destroy(panel);
        }

        panel = null;
    }

    private void BuildPanel(string text)
    {
        panel = new GameObject("ShowHPOnDeathPanel");
        panel.transform.SetParent(canvas!.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-32f, 32f);

        Image plate = panel.AddComponent<Image>();
        plate.sprite = QuickMenu.HkUi.SoftPlate;
        plate.type = Image.Type.Sliced;
        plate.color = PlateColor;
        plate.raycastTarget = false;

        float y = Padding - 4f;
        foreach (string line in text.Split('\n'))
        {
            string[] fields = line.Split('\t');
            switch (fields[0])
            {
                case TitleLine when fields.Length > 1:
                    y = AddTitle(fields[1], y);
                    break;
                case BossLine when fields.Length > 4:
                    y = AddBossRow(fields[1], ParseInt(fields[2]), ParseInt(fields[3]), ParseInt(fields[4]), y);
                    break;
                case NoteLine when fields.Length > 1:
                    y = AddCentered(fields[1], 18, GoldColor, y, 24f);
                    break;
                case HintLine when fields.Length > 1:
                    y = AddCentered(fields[1], 16, HintColor, y + 4f, 22f);
                    break;
            }
        }

        panelRect.sizeDelta = new Vector2(PanelWidth, y + Padding - 6f);
    }

    private float AddTitle(string title, float y)
    {
        Text text = CreateText("Title", title, QuickMenu.HkUi.TitleFont, 30, TextAnchor.MiddleCenter, Color.white);
        text.resizeTextForBestFit = true;
        text.resizeTextMaxSize = 30;
        text.resizeTextMinSize = 18;
        PlaceTop(text.rectTransform, Padding, y, PanelWidth - (Padding * 2f), 40f);

        Image line = CreateImage("Line", LineColor);
        PlaceTop(line.rectTransform, PanelWidth * 0.2f, y + 44f, PanelWidth * 0.6f, 2f);
        return y + 58f;
    }

    private float AddBossRow(string label, int hp, int max, int pb, float y)
    {
        float innerWidth = PanelWidth - (Padding * 2f);
        Text name = CreateText("Name", label, QuickMenu.HkUi.BodyFont, 22, TextAnchor.MiddleLeft, NameColor);
        name.resizeTextForBestFit = true;
        name.resizeTextMaxSize = 22;
        name.resizeTextMinSize = 14;
        PlaceTop(name.rectTransform, Padding, y, innerWidth - ValueWidth - PbWidth, 28f);

        if (pb >= 0)
        {
            Text best = CreateText("Best", $"PB {pb}", QuickMenu.HkUi.BodyFont, 17, TextAnchor.MiddleRight, GoldColor);
            PlaceTop(best.rectTransform, PanelWidth - Padding - ValueWidth - PbWidth, y, PbWidth, 28f);
        }

        string valueText = max > 0 ? $"{hp} / {max}" : hp.ToString();
        Text value = CreateText("Value", valueText, QuickMenu.HkUi.BodyFont, 22, TextAnchor.MiddleRight, Color.white);
        PlaceTop(value.rectTransform, PanelWidth - Padding - ValueWidth, y, ValueWidth, 28f);

        if (max > 0)
        {
            Image track = CreateImage("Track", TrackColor);
            PlaceTop(track.rectTransform, Padding, y + 31f, innerWidth, 5f);

            GameObject fillObject = new("Fill");
            fillObject.transform.SetParent(track.transform, false);
            RectTransform fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01((float)hp / max), 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fill = fillObject.AddComponent<Image>();
            fill.color = FillColor;
            fill.raycastTarget = false;
        }

        return y + RowHeight;
    }

    private float AddCentered(string content, int size, Color color, float y, float height)
    {
        Text text = CreateText("Note", content, QuickMenu.HkUi.BodyFont, size, TextAnchor.MiddleCenter, color);
        PlaceTop(text.rectTransform, Padding, y, PanelWidth - (Padding * 2f), height);
        return y + height;
    }

    private Text CreateText(string name, string content, Font font, int size, TextAnchor alignment, Color color)
    {
        GameObject textObject = new(name);
        textObject.transform.SetParent(panel!.transform, false);
        textObject.AddComponent<RectTransform>();
        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.text = content;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private Image CreateImage(string name, Color color)
    {
        GameObject imageObject = new(name);
        imageObject.transform.SetParent(panel!.transform, false);
        imageObject.AddComponent<RectTransform>();
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void PlaceTop(RectTransform rect, float left, float top, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(left, -top);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static int ParseInt(string value) => int.TryParse(value, out int result) ? result : 0;

    private sealed class Fader : MonoBehaviour
    {
        internal CanvasGroup? Group;
        internal float Target;
        internal Action? OnHidden;

        private void Update()
        {
            if (Group == null)
            {
                return;
            }

            float speed = Target > Group.alpha ? 7f : 3f;
            Group.alpha = Mathf.MoveTowards(Group.alpha, Target, Time.unscaledDeltaTime * speed);
            if (Target <= 0f && Group.alpha <= 0f && OnHidden != null)
            {
                Action hidden = OnHidden;
                OnHidden = null;
                hidden();
            }
        }
    }
}
