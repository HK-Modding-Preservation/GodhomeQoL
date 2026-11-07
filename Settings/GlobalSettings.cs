namespace GodhomeQoL.Settings;

public sealed class GlobalSettings : SettingBase<GlobalSettingAttribute>
{
    private readonly Dictionary<string, bool> modules = ModuleManager
        .Modules
        .Filter(pair => !pair.Value.Hidden)
        .ToDictionary(
            pair => pair.Key,
            pair => pair.Value.DefaultEnabled
        );

    [JsonProperty(PropertyName = nameof(modules))]
    public Dictionary<string, bool> Modules
    {
        get => modules;
        set
        {
            if (value == null)
            {
                return;
            }

            foreach (KeyValuePair<string, bool> pair in value)
            {
                if (modules.ContainsKey(pair.Key))
                {
                    modules[pair.Key] = pair.Value;

                    if (ModuleManager.TryGetModule(pair.Key, out Module? module))
                    {
                        module.Enabled = pair.Value;
                    }
                }
            }
        }
    }

    public ShowHPOnDeathSettings ShowHPOnDeath { get; set; } = new();

    public FastDreamWarpSettings FastDreamWarp { get; set; } = new();

    public MaskDamageSettings MaskDamage { get; set; } = new();

    public FreezeHitboxesSettings FreezeHitboxes { get; set; } = new();

    public GearSwitcherSettings GearSwitcher { get; set; } = new();

    public QuickMenuMasterSettings QuickMenuMasters { get; set; } = new();

    public List<string> QuickMenuOrder { get; set; } = new();

    public bool QuickMenuFreeLayout { get; set; } = false;

    public Dictionary<string, QuickMenuEntryPosition> QuickMenuPositions { get; set; } = new();

    public Dictionary<string, string> QuickMenuCustomLabels { get; set; } = new();

    public Dictionary<string, bool> QuickMenuVisibility { get; set; } = new();

    public string QuickMenuHotkey { get; set; } = "F3";

    public string QuickMenuControllerButton { get; set; } = "RightStickButton";

    public string TeleportKitControllerButton { get; set; } = "LeftStickButton";

    public int QuickMenuOpacity { get; set; } = 100;

    public Dictionary<string, string> QuickMenuOverlayHotkeys { get; set; } = new();

    public string NailDamageCheckKeybind { get; set; } = string.Empty;

    public Dictionary<string, PerSaveMirror> PerSaveMirrors { get; set; } = new();

    public List<SettingsPreset> SettingsPresets { get; set; } = new();

    public string? ActiveSettingsPreset { get; set; }
}
