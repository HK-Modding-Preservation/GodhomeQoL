
using System.IO;

namespace GodhomeQoL
{
    public sealed partial class GodhomeQoL
    : IGlobalSettings<GlobalSettings>, ILocalSettings<LocalSettings>
    {
        public static GlobalSettings GlobalSettings { get; private set; } = new();
        public void OnLoadGlobal(GlobalSettings s)
        {
            bool isFirstRun = IsFirstRun();
            GlobalSettings = s;
            GlobalSettings.ShowHPOnDeath ??= new ShowHPOnDeathSettings();
            GlobalSettings.FastDreamWarp ??= new FastDreamWarpSettings();
            GlobalSettings.MaskDamage ??= new MaskDamageSettings();
            GlobalSettings.GearSwitcher ??= new GearSwitcherSettings();
            GlobalSettings.QuickMenuMasters ??= new QuickMenuMasterSettings();
            GlobalSettings.QuickMenuMasters.BossManipulateGlobalP5TouchedModules ??= new List<string>();
            GlobalSettings.QuickMenuMasters.BossManipulateGlobalP5EnabledModules ??= new List<string>();
            GlobalSettings.QuickMenuOrder ??= new List<string>();
            GlobalSettings.QuickMenuPositions ??= new Dictionary<string, QuickMenuEntryPosition>();
            GlobalSettings.QuickMenuCustomLabels ??= new Dictionary<string, string>();
            GlobalSettings.QuickMenuVisibility ??= new Dictionary<string, bool>();
            GlobalSettings.QuickMenuHotkey ??= "F3";

            EnsureGearSwitcherDefaults(GlobalSettings.GearSwitcher);

            if (isFirstRun)
            {
                Modules.Tools.QuickMenu.ApplyInitialDefaults();
                SaveGlobalSettingsSafe();
            }
        }
        public GlobalSettings OnSaveGlobal()
        {
            UpdatePerSaveMirror();
            return GlobalSettings;
        }

        public static LocalSettings LocalSettings { get; private set; } = new();

        private static int activeSaveSlot;
        private static bool localSettingsLoaded;

        public void OnLoadLocal(LocalSettings s)
        {
            LocalSettings = s;
            localSettingsLoaded = true;
            ApplyPerSaveModuleStates(s.PerSaveModules);
            if (GlobalSettings?.GearSwitcher != null
                && string.IsNullOrWhiteSpace(GlobalSettings.GearSwitcher.LastPreset)
                && !string.IsNullOrWhiteSpace(s.GearSwitcherLastPreset))
            {
                GlobalSettings.GearSwitcher.LastPreset = s.GearSwitcherLastPreset;
                SaveGlobalSettingsSafe();
            }
        }
        public LocalSettings OnSaveLocal()
        {
            if (GlobalSettings?.GearSwitcher != null)
            {
                LocalSettings.GearSwitcherLastPreset = GlobalSettings.GearSwitcher.LastPreset ?? "FullGear";
            }

            LocalSettings.PerSaveModules = CapturePerSaveModuleStates();
            if (string.IsNullOrEmpty(LocalSettings.SaveToken))
            {
                LocalSettings.SaveToken = NewSaveToken();
            }

            UpdatePerSaveMirror();
            return LocalSettings;
        }

        private static string NewSaveToken() => Guid.NewGuid().ToString("N");

        private static void OnSavegameLoad(int slot)
        {
            try
            {
                ApplySavegameLoad(slot);
            }
            finally
            {
                Modules.Tools.QuickMenu.RefreshQuickMenuEntryColors();
            }
        }

        private static void ApplySavegameLoad(int slot)
        {
            activeSaveSlot = slot;
            bool loadedOwnData = localSettingsLoaded;
            localSettingsLoaded = false;

            PerSaveMirror? mirror = null;
            _ = GlobalSettings?.PerSaveMirrors?.TryGetValue(slot.ToString(), out mirror);

            if (!loadedOwnData)
            {
                LocalSettings.SaveToken = NewSaveToken();
                return;
            }

            if (string.IsNullOrEmpty(LocalSettings.SaveToken))
            {
                if (mirror != null && !string.IsNullOrEmpty(mirror.Token))
                {
                    LocalSettings.SaveToken = mirror.Token;
                    ApplyPerSaveMirror(mirror);
                }
                else
                {
                    LocalSettings.SaveToken = NewSaveToken();
                }

                return;
            }

            if (mirror != null && string.Equals(mirror.Token, LocalSettings.SaveToken, StringComparison.Ordinal))
            {
                ApplyPerSaveMirror(mirror);
            }
        }

        private static void ApplyPerSaveMirror(PerSaveMirror mirror)
        {
            try
            {
                LocalSettings.ApplyChangedValues(mirror.Values);
                ApplyPerSaveModuleStates(mirror.Modules);
                global::GodhomeQoL.Utils.Logger.LogDebug($"Per-save settings restored for slot {activeSaveSlot}");
            }
            catch (Exception ex)
            {
                LogSuppressed(ex, "Settings.cs/ApplyPerSaveMirror");
            }
        }

        internal static void UpdatePerSaveMirror()
        {
            try
            {
                if (activeSaveSlot <= 0)
                {
                    GameManager? manager = GameManager.instance;
                    if (manager == null || manager.profileID <= 0 || !manager.IsGameplayScene())
                    {
                        return;
                    }

                    activeSaveSlot = manager.profileID;
                }

                if (GlobalSettings == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(LocalSettings.SaveToken))
                {
                    LocalSettings.SaveToken = NewSaveToken();
                }

                GlobalSettings.PerSaveMirrors ??= new Dictionary<string, PerSaveMirror>();
                GlobalSettings.PerSaveMirrors[activeSaveSlot.ToString()] = new PerSaveMirror
                {
                    Token = LocalSettings.SaveToken!,
                    Values = LocalSettings.CaptureChangedValues(),
                    Modules = CapturePerSaveModuleStates()
                };
            }
            catch (Exception ex)
            {
                LogSuppressed(ex, "Settings.cs/UpdatePerSaveMirror");
            }
        }

        private static List<Module>? perSaveModules;

        private static List<Module> GetPerSaveModules() => perSaveModules ??= ModuleManager.Modules.Values
            .Where(module => module.Name != "SegmentedP5" && HasLocalSettingFields(module.Type))
            .ToList();

        private static bool HasLocalSettingFields(Type type) => type
            .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Any(field => Attribute.IsDefined(field, typeof(LocalSettingAttribute)));

        internal static Dictionary<string, bool> CapturePerSaveModuleStates()
        {
            Dictionary<string, bool> states = new(StringComparer.Ordinal);
            foreach (Module module in GetPerSaveModules())
            {
                if (module.Enabled)
                {
                    states[module.Name] = true;
                }
            }

            return states;
        }

        internal static void ApplyPerSaveModuleStates(Dictionary<string, bool>? states)
        {
            foreach (Module module in GetPerSaveModules())
            {
                bool target = states != null && states.TryGetValue(module.Name, out bool on) && on;
                if (module.Enabled == target)
                {
                    continue;
                }

                try
                {
                    module.Enabled = target;
                }
                catch (Exception ex)
                {
                    LogSuppressed(ex, "Settings.cs/" + module.Name);
                }
            }
        }

        internal static void ResetPerSaveState()
        {
            if (activeSaveSlot > 0)
            {
                SaveGlobalSettingsSafe();
            }

            activeSaveSlot = 0;
            localSettingsLoaded = false;
            if (LocalSettings != null)
            {
                LocalSettings.SaveToken = null;
            }

            LocalSettings?.ResetFieldsToDefaults();
            ApplyPerSaveModuleStates(null);

            QuickMenuMasterSettings? masters = GlobalSettings?.QuickMenuMasters;
            if (masters != null
                && (masters.BossManipulateGlobalP5Enabled
                    || masters.BossManipulateGlobalP5TouchedModules?.Count > 0
                    || masters.BossManipulateGlobalP5EnabledModules?.Count > 0))
            {
                masters.BossManipulateGlobalP5Enabled = false;
                masters.BossManipulateGlobalP5TouchedModules?.Clear();
                masters.BossManipulateGlobalP5EnabledModules?.Clear();
                SaveGlobalSettingsSafe();
            }

            global::GodhomeQoL.Utils.Logger.LogDebug("Per-save settings reset to defaults");
        }

        private static void OnActiveSceneChanged(Scene from, Scene to)
        {
            if (string.Equals(to.name, "Menu_Title", StringComparison.Ordinal))
            {
                ResetPerSaveState();
            }
        }

        private static bool IsFirstRun()
        {
            string settingsPath = Path.Combine(Application.persistentDataPath, $"{ModInfo.Name}.GlobalSettings.json");
            if (File.Exists(settingsPath) || File.Exists(settingsPath + ".bak"))
            {
                return false;
            }

            return true;
        }

        private static void EnsureGearSwitcherDefaults(GearSwitcherSettings settings)
        {
            if (settings.Presets == null || settings.Presets.Count == 0)
            {
                settings.Presets = GearPresetDefaults.CreateDefaults();
            }
            else
            {
                Dictionary<string, GearPreset> defaults = GearPresetDefaults.CreateDefaults();
                foreach ((string name, GearPreset preset) in defaults)
                {
                    if (!settings.Presets.ContainsKey(name))
                    {
                        settings.Presets[name] = preset;
                    }
                }
            }

            if (settings.PresetOrder == null || settings.PresetOrder.Count == 0)
            {
                settings.PresetOrder = GearPresetDefaults.DefaultOrder();
            }
            else
            {
                foreach (string name in GearPresetDefaults.DefaultOrder())
                {
                    if (!settings.PresetOrder.Contains(name))
                    {
                        settings.PresetOrder.Add(name);
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(settings.FullGearDisplayName))
            {
                settings.FullGearDisplayName = "FullGear";
            }

            if (string.IsNullOrWhiteSpace(settings.LastPreset))
            {
                settings.LastPreset = "FullGear";
            }

            ResetBuiltinPresets(settings);
            MigrateGearSwitcherMoves(settings);
        }

        private static void ResetBuiltinPresets(GearSwitcherSettings settings)
        {
            if (settings.Presets == null)
            {
                return;
            }

            foreach ((string name, GearPreset defaults) in GearPresetDefaults.CreateDefaults())
            {
                if (settings.Presets.ContainsKey(name))
                {
                    settings.Presets[name] = defaults;
                }
            }
        }

        private static void MigrateGearSwitcherMoves(GearSwitcherSettings settings)
        {
            if (settings.Presets == null)
            {
                return;
            }

            foreach (GearPreset preset in settings.Presets.Values)
            {
                if (!preset.HasAllMoveAbilities)
                {
                    continue;
                }

                preset.HasMoveAbilities ??= new Dictionary<string, bool>();
                preset.HasMoveAbilities["AcidArmour"] = true;
                preset.HasMoveAbilities["Dash"] = true;
                preset.HasMoveAbilities["Walljump"] = true;
                preset.HasMoveAbilities["SuperDash"] = true;
                preset.HasMoveAbilities["ShadowDash"] = true;
                preset.HasMoveAbilities["DoubleJump"] = true;
                preset.HasAllMoveAbilities = false;
            }
        }
    }
}
