using GodhomeQoL.Modules.Tools;

namespace GodhomeQoL.Modules.QoL;

public sealed class TeleportKit : Module
{
    internal enum HotkeySlot
    {
        Menu,
        SaveTeleport,
        Teleport
    }

    internal static TeleportKit? Instance { get; private set; }

    [GlobalSetting]
    public static KeyCode MenuHotkey = KeyCode.F6;

    [GlobalSetting]
    public static KeyCode SaveTeleportHotkey = KeyCode.R;

    [GlobalSetting]
    public static KeyCode TeleportHotkey = KeyCode.T;

    internal TeleportKitLogger Log { get; private set; } = null!;
    internal TeleportData Data { get; private set; } = null!;
    internal TeleportInputHandler Input { get; private set; } = null!;
    internal TeleportManager Teleport { get; private set; } = null!;
    internal TeleportMenuGUI GUI { get; private set; } = null!;

    internal KeyCode MenuKey => MenuHotkey;
    internal KeyCode SaveTeleportKey => IsValidHotkey(SaveTeleportHotkey) ? SaveTeleportHotkey : KeyCode.R;
    internal KeyCode TeleportKey => IsValidHotkey(TeleportHotkey) ? TeleportHotkey : KeyCode.T;

    public override bool DefaultEnabled => false;
    public override ToggleableLevel ToggleableLevel => ToggleableLevel.AnyTime;

    private static bool IsValidHotkey(KeyCode key) => key != KeyCode.None;

    private static KeyCode GetEffectiveKey(HotkeySlot slot) =>
        slot switch
        {
            HotkeySlot.Menu => MenuHotkey,
            HotkeySlot.SaveTeleport => SaveTeleportHotkey == KeyCode.None ? KeyCode.R : SaveTeleportHotkey,
            HotkeySlot.Teleport => TeleportHotkey == KeyCode.None ? KeyCode.T : TeleportHotkey,
            _ => KeyCode.None
        };

    private static string GetOwnerLabel(HotkeySlot slot) =>
        slot switch
        {
            HotkeySlot.Menu => "TeleportKit/MenuHotkey".Localize(),
            HotkeySlot.SaveTeleport => "TeleportKit/SaveHotkey".Localize(),
            HotkeySlot.Teleport => "TeleportKit/TeleportHotkey".Localize(),
            _ => "TeleportKit"
        };

    private static bool TryGetInternalConflictOwners(HotkeySlot slot, KeyCode key, out string ownersText)
    {
        var owners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (slot != HotkeySlot.Menu && GetEffectiveKey(HotkeySlot.Menu) == key)
        {
            owners.Add(GetOwnerLabel(HotkeySlot.Menu));
        }

        if (slot != HotkeySlot.SaveTeleport && GetEffectiveKey(HotkeySlot.SaveTeleport) == key)
        {
            owners.Add(GetOwnerLabel(HotkeySlot.SaveTeleport));
        }

        if (slot != HotkeySlot.Teleport && GetEffectiveKey(HotkeySlot.Teleport) == key)
        {
            owners.Add(GetOwnerLabel(HotkeySlot.Teleport));
        }

        ownersText = string.Join(", ", owners);
        return owners.Count > 0;
    }

    internal static bool TryAssignHotkey(HotkeySlot slot, KeyCode key, out string failureReason)
    {
        failureReason = string.Empty;

        if (key == KeyCode.None)
        {
            failureReason = "TeleportKit/KeyUnavailable".Localize();
            return false;
        }

        if ((slot == HotkeySlot.SaveTeleport || slot == HotkeySlot.Teleport)
            && (key == KeyCode.LeftControl || key == KeyCode.RightControl))
        {
            failureReason = "TeleportKit/KeyUnavailable".Localize();
            return false;
        }

        string selfOwner = GetOwnerLabel(slot);
        if (QuickMenu.TryGetHotkeyConflictOwnersExceptSelf(key, selfOwner, out string conflictOwners))
        {
            failureReason = string.Format("TeleportKit/KeyInUse".Localize(), conflictOwners);
            return false;
        }

        if (TryGetInternalConflictOwners(slot, key, out string internalOwners))
        {
            failureReason = string.Format("TeleportKit/KeyConflict".Localize(), internalOwners);
            return false;
        }

        switch (slot)
        {
            case HotkeySlot.Menu:
                MenuHotkey = key;
                break;
            case HotkeySlot.SaveTeleport:
                SaveTeleportHotkey = key;
                break;
            case HotkeySlot.Teleport:
                TeleportHotkey = key;
                break;
        }

        return true;
    }

    private protected override void Load()
    {
        Instance = this;

        Log = new TeleportKitLogger();
        Data = new TeleportData();
        Input = new TeleportInputHandler(this);
        Teleport = new TeleportManager(this);
        GUI = new TeleportMenuGUI(this);

        ModHooks.AfterSavegameLoadHook += OnAfterSavegameLoad;

        Log.Write("Teleport kit initialized");
    }

    private protected override void Unload()
    {
        ModHooks.AfterSavegameLoadHook -= OnAfterSavegameLoad;

        GUI?.Dispose();
        Input?.Dispose();
        Teleport?.Dispose();
        Log?.Dispose();

        Instance = null;
    }

    private void OnAfterSavegameLoad(SaveGameData _)
    {
        Input?.ResetPauseFlag();
    }
}
