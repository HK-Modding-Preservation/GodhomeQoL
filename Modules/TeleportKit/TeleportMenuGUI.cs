using GodhomeQoL.Modules.Tools;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.QoL;

internal sealed class TeleportMenuGUI : IDisposable
{
    private const int CanvasSortOrder = 10040;
    private const int RowsPerColumn = 9;
    private const int SlotCount = 18;
    private const float PanelWidth = 720f;
    private const float PanelHeight = 578f;
    private const float PanelLeft = 40f;
    private const float ColumnWidth = 320f;
    private const float ColumnLeft = 40f;
    private const float RowTop = 170f;
    private const float RowStep = 31f;
    private const float RowHeight = 28f;
    private const float FadeInSpeed = 8f;
    private const float FadeOutSpeed = 6f;

    private static readonly Color IdColor = new(0.902f, 0.749f, 0.384f, 1f);
    private static readonly Color NameColor = new(0.953f, 0.894f, 0.753f, 1f);
    private static readonly Color HintColor = new(0.75f, 0.72f, 0.66f, 1f);
    private static readonly Color AccentColor = new(1f, 0.86f, 0.5f, 1f);
    private static readonly Color PanelColor = new(0.02f, 0.015f, 0.01f, 0.86f);
    private static readonly Color SelectFillColor = new(0.24f, 0.16f, 0.06f, 0.92f);
    private static readonly Color SelectLineColor = new(0.902f, 0.749f, 0.384f, 0.9f);

    private readonly TeleportKit mod;
    private GameObject? root;
    private CanvasGroup? group;
    private Text? title;
    private Text? pageLine;
    private Text? fightLine;
    private Text? controlLine;
    private Text? hotkeyLine;
    private Text? extraLine;
    private readonly Slot[] slots = new Slot[SlotCount];
    private float alpha;

    internal TeleportMenuGUI(TeleportKit mod)
    {
        this.mod = mod;
        Build();
    }

    public void Dispose()
    {
        if (root != null)
        {
            UObject.Destroy(root);
            root = null;
        }

        alpha = 0f;
    }

    internal static (int start, int end) GetPageRange(int page)
    {
        return page switch
        {
            1 => (0, 18),
            2 => (18, 36),
            3 => (36, 44),
            4 => (44, 46),
            _ => (0, 0)
        };
    }

    private static string GetPageTitle(int page) => page switch
    {
        1 => "Hall of Gods - Floor 1",
        2 => "Hall of Gods - Floor 2",
        3 => "Pantheons",
        4 => "Path of Pain",
        _ => string.Empty
    };

    private void Build()
    {
        root = new GameObject("TeleportKitMenuCanvas");
        UObject.DontDestroyOnLoad(root);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortOrder;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        group = root.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        group.alpha = 0f;
        root.AddComponent<Driver>().Init(this);

        GameObject panel = new("Panel");
        panel.transform.SetParent(root.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(0f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = new Vector2(PanelLeft, 0f);
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.sprite = QuickMenu.HkUi.SoftPlate;
        panelImage.type = Image.Type.Sliced;
        panelImage.color = PanelColor;
        panelImage.raycastTarget = false;

        title = CreateText(panel.transform, "Title", 40, TextAnchor.MiddleCenter, Color.white);
        SetTopRect(title.rectTransform, PanelWidth * 0.5f, 52f, PanelWidth - 60f, 56f);
        QuickMenu.HkUi.StyleTitle(title);

        pageLine = CreateText(panel.transform, "Page", 20, TextAnchor.MiddleCenter, HintColor);
        SetTopRect(pageLine.rectTransform, PanelWidth * 0.5f, 136f, PanelWidth - 60f, 24f);

        for (int i = 0; i < SlotCount; i++)
        {
            int column = i / RowsPerColumn;
            int row = i % RowsPerColumn;
            slots[i] = CreateSlot(panel.transform, i, ColumnLeft + (column * (ColumnWidth + 20f)) + (ColumnWidth * 0.5f), RowTop + (row * RowStep));
        }

        float linesTop = RowTop + (RowsPerColumn * RowStep) + 18f;
        fightLine = CreateText(panel.transform, "Fight", 21, TextAnchor.MiddleCenter, AccentColor);
        SetTopRect(fightLine.rectTransform, PanelWidth * 0.5f, linesTop, PanelWidth - 40f, 26f);
        controlLine = CreateText(panel.transform, "Controls", 20, TextAnchor.MiddleCenter, NameColor);
        SetTopRect(controlLine.rectTransform, PanelWidth * 0.5f, linesTop + 28f, PanelWidth - 40f, 26f);
        hotkeyLine = CreateText(panel.transform, "Hotkeys", 18, TextAnchor.MiddleCenter, HintColor);
        SetTopRect(hotkeyLine.rectTransform, PanelWidth * 0.5f, linesTop + 56f, PanelWidth - 40f, 24f);
        extraLine = CreateText(panel.transform, "Extra", 18, TextAnchor.MiddleCenter, HintColor);
        SetTopRect(extraLine.rectTransform, PanelWidth * 0.5f, linesTop + 80f, PanelWidth - 40f, 24f);

        root.SetActive(true);
    }

    private Slot CreateSlot(Transform parent, int index, float centerX, float top)
    {
        GameObject slotObject = new($"Entry{index}");
        slotObject.transform.SetParent(parent, false);
        RectTransform rect = slotObject.AddComponent<RectTransform>();
        SetTopRect(rect, centerX, top, ColumnWidth, RowHeight);

        Image fill = slotObject.AddComponent<Image>();
        fill.sprite = QuickMenu.HkUi.ChamferFill;
        fill.type = Image.Type.Sliced;
        fill.color = SelectFillColor;
        fill.raycastTarget = false;

        GameObject lineObject = new("Line");
        lineObject.transform.SetParent(slotObject.transform, false);
        RectTransform lineRect = lineObject.AddComponent<RectTransform>();
        lineRect.anchorMin = Vector2.zero;
        lineRect.anchorMax = Vector2.one;
        lineRect.offsetMin = Vector2.zero;
        lineRect.offsetMax = Vector2.zero;
        Image line = lineObject.AddComponent<Image>();
        line.sprite = QuickMenu.HkUi.ChamferLine;
        line.type = Image.Type.Sliced;
        line.color = SelectLineColor;
        line.raycastTarget = false;

        Text label = CreateText(slotObject.transform, "Label", 22, TextAnchor.MiddleLeft, NameColor);
        label.supportRichText = true;
        label.resizeTextForBestFit = true;
        label.resizeTextMaxSize = 22;
        label.resizeTextMinSize = 14;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 0f);
        labelRect.offsetMax = new Vector2(-8f, 0f);

        fill.enabled = false;
        line.enabled = false;
        return new Slot(slotObject, fill, line, label);
    }

    private static Text CreateText(Transform parent, string name, int size, TextAnchor alignment, Color color)
    {
        GameObject textObject = new(name);
        textObject.transform.SetParent(parent, false);
        textObject.AddComponent<RectTransform>();
        Text text = textObject.AddComponent<Text>();
        text.font = QuickMenu.HkUi.BodyFont;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void SetTopRect(RectTransform rect, float centerX, float top, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(centerX, -top);
        rect.sizeDelta = new Vector2(width, height);
    }

    private void Tick()
    {
        if (root == null || group == null)
        {
            return;
        }

        bool visible = mod.Input.ShowMenu && GameManager.instance != null && !GameManager.instance.IsGamePaused();
        float target = visible ? 1f : 0f;
        alpha = Mathf.MoveTowards(alpha, target, Time.unscaledDeltaTime * (visible ? FadeInSpeed : FadeOutSpeed));
        group.alpha = alpha;
        if (alpha <= 0f)
        {
            return;
        }

        Refresh();
    }

    private void Refresh()
    {
        int page = mod.Input.CurrentPage;
        if (title != null)
        {
            title.text = GetPageTitle(page);
        }

        if (pageLine != null)
        {
            pageLine.text = mod.Input.GamepadMode
                ? $"LB  -  Page {page} of 4  -  RB"
                : $"Q  -  Page {page} of 4  -  E";
        }

        (int start, int end) = GetPageRange(page);
        int highlighted = GetHighlightedIndex(start, end);
        bool? fightVersion = GetFightVersion();
        for (int i = 0; i < SlotCount; i++)
        {
            int index = start + i;
            Slot slot = slots[i];
            bool used = index < end && index < mod.Data.Bosses.Length && !string.IsNullOrEmpty(mod.Data.Bosses[index].scene);
            slot.Root.SetActive(used);
            if (!used)
            {
                continue;
            }

            var boss = mod.Data.Bosses[index];
            bool selected = index == highlighted;
            slot.Fill.enabled = selected;
            slot.Line.enabled = selected;
            slot.Label.color = selected ? Color.white : NameColor;
            string name = selected && fightVersion != null ? FormatVersionName(boss.name, fightVersion.Value) : boss.name;
            slot.Label.text = $"<color=#{ColorUtility.ToHtmlStringRGB(IdColor)}>{boss.id}</color>   {name}";
        }

        if (fightLine != null)
        {
            fightLine.text = mod.Input.GamepadMode
                ? $"Fight: {mod.Input.GamepadFightLevelName}   -   X difficulty   -   RT version   -   Y start fight"
                : "Fight: 1!N Attuned   -   2!N Ascended   -   3!N Radiant   -   3!!N second version";
        }

        if (controlLine != null)
        {
            controlLine.text = GetControlText();
        }

        if (hotkeyLine != null)
        {
            hotkeyLine.text = $"Menu {mod.MenuKey}   -   Save point {mod.SaveTeleportKey}   -   Teleport {mod.TeleportKey}   (Ctrl + key to rebind)";
        }

        if (extraLine != null)
        {
            extraLine.text = mod.Data.CustomTeleportPosition != null
                ? $"Saved point: {mod.Data.CustomTeleportScene}"
                : string.Empty;
        }
    }

    private string GetControlText()
    {
        if (mod.Input.IsRebindingMenuKey)
        {
            return "Press any key for the menu hotkey (Esc to cancel)";
        }

        if (mod.Input.IsRebindingSaveKey)
        {
            return "Press any key for the save point key (Esc to cancel)";
        }

        if (mod.Input.IsRebindingTeleportKey)
        {
            return "Press any key for the teleport key (Esc to cancel)";
        }

        if (mod.Input.GamepadMode)
        {
            return "D-Pad select   -   A teleport   -   B close";
        }

        string buffer = mod.Input.InputBuffer;
        string typed = buffer.Length > 0
            ? $"Number: <color=#{ColorUtility.ToHtmlStringRGB(AccentColor)}>{buffer}</color>"
            : "Type a number";
        return $"{typed}   -   ! = Shift+1   -   Enter teleport   -   Esc close";
    }

    private bool? GetFightVersion()
    {
        if (mod.Input.GamepadMode)
        {
            return mod.Input.GamepadFightAlt;
        }

        string buffer = mod.Input.InputBuffer;
        return buffer.IndexOf(TeleportInputHandler.FightSeparator) >= 0
            ? TeleportInputHandler.IsAltFightInput(buffer)
            : null;
    }

    private static string FormatVersionName(string name, bool altVersion)
    {
        int slash = name.IndexOf('/');
        if (slash <= 0 || slash >= name.Length - 1)
        {
            return name;
        }

        string dim = ColorUtility.ToHtmlStringRGB(HintColor);
        string first = name.Substring(0, slash);
        string second = name.Substring(slash + 1);
        return altVersion
            ? $"<color=#{dim}>{first}/</color>{second}"
            : $"{first}<color=#{dim}>/{second}</color>";
    }

    private int GetHighlightedIndex(int start, int end)
    {
        if (mod.Input.GamepadMode)
        {
            return mod.Input.GamepadIndex;
        }

        string buffer = mod.Input.InputBuffer;
        int separator = buffer.LastIndexOf(TeleportInputHandler.FightSeparator);
        string number = separator >= 0 ? buffer.Substring(separator + 1) : buffer;
        if (!int.TryParse(number, out int id))
        {
            return -1;
        }

        for (int i = start; i < end && i < mod.Data.Bosses.Length; i++)
        {
            if (mod.Data.Bosses[i].id == id)
            {
                return i;
            }
        }

        return -1;
    }

    private void HandleGuiEvent()
    {
        Event? current = Event.current;
        if (current != null
            && current.type == EventType.KeyDown
            && mod.Input.ShowMenu
            && (current.character == TeleportInputHandler.FightSeparator || (current.shift && current.keyCode == KeyCode.Alpha1)))
        {
            mod.Input.QueueFightSeparator(current.character == TeleportInputHandler.FightSeparator ? "gui char" : "gui key");
        }
    }

    private sealed class Slot
    {
        internal Slot(GameObject root, Image fill, Image line, Text label)
        {
            Root = root;
            Fill = fill;
            Line = line;
            Label = label;
        }

        internal GameObject Root { get; }

        internal Image Fill { get; }

        internal Image Line { get; }

        internal Text Label { get; }
    }

    private sealed class Driver : MonoBehaviour
    {
        private TeleportMenuGUI? menu;

        internal void Init(TeleportMenuGUI owner)
        {
            menu = owner;
        }

        private void LateUpdate()
        {
            menu?.Tick();
        }

        private void OnGUI()
        {
            menu?.HandleGuiEvent();
        }
    }
}
