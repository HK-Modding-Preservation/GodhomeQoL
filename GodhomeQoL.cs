namespace GodhomeQoL;

public sealed partial class GodhomeQoL : Mod, ITogglableMod, ICustomMenuMod
{
    public static GodhomeQoL? Instance { get; private set; }
    public static GodhomeQoL UnsafeInstance => Instance!;

    public static bool Active { get; private set; }

    public GodhomeQoL() : base(ModInfo.Name) => Instance = this;

    public override string GetVersion() => ModInfo.Version;

    public override List<(string, string)> GetPreloadNames() => new()
    {
        ("GG_Atrium", "GG_Challenge_Door"),
        ("Room_mapper", "Shop Menu")
    };

    public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
    {
        On.GameManager.OnApplicationQuit += OnApplicationQuit;
        USceneManager.activeSceneChanged += OnActiveSceneChanged;
        ModHooks.SavegameLoadHook += OnSavegameLoad;
        ModMenu.InstallHooks();
        ToggleableBindings.ToggleableBindings.Initialize(preloadedObjects);
        ResetPerSaveState();
        ModuleManager.Load();
        Active = true;
        SettingsAutoSave.Start();
    }

    public void Unload()
    {
        Active = false;
        SettingsAutoSave.Stop();
        On.GameManager.OnApplicationQuit -= OnApplicationQuit;
        USceneManager.activeSceneChanged -= OnActiveSceneChanged;
        ModHooks.SavegameLoadHook -= OnSavegameLoad;
        ModMenu.UninstallHooks();
        ModuleManager.Unload();
        ToggleableBindings.ToggleableBindings.Unload();
    }

    private void OnApplicationQuit(On.GameManager.orig_OnApplicationQuit orig, GameManager self)
    {
        try
        {
            ForceDisableSessionScopedModesForNextSession();
        }
        finally
        {
            orig(self);
        }
    }

    private static void ForceDisableSessionScopedModesForNextSession()
    {
        bool changed = false;
        changed |= ForceDisableAlwaysFuriousForNextSession();
        changed |= ForceDisablePantheonOrderModesForNextSession();
        if (changed)
        {
            SaveGlobalSettingsSafe();
        }
    }

    internal static void SaveGlobalSettingsSafe()
    {
        Instance?.SaveGlobalSettings();
        SettingsAutoSave.MarkSaved();
    }

    internal static void MarkMenuDirty() => ModMenu.MarkDirty();

    private static bool ForceDisableAlwaysFuriousForNextSession()
    {
        string moduleName = typeof(global::GodhomeQoL.Modules.BossChallenge.AlwaysFurious).Name;
        return ForceDisableModuleForNextSession(moduleName);
    }

    private static bool ForceDisablePantheonOrderModesForNextSession()
    {
        bool changed = false;

        bool hadRandomPantheonsEnabled = global::GodhomeQoL.Modules.BossChallenge.RandomPantheons.AnyPantheonEnabled;
        if (hadRandomPantheonsEnabled)
        {
            global::GodhomeQoL.Modules.BossChallenge.RandomPantheons.Pantheon1Enabled = false;
            global::GodhomeQoL.Modules.BossChallenge.RandomPantheons.Pantheon2Enabled = false;
            global::GodhomeQoL.Modules.BossChallenge.RandomPantheons.Pantheon3Enabled = false;
            global::GodhomeQoL.Modules.BossChallenge.RandomPantheons.Pantheon4Enabled = false;
            global::GodhomeQoL.Modules.BossChallenge.RandomPantheons.Pantheon5Enabled = false;
            changed = true;
        }

        bool hadTrueBossRushEnabled = global::GodhomeQoL.Modules.BossChallenge.TrueBossRush.AnyPantheonEnabled;
        if (hadTrueBossRushEnabled)
        {
            global::GodhomeQoL.Modules.BossChallenge.TrueBossRush.TrueBossRushPantheon1Enabled = false;
            global::GodhomeQoL.Modules.BossChallenge.TrueBossRush.TrueBossRushPantheon2Enabled = false;
            global::GodhomeQoL.Modules.BossChallenge.TrueBossRush.TrueBossRushPantheon3Enabled = false;
            global::GodhomeQoL.Modules.BossChallenge.TrueBossRush.TrueBossRushPantheon4Enabled = false;
            global::GodhomeQoL.Modules.BossChallenge.TrueBossRush.TrueBossRushPantheon5Enabled = false;
            changed = true;
        }

        changed |= ForceDisableModuleForNextSession(typeof(global::GodhomeQoL.Modules.BossChallenge.RandomPantheons).Name);
        changed |= ForceDisableModuleForNextSession(typeof(global::GodhomeQoL.Modules.BossChallenge.TrueBossRush).Name);

        QuickMenuMasterSettings settings = GlobalSettings.QuickMenuMasters ??= new QuickMenuMasterSettings();
        if (settings.RandomPantheonsEnabled)
        {
            settings.RandomPantheonsEnabled = false;
            changed = true;
        }

        if (settings.TrueBossRushEnabled)
        {
            settings.TrueBossRushEnabled = false;
            changed = true;
        }

        return changed;
    }

    private static bool ForceDisableModuleForNextSession(string moduleName)
    {
        bool changed = false;
        if (!Setting.Global.Modules.TryGetValue(moduleName, out bool isEnabledInSettings) || isEnabledInSettings)
        {
            Setting.Global.Modules[moduleName] = false;
            changed = true;
        }

        if (ModuleManager.TryGetModule(moduleName, out Module? module) && module.Enabled)
        {
            module.Enabled = false;
            changed = true;
        }

        return changed;
    }
}
