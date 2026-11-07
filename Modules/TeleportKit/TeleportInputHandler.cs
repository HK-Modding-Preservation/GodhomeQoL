using InControl;
using UnityEngine.UI;

using GodhomeQoL.Modules.Tools;

namespace GodhomeQoL.Modules.QoL;

internal sealed class TeleportInputHandler : IDisposable
{
    private enum BindResult
    {
        None,
        Applied,
        Cancelled,
        Rejected
    }

    internal const char FightSeparator = '!';

    private readonly TeleportKit mod;
    private string? queuedSeparatorSource;
    private int separatorFrame = -1;
    private string inputBuffer = string.Empty;
    private int currentPage = 1;
    private bool gameWasPaused;
    private bool pauseEverOpened;
    private static bool pauseHintActive;
    private static bool benchHintActive;
    private static float benchHintCooldownUntil;

    internal TeleportInputHandler(TeleportKit mod)
    {
        this.mod = mod;
        ModHooks.HeroUpdateHook += CheckInput;
        On.HeroController.Pause += OnPause;
    }

    internal bool IsRebindingMenuKey { get; private set; }
    internal bool IsRebindingSaveKey { get; private set; }
    internal bool IsRebindingTeleportKey { get; private set; }
    internal bool ShowMenu { get; set; }
    internal string InputBuffer => inputBuffer;
    internal int CurrentPage => currentPage;

    public void Dispose()
    {
        ModHooks.HeroUpdateHook -= CheckInput;
        On.HeroController.Pause -= OnPause;
        ShowMenu = false;
        ReleaseGamepad();
        inputBuffer = string.Empty;
        queuedSeparatorSource = null;
        IsRebindingMenuKey = false;
        IsRebindingSaveKey = false;
        IsRebindingTeleportKey = false;
    }

    internal void ResetPauseFlag() => pauseEverOpened = false;

    private void CheckInput()
    {
        if (!ShowMenu && (GamepadMode || gamepadHeroTaken))
        {
            ReleaseGamepad();
        }

        bool isGamePaused = GameManager.instance.IsGamePaused();

        if (isGamePaused && !gameWasPaused)
        {
            ShowMenu = false;
            inputBuffer = string.Empty;
            mod.Log.Write("Menu closed due to game pause");
        }

        gameWasPaused = isGamePaused;

        if (isGamePaused)
        {
            pauseEverOpened = true;
            return;
        }

        PlayerData? pd = PlayerData.instance;
        if (pd != null && pd.atBench)
        {
            if (!Input.GetKey(KeyCode.LeftControl)
                && !Input.GetKey(KeyCode.RightControl)
                && Input.GetKeyDown(mod.MenuKey))
            {
                ShowBenchHint();
            }

            if (ShowMenu || IsRebindingMenuKey || IsRebindingSaveKey || IsRebindingTeleportKey)
            {
                ShowMenu = false;
                inputBuffer = string.Empty;
                currentPage = 1;
                IsRebindingMenuKey = false;
                IsRebindingSaveKey = false;
                IsRebindingTeleportKey = false;
                mod.Log.Write("TeleportKit input cancelled due to sitting on bench");
            }

            return;
        }

        bool ownsTeleportInput = ShowMenu || IsRebindingMenuKey || IsRebindingSaveKey || IsRebindingTeleportKey;
        if (!ownsTeleportInput && (QuickMenu.IsHotkeyInputBlocked() || QuickMenu.IsAnyUiVisible()))
        {
            return;
        }

        if (ownsTeleportInput && QuickMenu.IsAnyUiVisible())
        {
            return;
        }

        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (ctrlHeld && Input.GetKeyDown(mod.MenuKey))
        {
            IsRebindingMenuKey = true;
            mod.Log.Write("Started rebinding menu hotkey");
            return;
        }

        if (ctrlHeld && Input.GetKeyDown(mod.SaveTeleportKey))
        {
            IsRebindingSaveKey = true;
            mod.Log.Write("Started rebinding save teleport key");
            return;
        }

        if (ctrlHeld && Input.GetKeyDown(mod.TeleportKey))
        {
            IsRebindingTeleportKey = true;
            mod.Log.Write("Started rebinding teleport key");
            return;
        }

        if (HandleRebinds())
        {
            return;
        }

        if (Input.GetKeyDown(mod.MenuKey))
        {
            if (!pauseEverOpened)
            {
                mod.Log.Write("Menu hotkey ignored because pause menu was not opened yet");
                ShowPauseHint();
                return;
            }

            ShowMenu = !ShowMenu;
            GamepadMode = false;
            inputBuffer = string.Empty;
            currentPage = 1;
            mod.Log.Write($"Menu {(ShowMenu ? "opened" : "closed")}");
        }
        else if (IsTeleportGamepadButtonPressed())
        {
            if (!pauseEverOpened)
            {
                mod.Log.Write("Gamepad menu button ignored because pause menu was not opened yet");
                ShowPauseHint();
                return;
            }

            ShowMenu = !ShowMenu;
            inputBuffer = string.Empty;
            currentPage = 1;
            if (ShowMenu)
            {
                GamepadMode = true;
                GamepadIndex = TeleportMenuGUI.GetPageRange(currentPage).start;
                TakeGamepadHero();
            }

            mod.Log.Write($"Menu {(ShowMenu ? "opened" : "closed")} with gamepad");
            return;
        }

        if (ShowMenu && GamepadMode)
        {
            HandleGamepadMenu();
        }

        if (ShowMenu)
        {
            HandleMenuNavigation();
            HandleMenuInput();
        }

        if (!ShowMenu && !mod.Teleport.IsBusy)
        {
            if (Input.GetKeyDown(mod.SaveTeleportKey))
            {
                SetCustomTeleportPoint();
            }
            else if (Input.GetKeyDown(mod.TeleportKey))
            {
                TeleportToCustomPoint();
            }
        }
    }

    private bool HandleRebinds()
    {
        if (IsRebindingMenuKey)
        {
            BindResult result = TryBindKey(
                TeleportKit.HotkeySlot.Menu,
                () => IsRebindingMenuKey = false
            );

            if (result == BindResult.Applied)
            {
                mod.Log.Write($"Menu hotkey rebound to: {mod.MenuKey}");
            }

            return result != BindResult.None;
        }

        if (IsRebindingSaveKey)
        {
            BindResult result = TryBindKey(
                TeleportKit.HotkeySlot.SaveTeleport,
                () => IsRebindingSaveKey = false,
                key => key != KeyCode.LeftControl && key != KeyCode.RightControl
            );

            if (result == BindResult.Applied)
            {
                mod.Log.Write($"Save teleport key rebound to: {mod.SaveTeleportKey}");
            }

            return result != BindResult.None;
        }

        if (IsRebindingTeleportKey)
        {
            BindResult result = TryBindKey(
                TeleportKit.HotkeySlot.Teleport,
                () => IsRebindingTeleportKey = false,
                key => key != KeyCode.LeftControl && key != KeyCode.RightControl
            );

            if (result == BindResult.Applied)
            {
                mod.Log.Write($"Teleport key rebound to: {mod.TeleportKey}");
            }

            return result != BindResult.None;
        }

        return false;
    }

    private BindResult TryBindKey(TeleportKit.HotkeySlot slot, Action onDone, Func<KeyCode, bool>? predicate = null)
    {
        if (!Input.anyKeyDown)
        {
            return BindResult.None;
        }

        foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyDown(keyCode) || keyCode == KeyCode.None)
            {
                continue;
            }

            if (keyCode >= KeyCode.Mouse0 && keyCode <= KeyCode.Mouse6)
            {
                continue;
            }

            if (keyCode == KeyCode.Escape)
            {
                onDone.Invoke();
                mod.Log.Write("TeleportKit hotkey rebind cancelled");
                return BindResult.Cancelled;
            }

            if (predicate != null && !predicate.Invoke(keyCode))
            {
                continue;
            }

            if (!TeleportKit.TryAssignHotkey(slot, keyCode, out string failureReason))
            {
                mod.Log.Write($"TeleportKit hotkey rebind rejected: {keyCode} {failureReason}");
                return BindResult.Rejected;
            }

            onDone.Invoke();
            return BindResult.Applied;
        }

        return BindResult.None;
    }

    private void HandleMenuNavigation()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            currentPage--;
            if (currentPage < 1)
            {
                currentPage = 4;
            }
            inputBuffer = string.Empty;
            GamepadIndex = TeleportMenuGUI.GetPageRange(currentPage).start;
            mod.Log.Write($"Switched to page {currentPage} (left)");
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            currentPage++;
            if (currentPage > 4)
            {
                currentPage = 1;
            }
            inputBuffer = string.Empty;
            GamepadIndex = TeleportMenuGUI.GetPageRange(currentPage).start;
            mod.Log.Write($"Switched to page {currentPage} (right)");
        }
    }

    private void HandleMenuInput()
    {
        string typed = Input.inputString ?? string.Empty;
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (shiftHeld && Input.anyKeyDown)
        {
            LogShiftInput(typed);
        }

        string? separatorSource = null;
        if (typed.IndexOf(FightSeparator) >= 0)
        {
            separatorSource = "text";
        }
        else if (Input.GetKeyDown(KeyCode.Exclaim))
        {
            separatorSource = "exclaim key";
        }
        else if (shiftHeld && (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)))
        {
            separatorSource = "shift+1";
        }

        bool separatorThisFrame = separatorSource != null;
        if (separatorThisFrame)
        {
            separatorFrame = Time.frameCount;
        }

        separatorSource ??= queuedSeparatorSource;
        queuedSeparatorSource = null;

        if (separatorSource != null && CanAddSeparator())
        {
            inputBuffer += FightSeparator;
            mod.Log.Write($"Input buffer: {inputBuffer} ({separatorSource})");
        }

        if (!separatorThisFrame)
        {
            for (int i = 0; i <= 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                {
                    inputBuffer += i.ToString();
                    mod.Log.Write($"Input buffer: {inputBuffer}");
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Return) && inputBuffer.Length > 0)
        {
            if (inputBuffer.IndexOf(FightSeparator) >= 0)
            {
                StartStatueFightFromInput(inputBuffer);
            }
            else if (int.TryParse(inputBuffer, out int bossId))
            {
                var boss = mod.Data.Bosses.FirstOrDefault(b => b.id == bossId);
                if (!string.IsNullOrEmpty(boss.scene))
                {
                    mod.Log.Write($"Attempting teleport to boss ID {bossId} ({boss.name})");
                    mod.Teleport.StartTeleport(boss.position, boss.scene);
                }
                else
                {
                    mod.Log.Write($"Invalid boss ID: {bossId}");
                }
            }
            else if (inputBuffer == "12151920815165")
            {
                mod.Log.Write("Attempting teleport to Dream_Room_Believer_Shrine");
                mod.Teleport.StartTeleport(new Vector3(56.486f, 40.388f, 0f), "Dream_Room_Believer_Shrine");
            }

            inputBuffer = string.Empty;
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            inputBuffer = inputBuffer.Length > 0
                ? inputBuffer.Substring(0, inputBuffer.Length - 1)
                : string.Empty;
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            ShowMenu = false;
            inputBuffer = string.Empty;
            mod.Log.Write("Menu closed via Escape");
        }
    }

    internal void QueueFightSeparator(string source)
    {
        if (Time.frameCount == separatorFrame)
        {
            return;
        }

        queuedSeparatorSource ??= source;
    }

    private bool CanAddSeparator()
    {
        if (inputBuffer.Length == 0)
        {
            return false;
        }

        int count = inputBuffer.Count(c => c == FightSeparator);
        return count == 0 || (count == 1 && inputBuffer[inputBuffer.Length - 1] == FightSeparator);
    }

    internal static bool IsAltFightInput(string text)
    {
        int first = text.IndexOf(FightSeparator);
        return first >= 0 && first + 1 < text.Length && text[first + 1] == FightSeparator;
    }

    private void LogShiftInput(string typed)
    {
        List<string> keys = new();
        foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
        {
            if (keyCode != KeyCode.None && keyCode < KeyCode.Mouse0 && Input.GetKeyDown(keyCode))
            {
                keys.Add(keyCode.ToString());
            }
        }

        mod.Log.Write($"Shift input: text=\"{typed}\" keys=[{string.Join(", ", keys)}] buffer=\"{inputBuffer}\"");
    }

    private const string DefaultTeleportGamepadButton = "LeftStickButton";
    private const int LastStatueBossId = 35;
    private const int StatueRowsPerColumn = 9;
    private static readonly string[] StatueLevelNames = { "Attuned", "Ascended", "Radiant" };

    private bool gamepadHeroTaken;

    internal bool GamepadMode { get; private set; }

    internal int GamepadIndex { get; private set; } = -1;

    internal int GamepadFightLevel { get; private set; } = 2;

    internal string GamepadFightLevelName => StatueLevelNames[GamepadFightLevel];

    internal bool GamepadFightAlt { get; private set; }

    private static bool IsTeleportGamepadButtonPressed()
    {
        string raw = GodhomeQoL.GlobalSettings.TeleportKitControllerButton ?? DefaultTeleportGamepadButton;
        if (!Enum.TryParse(raw, out InputControlType button) || button == InputControlType.None)
        {
            return false;
        }

        return WasPressed(device => device.GetControl(button));
    }

    private static bool WasPressed(Func<InputDevice, InputControl?> select)
    {
        foreach (InputDevice device in InputManager.Devices)
        {
            if (device == null)
            {
                continue;
            }

            InputControl? control = select(device);
            if (control != null && control.WasPressed)
            {
                return true;
            }
        }

        return false;
    }

    private void TakeGamepadHero()
    {
        HeroController? hero = HeroController.instance;
        if (gamepadHeroTaken || hero == null || !hero.acceptingInput || hero.controlReqlinquished)
        {
            return;
        }

        hero.RelinquishControl();
        gamepadHeroTaken = true;
    }

    private void ReleaseGamepad()
    {
        GamepadMode = false;
        if (!gamepadHeroTaken)
        {
            return;
        }

        gamepadHeroTaken = false;
        HeroController? hero = HeroController.instance;
        GameManager? manager = GameManager.instance;
        if (hero != null && hero.controlReqlinquished && manager != null && !manager.IsInSceneTransition)
        {
            hero.RegainControl();
        }
    }

    private void HandleGamepadMenu()
    {
        if (WasPressed(device => device.Action2))
        {
            ShowMenu = false;
            inputBuffer = string.Empty;
            ReleaseGamepad();
            mod.Log.Write("Menu closed with gamepad");
            return;
        }

        if (WasPressed(device => device.LeftBumper))
        {
            ChangeGamepadPage(-1);
        }
        else if (WasPressed(device => device.RightBumper))
        {
            ChangeGamepadPage(1);
        }

        (int start, int end) = TeleportMenuGUI.GetPageRange(currentPage);
        end = Math.Min(end, mod.Data.Bosses.Length);
        int count = end - start;
        if (count <= 0)
        {
            return;
        }

        if (GamepadIndex < start || GamepadIndex >= end)
        {
            GamepadIndex = start;
        }

        int local = GamepadIndex - start;
        int row = local % StatueRowsPerColumn;
        if ((WasPressed(device => device.DPadUp) || WasPressed(device => device.LeftStickUp)) && row > 0)
        {
            local--;
        }
        else if ((WasPressed(device => device.DPadDown) || WasPressed(device => device.LeftStickDown)) && row < StatueRowsPerColumn - 1 && local + 1 < count)
        {
            local++;
        }
        else if ((WasPressed(device => device.DPadLeft) || WasPressed(device => device.LeftStickLeft)) && local >= StatueRowsPerColumn)
        {
            local -= StatueRowsPerColumn;
        }
        else if ((WasPressed(device => device.DPadRight) || WasPressed(device => device.LeftStickRight)) && local + StatueRowsPerColumn < count)
        {
            local += StatueRowsPerColumn;
        }

        GamepadIndex = start + local;

        if (WasPressed(device => device.Action3))
        {
            GamepadFightLevel = (GamepadFightLevel + 1) % StatueLevelNames.Length;
        }

        if (WasPressed(device => device.RightTrigger))
        {
            GamepadFightAlt = !GamepadFightAlt;
        }

        var boss = mod.Data.Bosses[GamepadIndex];
        if (WasPressed(device => device.Action1))
        {
            ShowMenu = false;
            ReleaseGamepad();
            mod.Log.Write($"Gamepad teleport to boss ID {boss.id} ({boss.name})");
            mod.Teleport.StartTeleport(boss.position, boss.scene);
        }
        else if (WasPressed(device => device.Action4))
        {
            if (boss.id < 1 || boss.id > LastStatueBossId || !string.Equals(boss.scene, "GG_Workshop", StringComparison.Ordinal))
            {
                QuickMenu.ShowStatusMessageFromExternal(string.Format("TeleportKit/FightNotStatue".Localize(), boss.id, LastStatueBossId));
                return;
            }

            ShowMenu = false;
            ReleaseGamepad();
            mod.Log.Write($"Gamepad statue fight {boss.id} ({boss.name}) at level {GamepadFightLevel}, second version {GamepadFightAlt}");
            mod.Teleport.StartStatueFight(boss.id, GamepadFightLevel, GamepadFightAlt);
        }
    }

    private void ChangeGamepadPage(int delta)
    {
        currentPage += delta;
        if (currentPage < 1)
        {
            currentPage = 4;
        }
        else if (currentPage > 4)
        {
            currentPage = 1;
        }

        inputBuffer = string.Empty;
        GamepadIndex = TeleportMenuGUI.GetPageRange(currentPage).start;
    }

    private void StartStatueFightFromInput(string text)
    {
        bool alt = IsAltFightInput(text);
        int first = text.IndexOf(FightSeparator);
        string levelPart = first > 0 ? text.Substring(0, first) : string.Empty;
        string numberPart = first >= 0 ? text.Substring(first + (alt ? 2 : 1)) : string.Empty;
        if (!int.TryParse(levelPart, out int difficulty)
            || !int.TryParse(numberPart, out int bossId))
        {
            mod.Log.Write($"Invalid statue fight input: {text}");
            QuickMenu.ShowStatusMessageFromExternal("TeleportKit/FightFormat".Localize());
            return;
        }

        mod.Teleport.StartStatueFight(bossId, difficulty - 1, alt);
    }

    private void SetCustomTeleportPoint()
    {
        if (HeroController.instance == null)
        {
            return;
        }

        mod.Data.CustomTeleportPosition = HeroController.instance.transform.position;
        mod.Data.CustomTeleportScene = GameManager.instance.sceneName;
        mod.Log.Write($"Custom teleport point set at {mod.Data.CustomTeleportPosition} in {mod.Data.CustomTeleportScene}");
    }

    private void TeleportToCustomPoint()
    {
        if (mod.Data.CustomTeleportPosition == null || string.IsNullOrEmpty(mod.Data.CustomTeleportScene))
        {
            mod.Log.Write("Attempted teleport to custom point but none was set");
            QuickMenu.ShowStatusMessageFromExternal("TeleportKit/CustomPointNotSet".Localize());
            return;
        }

        if (GameManager.instance.sceneName != mod.Data.CustomTeleportScene)
        {
            mod.Log.Write($"Custom teleport point is in another scene ({mod.Data.CustomTeleportScene})");
            QuickMenu.ShowStatusMessageFromExternal(string.Format("TeleportKit/CustomPointOtherRoom".Localize(), mod.Data.CustomTeleportScene));
            return;
        }

        mod.Log.Write($"Teleporting to custom point at {mod.Data.CustomTeleportPosition} in {mod.Data.CustomTeleportScene}");
        mod.Teleport.StartTeleport(mod.Data.CustomTeleportPosition.Value, mod.Data.CustomTeleportScene);
    }

    private void OnPause(On.HeroController.orig_Pause orig, HeroController self)
    {
        orig(self);
        ShowMenu = false;
        inputBuffer = string.Empty;
        IsRebindingMenuKey = false;
        IsRebindingSaveKey = false;
        IsRebindingTeleportKey = false;
        mod.Log.Write("Game paused, resetting menu state");
    }

    private static void ShowPauseHint()
    {
        if (pauseHintActive || GameManager.instance == null)
        {
            return;
        }

        pauseHintActive = true;
        _ = GameManager.instance.StartCoroutine(PauseHintRoutine());
    }

    private static void ShowBenchHint()
    {
        if (benchHintActive || GameManager.instance == null || Time.unscaledTime < benchHintCooldownUntil)
        {
            return;
        }

        benchHintCooldownUntil = Time.unscaledTime + 0.5f;
        benchHintActive = true;
        _ = GameManager.instance.StartCoroutine(BenchHintRoutine());
    }

    private static IEnumerator PauseHintRoutine()
    {
        const float duration = 3f;

        GameObject canvasObj = new("TeleportKitPauseHint");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject textObj = new("Text");
        textObj.transform.SetParent(canvasObj.transform, false);

        var txt = textObj.AddComponent<UnityEngine.UI.Text>();
        txt.text = "Please, first open the pause menu. Press ESC.";
        txt.fontSize = 26;
        txt.alignment = TextAnchor.LowerCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.color = Color.white;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        RectTransform rt = txt.rectTransform;
        rt.sizeDelta = new Vector2(900, 80);
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 40f);

        float elapsed = 0f;
        Color baseColor = txt.color;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float alpha = 1f - t;
            if (txt != null)
            {
                txt.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (canvasObj != null)
        {
            UObject.Destroy(canvasObj);
        }

        pauseHintActive = false;
    }

    private static IEnumerator BenchHintRoutine()
    {
        const float duration = 2f;

        GameObject canvasObj = new("TeleportKitBenchHint");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject textObj = new("Text");
        textObj.transform.SetParent(canvasObj.transform, false);

        var txt = textObj.AddComponent<UnityEngine.UI.Text>();
        txt.text = "Get up from the bench";
        txt.fontSize = 28;
        txt.alignment = TextAnchor.LowerCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.color = Color.white;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        RectTransform rt = txt.rectTransform;
        rt.sizeDelta = new Vector2(900, 80);
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 70f);

        float elapsed = 0f;
        Color baseColor = txt.color;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float alpha = 1f - t;
            if (txt != null)
            {
                txt.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (canvasObj != null)
        {
            UObject.Destroy(canvasObj);
        }

        benchHintActive = false;
    }
}
