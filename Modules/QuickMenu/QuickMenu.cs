using System.IO;
using GodhomeQoL.Settings;
using InControl;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    public override bool DefaultEnabled => true;
    public override bool Hidden => true;
    public override bool AlwaysEnabled => true;

    private GameObject? root;
    private static QuickMenuController? controller;
    private static Satchel.BetterMenus.MenuButton? quickMenuHotkeyButton;
    private const string DefaultQuickMenuHotkey = "F3";
    private const string OverlayControllerPrefix = "Pad:";

    internal static Satchel.BetterMenus.MenuButton QuickMenuHotkeyButton() =>
        quickMenuHotkeyButton = new Satchel.BetterMenus.MenuButton(
            FormatQuickMenuButtonName("Settings/QuickMenu/Hotkey".Localize(), GetQuickMenuHotkeyLabel()),
            "Settings/QuickMenu/HotkeyDesc".Localize(),
            _ => StartQuickMenuHotkeyRebind(),
            false
        );

    internal static void StartQuickMenuHotkeyRebind()
    {
        controller?.StartQuickMenuHotkeyRebind();
    }

    internal static void ResetFreeMenuPositions()
    {
        GodhomeQoL.GlobalSettings.QuickMenuPositions = new Dictionary<string, QuickMenuEntryPosition>();
        GodhomeQoL.SaveGlobalSettingsSafe();
        controller?.ResetFreeMenuPositions();
    }

    internal static void RefreshQuickMenuEntryColors()
    {
        controller?.RefreshQuickMenuEntryColors();
    }

    internal static void RefreshPantheonCompatibilityUi()
    {
        controller?.RefreshPantheonCompatibilityUiFromExternal();
    }

    internal static void InvalidateQolSnapshotFromExternal()
    {
        QuickMenuMasterSettings settings = GodhomeQoL.GlobalSettings.QuickMenuMasters ??= new QuickMenuMasterSettings();
        settings.QolHasSnapshot = false;
        settings.QolSavedHallOfGods = false;
        controller?.InvalidateQolSnapshotFromExternal();
        GodhomeQoL.SaveGlobalSettingsSafe();
    }

    internal static void InvalidateMenuAnimationSnapshotFromExternal()
    {
        QuickMenuMasterSettings settings = GodhomeQoL.GlobalSettings.QuickMenuMasters ??= new QuickMenuMasterSettings();
        settings.MenuAnimHasSnapshot = false;
        controller?.InvalidateMenuAnimationSnapshotFromExternal();
        GodhomeQoL.SaveGlobalSettingsSafe();
    }

    internal static void InvalidateBossAnimationSnapshotFromExternal()
    {
        QuickMenuMasterSettings settings = GodhomeQoL.GlobalSettings.QuickMenuMasters ??= new QuickMenuMasterSettings();
        settings.BossAnimHasSnapshot = false;
        controller?.InvalidateBossAnimationSnapshotFromExternal();
        GodhomeQoL.SaveGlobalSettingsSafe();
    }

    internal static void ApplyInitialDefaults()
    {
        QuickMenuController.ApplyInitialDefaults();
    }

    internal static bool IsHotkeyInputBlocked()
    {
        return controller != null && controller.IsHotkeyInputBlocked();
    }

    internal static bool IsAnyUiVisible()
    {
        return controller != null && controller.IsAnyUiVisible();
    }

    internal static bool IsSpeedChangerOverlayVisible()
    {
        return controller != null && controller.IsSpeedChangerVisible();
    }

    internal static void SetSpeedChangerOverlayVisible(bool value)
    {
        controller?.SetSpeedChangerVisibleFromExternal(value);
    }

    internal static void ShowStatusMessageFromExternal(string message)
    {
        controller?.ShowStatusMessageFromExternal(message);
    }

    internal static bool TryGetHotkeyConflictOwnersExceptSelf(KeyCode key, string selfOwner, out string ownersText)
    {
        if (controller != null)
        {
            return controller.TryGetHotkeyConflictOwnersExceptSelfExternal(key, selfOwner, out ownersText);
        }

        ownersText = string.Empty;
        return false;
    }

    private static void SetController(QuickMenuController? value)
    {
        controller = value;
    }

    private static void UpdateQuickMenuHotkeyButton(string value)
    {
        if (quickMenuHotkeyButton == null)
        {
            return;
        }

        quickMenuHotkeyButton.Name = FormatQuickMenuButtonName("Settings/QuickMenu/Hotkey".Localize(), value);
        quickMenuHotkeyButton.Update();
    }

    internal static string GetQuickMenuHotkeyLabel() => FormatQuickMenuKeyLabel(GetQuickMenuToggleKey());

    private static string FormatQuickMenuButtonName(string title, string value) => $"{title}: {value}";

    private static string FormatQuickMenuKeyLabel(KeyCode key) => key == KeyCode.None
        ? "Settings/FastReload/NotSet".Localize()
        : key.ToString();

    private static KeyCode GetQuickMenuToggleKey()
    {
        string? raw = GodhomeQoL.GlobalSettings.QuickMenuHotkey;
        if (raw == null)
        {
            raw = DefaultQuickMenuHotkey;
        }

        if (raw.Length == 0)
        {
            return KeyCode.None;
        }

        return Enum.TryParse(raw, true, out KeyCode key)
            ? key
            : KeyCode.F3;
    }

    private protected override void Load()
    {
        if (root != null)
        {
            return;
        }

        root = new GameObject("GodhomeQoL_QuickMenu");
        UObject.DontDestroyOnLoad(root);
        root.AddComponent<QuickMenuController>();
        LogDebug("QuickMenu loaded");
    }

    private protected override void Unload()
    {
        if (root != null)
        {
            UObject.Destroy(root);
            root = null;
        }
    }

}
