using System.Globalization;

namespace GodhomeQoL
{
    public sealed partial class GodhomeQoL
    {
        internal const int SettingsPresetNameLimit = 40;

        private const string GearSectionKey = "GearSwitcherLastPreset";

        private static readonly HashSet<string> PresetHubScenes = new(StringComparer.Ordinal)
        {
            "GG_Workshop",
            "GG_Atrium",
            "GG_Atrium_Roof"
        };

        private static readonly JsonSerializerSettings PresetSectionJson = new()
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace
        };

        internal static List<SettingsPreset> SettingsPresetList => GlobalSettings.SettingsPresets ??= new List<SettingsPreset>();

        internal static SettingsPreset? FindSettingsPreset(string? name) => string.IsNullOrEmpty(name)
            ? null
            : SettingsPresetList.FirstOrDefault(preset => string.Equals(preset.Name, name, StringComparison.OrdinalIgnoreCase));

        internal static bool IsSettingsPresetNameTaken(string name, SettingsPreset? except = null) => SettingsPresetList
            .Any(preset => !ReferenceEquals(preset, except) && string.Equals(preset.Name, name, StringComparison.OrdinalIgnoreCase));

        internal static string NextSettingsPresetName()
        {
            for (int i = 1; ; i++)
            {
                string name = $"Preset {i}";
                if (!IsSettingsPresetNameTaken(name))
                {
                    return name;
                }
            }
        }

        internal static SettingsPreset CaptureSettingsPreset(string name) => new()
        {
            Name = name,
            Modules = PresetModules().ToDictionary(module => module.Name, module => module.Enabled, StringComparer.Ordinal),
            GlobalValues = GlobalSettings.CaptureChangedValues(),
            LocalValues = LocalSettings.CaptureChangedValues(),
            Sections = CapturePresetSections()
        };

        internal static SettingsPreset CreateSettingsPresetFromCurrent()
        {
            SettingsPreset preset = CaptureSettingsPreset(NextSettingsPresetName());
            SettingsPresetList.Add(preset);
            GlobalSettings.ActiveSettingsPreset = preset.Name;
            SaveGlobalSettingsSafe();
            return preset;
        }

        internal static void OverwriteSettingsPreset(SettingsPreset preset)
        {
            SettingsPreset current = CaptureSettingsPreset(preset.Name);
            preset.Modules = current.Modules;
            preset.GlobalValues = current.GlobalValues;
            preset.LocalValues = current.LocalValues;
            preset.Sections = current.Sections;
            GlobalSettings.ActiveSettingsPreset = preset.Name;
            SaveGlobalSettingsSafe();
        }

        internal static bool RenameSettingsPreset(SettingsPreset preset, string newName)
        {
            string name = (newName ?? string.Empty).Trim();
            if (name.Length > SettingsPresetNameLimit)
            {
                name = name.Substring(0, SettingsPresetNameLimit);
            }

            if (name.Length == 0 || IsSettingsPresetNameTaken(name, preset))
            {
                return false;
            }

            if (string.Equals(GlobalSettings.ActiveSettingsPreset, preset.Name, StringComparison.OrdinalIgnoreCase))
            {
                GlobalSettings.ActiveSettingsPreset = name;
            }

            preset.Name = name;
            SaveGlobalSettingsSafe();
            return true;
        }

        internal static void DeleteSettingsPreset(SettingsPreset preset)
        {
            _ = SettingsPresetList.Remove(preset);
            if (string.Equals(GlobalSettings.ActiveSettingsPreset, preset.Name, StringComparison.OrdinalIgnoreCase))
            {
                GlobalSettings.ActiveSettingsPreset = null;
            }

            SaveGlobalSettingsSafe();
        }

        internal static bool CanApplySettingsPreset()
        {
            GameManager? manager = GameManager.instance;
            if (manager == null || manager.IsInSceneTransition || !manager.IsGameplayScene())
            {
                return false;
            }

            if (BossSceneController.IsBossScene)
            {
                return false;
            }

            string scene = manager.sceneName ?? string.Empty;
            return !(BossSequenceController.IsInSequence
                && scene.StartsWith("GG_", StringComparison.Ordinal)
                && !PresetHubScenes.Contains(scene));
        }

        internal static bool SettingsPresetMatchesCurrent(SettingsPreset preset)
        {
            SettingsPreset current = CaptureSettingsPreset(preset.Name);
            return SameModules(current.Modules, preset.Modules)
                && ChangedValueTypes(current.GlobalValues, preset.GlobalValues).Count == 0
                && ChangedValueTypes(current.LocalValues, preset.LocalValues).Count == 0
                && ChangedSections(current.Sections, preset.Sections).Count == 0;
        }

        internal static void ApplySettingsPreset(SettingsPreset preset)
        {
            SettingsPreset before = CaptureSettingsPreset(preset.Name);
            HashSet<string> changedTypes = ChangedValueTypes(before.GlobalValues, preset.GlobalValues);
            changedTypes.UnionWith(ChangedValueTypes(before.LocalValues, preset.LocalValues));
            HashSet<string> changedSections = ChangedSections(before.Sections, preset.Sections);

            List<Module> enableAfter = new();
            foreach (Module module in PresetModules().ToList())
            {
                bool target = preset.Modules.TryGetValue(module.Name, out bool enabled) ? enabled : module.Enabled;
                bool reload = module.Enabled && target && SettingsChangedFor(module, changedTypes, changedSections);
                if (module.Enabled && (!target || reload))
                {
                    SetPresetModuleEnabled(module, false);
                }

                if (target)
                {
                    enableAfter.Add(module);
                }
            }

            try
            {
                GlobalSettings.ApplyChangedValues(preset.GlobalValues);
                LocalSettings.ApplyChangedValues(preset.LocalValues);
                ApplyPresetSections(preset.Sections);
            }
            catch (Exception ex)
            {
                LogSuppressed(ex, "SettingsPresets.cs/ApplyValues");
            }

            foreach (Module module in enableAfter)
            {
                if (!module.Enabled)
                {
                    SetPresetModuleEnabled(module, true);
                }
            }

            ApplyPresetGear(preset.Sections, changedSections);

            GlobalSettings.ActiveSettingsPreset = preset.Name;
            UpdatePerSaveMirror();
            SaveGlobalSettingsSafe();
            global::GodhomeQoL.Utils.Logger.LogDebug($"Settings preset applied: {preset.Name}");
        }

        private static IEnumerable<Module> PresetModules() => ModuleManager.Modules.Values
            .Where(module => !module.Hidden && !module.AlwaysEnabled);

        private static void SetPresetModuleEnabled(Module module, bool value)
        {
            try
            {
                module.Enabled = value;
            }
            catch (Exception ex)
            {
                LogSuppressed(ex, "SettingsPresets.cs/" + module.Name);
            }
        }

        private static bool SettingsChangedFor(Module module, HashSet<string> changedTypes, HashSet<string> changedSections)
        {
            if (changedSections.Contains(module.Name))
            {
                return true;
            }

            string typeName = module.Type.FullName ?? module.Name;
            string nestedPrefix = typeName + "+";
            return changedTypes.Any(type => string.Equals(type, typeName, StringComparison.Ordinal)
                || type.StartsWith(nestedPrefix, StringComparison.Ordinal));
        }

        private static bool SameModules(Dictionary<string, bool> current, Dictionary<string, bool> preset)
        {
            foreach ((string name, bool enabled) in current)
            {
                if (preset.TryGetValue(name, out bool stored) && stored != enabled)
                {
                    return false;
                }
            }

            return true;
        }

        private static HashSet<string> ChangedValueTypes(Dictionary<string, object> left, Dictionary<string, object> right)
        {
            HashSet<string> types = new(StringComparer.Ordinal);
            foreach (string key in left.Keys.Union(right.Keys))
            {
                _ = left.TryGetValue(key, out object? a);
                _ = right.TryGetValue(key, out object? b);
                if (!SamePresetValue(a, b))
                {
                    int dot = key.LastIndexOf('.');
                    _ = types.Add(dot > 0 ? key.Substring(0, dot) : key);
                }
            }

            return types;
        }

        private static bool SamePresetValue(object? a, object? b)
        {
            if (a == null || b == null)
            {
                return a == null && b == null;
            }

            if (a is string || b is string || a is bool || b is bool)
            {
                return string.Equals(
                    Convert.ToString(a, CultureInfo.InvariantCulture),
                    Convert.ToString(b, CultureInfo.InvariantCulture),
                    StringComparison.Ordinal);
            }

            try
            {
                double x = Convert.ToDouble(a, CultureInfo.InvariantCulture);
                double y = Convert.ToDouble(b, CultureInfo.InvariantCulture);
                return Math.Abs(x - y) <= 1e-4 * Math.Max(1d, Math.Abs(x));
            }
            catch (Exception)
            {
                return Equals(a, b);
            }
        }

        private static HashSet<string> ChangedSections(Dictionary<string, string> current, Dictionary<string, string> preset)
        {
            HashSet<string> changed = new(StringComparer.Ordinal);
            foreach ((string key, string json) in preset)
            {
                if (!current.TryGetValue(key, out string? now) || !string.Equals(now, json, StringComparison.Ordinal))
                {
                    _ = changed.Add(key);
                }
            }

            return changed;
        }

        private static Dictionary<string, string> CapturePresetSections()
        {
            GlobalSettings settings = GlobalSettings;
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ShowHPOnDeath"] = JsonConvert.SerializeObject(settings.ShowHPOnDeath),
                ["FastDreamWarp"] = JsonConvert.SerializeObject(settings.FastDreamWarp),
                ["MaskDamage"] = JsonConvert.SerializeObject(settings.MaskDamage),
                ["FreezeHitboxes"] = JsonConvert.SerializeObject(settings.FreezeHitboxes),
                ["QuickMenuMasters"] = JsonConvert.SerializeObject(settings.QuickMenuMasters),
                ["QuickMenuOverlayHotkeys"] = JsonConvert.SerializeObject(settings.QuickMenuOverlayHotkeys ?? new Dictionary<string, string>()),
                ["NailDamageCheck"] = JsonConvert.SerializeObject(settings.NailDamageCheckKeybind ?? string.Empty),
                [GearSectionKey] = JsonConvert.SerializeObject(settings.GearSwitcher?.LastPreset ?? "FullGear")
            };
        }

        private static void ApplyPresetSections(Dictionary<string, string> sections)
        {
            GlobalSettings settings = GlobalSettings;
            PopulatePresetSection(sections, "ShowHPOnDeath", settings.ShowHPOnDeath ??= new ShowHPOnDeathSettings());
            PopulatePresetSection(sections, "FastDreamWarp", settings.FastDreamWarp ??= new FastDreamWarpSettings());
            PopulatePresetSection(sections, "MaskDamage", settings.MaskDamage ??= new MaskDamageSettings());
            PopulatePresetSection(sections, "FreezeHitboxes", settings.FreezeHitboxes ??= new FreezeHitboxesSettings());
            PopulatePresetSection(sections, "QuickMenuMasters", settings.QuickMenuMasters ??= new QuickMenuMasterSettings());
            settings.QuickMenuMasters.BossManipulateGlobalP5TouchedModules ??= new List<string>();
            settings.QuickMenuMasters.BossManipulateGlobalP5EnabledModules ??= new List<string>();

            if (TryReadPresetSection(sections, "QuickMenuOverlayHotkeys", out Dictionary<string, string>? hotkeys))
            {
                settings.QuickMenuOverlayHotkeys = hotkeys ?? new Dictionary<string, string>();
            }

            if (TryReadPresetSection(sections, "NailDamageCheck", out string? nailKey))
            {
                settings.NailDamageCheckKeybind = nailKey ?? string.Empty;
            }
        }

        private static void ApplyPresetGear(Dictionary<string, string> sections, HashSet<string> changedSections)
        {
            if (!changedSections.Contains(GearSectionKey)
                || !TryReadPresetSection(sections, GearSectionKey, out string? gear)
                || string.IsNullOrWhiteSpace(gear)
                || !ModuleManager.IsModuleLoaded<Modules.Tools.GearSwitcher>())
            {
                return;
            }

            Dictionary<string, GearPreset>? gears = GlobalSettings.GearSwitcher?.Presets;
            string? match = gears?.Keys.FirstOrDefault(name => string.Equals(name, gear, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                return;
            }

            try
            {
                Modules.Tools.GearSwitcher.ApplyPreset(match, allowQueue: true);
            }
            catch (Exception ex)
            {
                LogSuppressed(ex, "SettingsPresets.cs/Gear");
            }
        }

        private static void PopulatePresetSection(Dictionary<string, string> sections, string key, object target)
        {
            if (!sections.TryGetValue(key, out string? json) || string.IsNullOrEmpty(json))
            {
                return;
            }

            try
            {
                JsonConvert.PopulateObject(json, target, PresetSectionJson);
            }
            catch (Exception ex)
            {
                LogSuppressed(ex, "SettingsPresets.cs/" + key);
            }
        }

        private static bool TryReadPresetSection<T>(Dictionary<string, string> sections, string key, out T? value)
        {
            value = default;
            if (!sections.TryGetValue(key, out string? json) || string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                value = JsonConvert.DeserializeObject<T>(json);
                return true;
            }
            catch (Exception ex)
            {
                LogSuppressed(ex, "SettingsPresets.cs/" + key);
                return false;
            }
        }
    }
}
