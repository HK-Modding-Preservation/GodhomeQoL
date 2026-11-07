using Mono.Cecil.Cil;
using MonoMod.Cil;
using IL;
using InControl;
using GodhomeQoL.Modules.BossChallenge;
using GodhomeQoL.Modules.QoL;
using ToggleableBindings;
using ToggleableBindings.VanillaBindings;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class GearSwitcher : Module
{
    internal static IReadOnlyList<string> GetPresetOrder()
    {
        EnsurePresetDefaults();

        List<string> defaults = GearPresetDefaults.DefaultOrder();
        List<string> order = Settings.PresetOrder ?? new List<string>(defaults);
        List<string> result = new();

        foreach (string name in defaults)
        {
            if (Settings.Presets.ContainsKey(name) && !result.Contains(name))
            {
                result.Add(name);
            }
        }

        foreach (string name in order)
        {
            if (Settings.Presets.ContainsKey(name) && !result.Contains(name))
            {
                result.Add(name);
            }
        }

        foreach (string name in Settings.Presets.Keys)
        {
            if (!result.Contains(name))
            {
                result.Add(name);
            }
        }

        Settings.PresetOrder = result;
        return result;
    }

    internal static bool TryGetPreset(string name, out GearPreset preset)
    {
        EnsurePresetDefaults();
        return Settings.Presets.TryGetValue(name, out preset!);
    }

    internal static void ResetDefaults()
    {
        Settings.Presets = GearPresetDefaults.CreateDefaults();
        Settings.PresetOrder = GearPresetDefaults.DefaultOrder();
        GodhomeQoL.GlobalSettings.GearSwitcher.LastPreset = "FullGear";
        GodhomeQoL.SaveGlobalSettingsSafe();
    }

    internal static bool ResetBuiltinPreset(string presetName)
    {
        presetName = NormalizeBuiltinPresetName(presetName);
        if (!IsBuiltinPresetName(presetName))
        {
            return false;
        }

        EnsurePresetDefaults();
        foreach (KeyValuePair<string, GearPreset> entry in GearPresetDefaults.CreateDefaults())
        {
            if (string.Equals(entry.Key, presetName, StringComparison.OrdinalIgnoreCase))
            {
                Settings.Presets[entry.Key] = entry.Value;
                GodhomeQoL.SaveGlobalSettingsSafe();
                return true;
            }
        }

        return false;
    }

    private static void EnsurePresetDefaults()
    {
        if (Settings.Presets == null || Settings.Presets.Count == 0)
        {
            Settings.Presets = GearPresetDefaults.CreateDefaults();
        }

        MigrateLegacyPresetName("O4", "04");
        MigrateLegacyPresetName("Ow", "0w");

        Dictionary<string, GearPreset> defaults = GearPresetDefaults.CreateDefaults();
        foreach (KeyValuePair<string, GearPreset> entry in defaults)
        {
            if (!Settings.Presets.ContainsKey(entry.Key))
            {
                Settings.Presets[entry.Key] = entry.Value;
            }
        }

        if (Settings.PresetOrder != null)
        {
            Settings.PresetOrder = Settings.PresetOrder.Where(Settings.Presets.ContainsKey).ToList();
        }
    }

    private static void MigrateLegacyPresetName(string legacyName, string newName)
    {
        if (Settings.Presets == null)
        {
            return;
        }

        string? legacyKey = Settings.Presets.Keys.FirstOrDefault(key => string.Equals(key, legacyName, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(legacyKey))
        {
            return;
        }

        if (Settings.Presets.TryGetValue(newName, out _))
        {
            Settings.Presets.Remove(legacyKey);
        }
        else if (Settings.Presets.TryGetValue(legacyKey, out GearPreset preset))
        {
            Settings.Presets.Remove(legacyKey);
            preset.Name = newName;
            Settings.Presets[newName] = preset;
        }

        if (Settings.PresetOrder != null)
        {
            for (int i = 0; i < Settings.PresetOrder.Count; i++)
            {
                if (string.Equals(Settings.PresetOrder[i], legacyName, StringComparison.OrdinalIgnoreCase))
                {
                    Settings.PresetOrder[i] = newName;
                }
            }
        }

        if (string.Equals(GodhomeQoL.GlobalSettings.GearSwitcher.LastPreset, legacyName, StringComparison.OrdinalIgnoreCase))
        {
            GodhomeQoL.GlobalSettings.GearSwitcher.LastPreset = newName;
        }
    }

    internal static string CreateCustomPresetFromCurrentGear()
    {
        EnsurePresetDefaults();
        GearPreset? preset = null;
        string currentName = NormalizeBuiltinPresetName(GetLastPresetName());
        if (!string.IsNullOrEmpty(currentName) && TryGetPreset(currentName, out GearPreset current) && current != null)
        {
            try
            {
                preset = JsonConvert.DeserializeObject<GearPreset>(JsonConvert.SerializeObject(current));
            }
            catch (Exception ex)
            {
                LogSuppressed(ex, "GearSwitcher.Presets.cs/CreateCustomPresetFromCurrentGear");
            }
        }

        if (preset == null && !GearPresetDefaults.CreateDefaults().TryGetValue("FullGear", out preset))
        {
            return string.Empty;
        }

        string name = GetNextPresetName();
        preset.Name = name;
        Settings.Presets[name] = preset;
        Settings.PresetOrder ??= new List<string>(GearPresetDefaults.DefaultOrder());
        if (!Settings.PresetOrder.Contains(name))
        {
            Settings.PresetOrder.Add(name);
        }

        GodhomeQoL.SaveGlobalSettingsSafe();
        return name;
    }

    internal static bool RenameCustomPreset(string currentName, string newName)
    {
        if (string.IsNullOrWhiteSpace(currentName) || string.IsNullOrWhiteSpace(newName))
        {
            return false;
        }

        EnsurePresetDefaults();
        if (IsBuiltinPresetName(currentName))
        {
            return false;
        }

        if (!Settings.Presets.TryGetValue(currentName, out GearPreset preset))
        {
            return false;
        }

        if (IsBuiltinPresetName(newName))
        {
            return false;
        }

        foreach (string key in Settings.Presets.Keys)
        {
            if (string.Equals(key, newName, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(key, currentName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        if (string.Equals(currentName, newName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        Settings.Presets.Remove(currentName);
        preset.Name = newName;
        Settings.Presets[newName] = preset;

        if (Settings.PresetOrder != null)
        {
            for (int i = 0; i < Settings.PresetOrder.Count; i++)
            {
                if (string.Equals(Settings.PresetOrder[i], currentName, StringComparison.OrdinalIgnoreCase))
                {
                    Settings.PresetOrder[i] = newName;
                }
            }
        }

        if (string.Equals(GodhomeQoL.GlobalSettings.GearSwitcher.LastPreset, currentName, StringComparison.OrdinalIgnoreCase))
        {
            GodhomeQoL.GlobalSettings.GearSwitcher.LastPreset = newName;
        }

        GodhomeQoL.SaveGlobalSettingsSafe();
        return true;
    }

    internal static bool DeleteCustomPreset(string presetName)
    {
        if (string.IsNullOrWhiteSpace(presetName))
        {
            return false;
        }

        EnsurePresetDefaults();
        if (IsBuiltinPresetName(presetName))
        {
            return false;
        }

        bool removed = Settings.Presets.Remove(presetName);
        if (!removed)
        {
            return false;
        }

        if (Settings.PresetOrder != null)
        {
            Settings.PresetOrder.RemoveAll(name => string.Equals(name, presetName, StringComparison.OrdinalIgnoreCase));
        }

        if (string.Equals(GodhomeQoL.GlobalSettings.GearSwitcher.LastPreset, presetName, StringComparison.OrdinalIgnoreCase))
        {
            GodhomeQoL.GlobalSettings.GearSwitcher.LastPreset = "FullGear";
        }

        GodhomeQoL.SaveGlobalSettingsSafe();
        return true;
    }

    private static string GetNextPresetName()
    {
        const string prefix = "Preset ";
        HashSet<string> existing = new(Settings.Presets.Keys, StringComparer.OrdinalIgnoreCase);
        int index = 1;
        while (existing.Contains($"{prefix}{index}"))
        {
            index++;
        }

        return $"{prefix}{index}";
    }

    internal static void ApplyPreset(string presetName, bool allowQueue)
    {
        if (!IsGloballyEnabled)
        {
            return;
        }

        presetName = NormalizeBuiltinPresetName(presetName);
        UpdatePantheonShellBindingState();
        if (allowQueue)
        {
            ClearPendingApplies();
        }
        if (!TryGetPreset(presetName, out GearPreset preset))
        {
            return;
        }

        if (!HasAnyBindingEnabled(preset))
        {
            RestoreAllBindingsImmediate();
        }

        string? previousPreset = GodhomeQoL.GlobalSettings.GearSwitcher.LastPreset;
        if (!string.IsNullOrEmpty(previousPreset)
            && !string.Equals(NormalizeBuiltinPresetName(previousPreset!), presetName, StringComparison.OrdinalIgnoreCase))
        {
            ForgetTemporaryBuiltinCharms(previousPreset);
        }

        GodhomeQoL.GlobalSettings.GearSwitcher.LastPreset = presetName;
        GodhomeQoL.SaveGlobalSettingsSafe();

        if (!IsSafeToApply())
        {
            if (allowQueue)
            {
                QueueApplyPreset(presetName);
            }

            return;
        }

        IsApplyingPreset = true;
        try
        {
            ApplyStats(preset);
            ApplyAbilities(preset);
            ApplySpells(preset);
            ApplyNailArts(preset);
            ApplyDreamNail(preset);
            ApplyBindings(preset, forceResync: true);
            ApplyNailInput(preset);
            ApplyCharmCosts(preset);
            ApplyOvercharmed(preset);
        }
        finally
        {
            IsApplyingPreset = false;
            AlwaysFurious.NotifyGearSwitcherApplied();
            ScheduleShellBindingResync();
        }
    }

    private static bool IsBuiltinPresetName(string presetName)
    {
        foreach (string name in GearPresetDefaults.DefaultOrder())
        {
            if (string.Equals(presetName, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeBuiltinPresetName(string presetName)
    {
        if (string.Equals(presetName, "O4", StringComparison.OrdinalIgnoreCase)
            || string.Equals(presetName, "04", StringComparison.OrdinalIgnoreCase))
        {
            return "04";
        }

        if (string.Equals(presetName, "Ow", StringComparison.OrdinalIgnoreCase)
            || string.Equals(presetName, "0w", StringComparison.OrdinalIgnoreCase))
        {
            return "0w";
        }

        return presetName;
    }

    private static bool HasAnyBindingEnabled(GearPreset preset)
    {
        if (preset.HasAllBindings)
        {
            return true;
        }

        return GetPresetBool(preset.Bindings, "CharmsBinding")
            || GetPresetBool(preset.Bindings, "NailBinding")
            || GetPresetBool(preset.Bindings, "ShellBinding")
            || GetPresetBool(preset.Bindings, "SoulBinding");
    }

    private static void QueueApplyPreset(string presetName)
    {
        ClearPendingComponentApplies();
        pendingPresetName = presetName;
        pendingApply = true;
        EnsurePendingCoroutine();
    }

    private static void SyncShellBindingWithLastPreset()
    {
        if (!IsGloballyEnabled)
        {
            return;
        }

        string lastPreset = NormalizeBuiltinPresetName(GetLastPresetName());
        if (string.IsNullOrEmpty(lastPreset))
        {
            ForceRestoreShellBinding();
            return;
        }

        if (!TryGetPreset(lastPreset, out GearPreset preset))
        {
            ForceRestoreShellBinding();
            return;
        }

        bool shouldEnableShell = GetPresetShellBindingState(preset);
        if (IsPantheonSequenceActive() && pantheonShellBound)
        {
            shouldEnableShell = false;
        }

        if (shouldEnableShell)
        {
            SetBinding<ShellBinding>(true);
            return;
        }

        ForceRestoreShellBinding();
    }

    private static void EnforceBindingsWithLastPreset()
    {
        if (!IsGloballyEnabled || IsApplyingPreset || !IsSafeToApply())
        {
            return;
        }

        if (!TryGetPreset(NormalizeBuiltinPresetName(GetLastPresetName()), out GearPreset preset))
        {
            return;
        }

        bool wantsCharms = preset.HasAllBindings || GetPresetBool(preset.Bindings, "CharmsBinding");
        bool wantsNail = preset.HasAllBindings || GetPresetBool(preset.Bindings, "NailBinding");
        bool wantsSoul = preset.HasAllBindings || GetPresetBool(preset.Bindings, "SoulBinding");
        if (IsBindingApplied<CharmsBinding>() != wantsCharms
            || IsBindingApplied<NailBinding>() != wantsNail
            || IsBindingApplied<SoulBinding>() != wantsSoul)
        {
            ApplyBindings(preset);
        }
    }

    private static string GetLastPresetName() =>
        GodhomeQoL.GlobalSettings.GearSwitcher.LastPreset ?? string.Empty;

    internal static void ReapplyLastPresetStats()
    {
        if (!IsGloballyEnabled)
        {
            return;
        }

        string lastPreset = NormalizeBuiltinPresetName(GetLastPresetName());
        if (string.IsNullOrEmpty(lastPreset))
        {
            return;
        }

        if (!TryGetPreset(lastPreset, out GearPreset preset))
        {
            return;
        }

        ApplyStatsImmediate(preset);
    }

    private static bool GetPresetBool(Dictionary<string, bool> values, string key)
    {
        if (values != null && values.TryGetValue(key, out bool value))
        {
            return value;
        }

        return false;
    }

    private static bool GetPresetShellBindingState(GearPreset preset)
    {
        if (preset.HasAllBindings)
        {
            return true;
        }

        return GetPresetBool(preset.Bindings, "ShellBinding");
    }

    private static int GetPresetInt(Dictionary<string, int> values, string key)
    {
        if (values != null && values.TryGetValue(key, out int value))
        {
            return value;
        }

        return 0;
    }
}
