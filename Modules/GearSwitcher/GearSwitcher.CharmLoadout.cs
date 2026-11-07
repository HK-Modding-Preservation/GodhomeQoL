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
    internal enum CharmSelectResult
    {
        Ok,
        BuiltinGear,
        CharmsBound,
        NoNotches,
        Unknown
    }

    private const int FirstCharmId = 1;
    private const int LastCharmId = 40;

    private static readonly List<int> managedCharmsApplied = new();

    private static readonly Dictionary<string, int> CharmIdsByKey = new(StringComparer.Ordinal)
    {
        ["GatheringSwarm"] = 1,
        ["WaywardCompass"] = 2,
        ["Grubsong"] = 3,
        ["StalwartShell"] = 4,
        ["BaldurShell"] = 5,
        ["FuryOfTheFallen"] = 6,
        ["QuickFocus"] = 7,
        ["LifebloodHeart"] = 8,
        ["LifebloodCore"] = 9,
        ["DefendersCrest"] = 10,
        ["Flukenest"] = 11,
        ["ThornsOfAgony"] = 12,
        ["MarkOfPride"] = 13,
        ["SteadyBody"] = 14,
        ["HeavyBlow"] = 15,
        ["SharpShadow"] = 16,
        ["SporeShroom"] = 17,
        ["Longnail"] = 18,
        ["ShamanStone"] = 19,
        ["SoulCatcher"] = 20,
        ["SoulEater"] = 21,
        ["GlowingWomb"] = 22,
        ["UnbreakableHeart"] = 23,
        ["UnbreakableGreed"] = 24,
        ["UnbreakableStrength"] = 25,
        ["NailmastersGlory"] = 26,
        ["JonisBlessing"] = 27,
        ["ShapeOfUnn"] = 28,
        ["Hiveblood"] = 29,
        ["DreamWielder"] = 30,
        ["Dashmaster"] = 31,
        ["QuickSlash"] = 32,
        ["SpellTwister"] = 33,
        ["DeepFocus"] = 34,
        ["GrubberflysElegy"] = 35,
        ["VoidHeart"] = 36,
        ["Sprintmaster"] = 37,
        ["Dreamshield"] = 38,
        ["Weaversong"] = 39,
        ["CarefreeMelody"] = 40
    };

    internal static bool TryGetCharmId(string key, out int charmId) => CharmIdsByKey.TryGetValue(key, out charmId);

    internal static bool IsCharmSelected(GearPreset preset, int charmId) =>
        preset.EquippedCharms != null && preset.EquippedCharms.Contains(charmId);

    internal static bool IsCharmSelectionLocked(GearPreset preset) =>
        preset.HasAllBindings || GetPresetBool(preset.Bindings, "CharmsBinding");

    internal static int GetPlannedNotchesUsed(GearPreset preset, int excludedCharmId = 0)
    {
        int total = 0;
        if (preset.EquippedCharms != null)
        {
            foreach (int charmId in preset.EquippedCharms)
            {
                if (charmId != excludedCharmId)
                {
                    total += GetPresetCharmCost(preset, charmId);
                }
            }
        }

        return total;
    }

    internal static int GetPresetNotchSlots(GearPreset preset) => GetPresetCharmSlots(preset);

    internal static bool CanPutOnCharm(int usedNotches, int cost, int slots) =>
        usedNotches + cost <= slots || usedNotches < slots;

    internal static CharmSelectResult TrySetCharmEquipped(string presetName, GearPreset preset, int charmId, bool equip)
    {
        if (charmId < FirstCharmId || charmId > LastCharmId)
        {
            return CharmSelectResult.Unknown;
        }

        if (IsCharmSelectionLocked(preset))
        {
            return CharmSelectResult.CharmsBound;
        }

        List<int> list = preset.EquippedCharms ??= new List<int>();
        if (equip)
        {
            if (list.Contains(charmId))
            {
                return CharmSelectResult.Ok;
            }

            if (!CanPutOnCharm(GetPlannedNotchesUsed(preset), GetPresetCharmCost(preset, charmId), GetPresetNotchSlots(preset)))
            {
                return CharmSelectResult.NoNotches;
            }

            list.Add(charmId);
        }
        else
        {
            list.Remove(charmId);
        }

        if (IsBuiltinPresetName(presetName))
        {
            GodhomeQoL.LocalSettings.GearSwitcherTemporaryCharms = list.ToList();
        }

        GodhomeQoL.SaveGlobalSettingsSafe();
        ApplyCharmCostsImmediate(preset);
        return CharmSelectResult.Ok;
    }

    internal static bool CanSwapCharmVariant(GearPreset preset, int charmId)
    {
        if (!IsCharmSelected(preset, charmId))
        {
            return true;
        }

        int currentCost = GetPresetCharmCost(preset, charmId);
        int newCost;
        if (charmId == VoidHeartCharmId)
        {
            newCost = preset.UseVoidHeart
                ? (preset.KingsoulCostInitialized ? preset.KingsoulCost : 5)
                : preset.VoidHeartCost;
        }
        else
        {
            newCost = preset.UseGrimmchild
                ? (preset.CarefreeMelodyCostInitialized ? preset.CarefreeMelodyCost : 3)
                : (preset.GrimmchildCost < 0 ? 2 : preset.GrimmchildCost);
        }

        newCost = Math.Max(0, Math.Min(99, newCost));
        return newCost <= currentCost || CanPutOnCharm(GetPlannedNotchesUsed(preset, charmId), newCost, GetPresetNotchSlots(preset));
    }

    private static int GetPresetCharmCost(GearPreset preset, int charmId)
    {
        int cost = charmId switch
        {
            1 => preset.GatheringSwarmCost,
            2 => preset.WaywardCompassCost,
            3 => preset.GrubsongCost,
            4 => preset.StalwartShellCost,
            5 => preset.BaldurShellCost,
            6 => preset.FuryOfTheFallenCost,
            7 => preset.QuickFocusCost,
            8 => preset.LifebloodHeartCost,
            9 => preset.LifebloodCoreCost,
            10 => preset.DefendersCrestCost,
            11 => preset.FlukenestCost,
            12 => preset.ThornsOfAgonyCost,
            13 => preset.MarkOfPrideCost,
            14 => preset.SteadyBodyCost,
            15 => preset.HeavyBlowCost,
            16 => preset.SharpShadowCost,
            17 => preset.SporeShroomCost,
            18 => preset.LongnailCost,
            19 => preset.ShamanStoneCost,
            20 => preset.SoulCatcherCost,
            21 => preset.SoulEaterCost,
            22 => preset.GlowingWombCost,
            23 => preset.UnbreakableHeartCost,
            24 => preset.UnbreakableGreedCost,
            25 => preset.UnbreakableStrengthCost,
            26 => preset.NailmastersGloryCost,
            27 => preset.JonisBlessingCost,
            28 => preset.ShapeOfUnnCost,
            29 => preset.HivebloodCost,
            30 => preset.DreamWielderCost,
            31 => preset.DashmasterCost,
            32 => preset.QuickSlashCost,
            33 => preset.SpellTwisterCost,
            34 => preset.DeepFocusCost,
            35 => preset.GrubberflysElegyCost,
            36 => preset.UseVoidHeart ? preset.VoidHeartCost : preset.KingsoulCost,
            37 => preset.SprintmasterCost,
            38 => preset.DreamshieldCost,
            39 => preset.WeaversongCost,
            40 => preset.UseGrimmchild ? preset.GrimmchildCost : preset.CarefreeMelodyCost,
            _ => 0
        };

        return Math.Max(0, Math.Min(99, cost));
    }

    private static void ApplyCharmLoadout(GearPreset preset)
    {
        PlayerData? pd = PlayerData.instance;
        if (pd == null)
        {
            return;
        }

        List<int>? desired = preset.EquippedCharms;
        if (desired == null)
        {
            AdoptSavedTemporaryCharms();
            ReleaseManagedCharms(pd, preset);
            return;
        }

        if (IsCharmSelectionLocked(preset) || IsBindingApplied<CharmsBinding>())
        {
            return;
        }

        bool changed = false;
        bool grimmchildPutOn = false;
        foreach (int charmId in GetEquippedCharms())
        {
            if (charmId >= FirstCharmId && charmId <= LastCharmId && !desired.Contains(charmId))
            {
                UnequipCharmOnPlayer(pd, charmId);
                changed = true;
            }
        }

        foreach (int charmId in desired)
        {
            if (charmId < FirstCharmId || charmId > LastCharmId || pd.GetBoolInternal($"equippedCharm_{charmId}"))
            {
                continue;
            }

            pd.CalculateNotchesUsed();
            if (!CanPutOnCharm(pd.charmSlotsFilled, pd.GetInt($"charmCost_{charmId}"), pd.charmSlots))
            {
                continue;
            }

            EquipCharmOnPlayer(pd, charmId);
            changed = true;
            grimmchildPutOn |= charmId == GrimmchildCharmId && preset.UseGrimmchild;
        }

        managedCharmsApplied.Clear();
        managedCharmsApplied.AddRange(desired);
        if (changed)
        {
            FinishCharmChange(pd, preset, grimmchildPutOn);
        }
    }

    internal static void ForgetTemporaryBuiltinCharms(string? presetName)
    {
        if (string.IsNullOrEmpty(presetName) || !IsBuiltinPresetName(presetName!))
        {
            return;
        }

        if (TryGetPreset(NormalizeBuiltinPresetName(presetName!), out GearPreset preset))
        {
            preset.EquippedCharms = null;
        }

        GodhomeQoL.LocalSettings.GearSwitcherTemporaryCharms = null;
    }

    private static void AdoptSavedTemporaryCharms()
    {
        List<int>? saved = GodhomeQoL.LocalSettings.GearSwitcherTemporaryCharms;
        if (saved == null || saved.Count == 0)
        {
            return;
        }

        foreach (int charmId in saved)
        {
            if (charmId >= FirstCharmId && charmId <= LastCharmId && !managedCharmsApplied.Contains(charmId))
            {
                managedCharmsApplied.Add(charmId);
            }
        }

        GodhomeQoL.LocalSettings.GearSwitcherTemporaryCharms = null;
    }

    private static void ReleaseManagedCharms(PlayerData pd, GearPreset preset)
    {
        if (managedCharmsApplied.Count == 0)
        {
            return;
        }

        bool changed = false;
        foreach (int charmId in managedCharmsApplied.ToList())
        {
            if (pd.GetBoolInternal($"equippedCharm_{charmId}"))
            {
                UnequipCharmOnPlayer(pd, charmId);
                changed = true;
            }
        }

        managedCharmsApplied.Clear();
        if (changed)
        {
            FinishCharmChange(pd, preset, grimmchildPutOn: false);
        }
    }

    private static void EquipCharmOnPlayer(PlayerData pd, int charmId)
    {
        pd.SetBoolInternal($"gotCharm_{charmId}", true);
        switch (charmId)
        {
            case 23:
                pd.SetBoolInternal("brokenCharm_23", false);
                pd.SetBoolInternal("fragileHealth_unbreakable", true);
                break;
            case 24:
                pd.SetBoolInternal("brokenCharm_24", false);
                pd.SetBoolInternal("fragileGreed_unbreakable", true);
                break;
            case 25:
                pd.SetBoolInternal("brokenCharm_25", false);
                pd.SetBoolInternal("fragileStrength_unbreakable", true);
                break;
            case GrimmchildCharmId:
                EnsureGrimmchildUnlocked(pd);
                break;
        }

        pd.SetBoolInternal($"equippedCharm_{charmId}", true);
        if (!GetEquippedCharms().Contains(charmId))
        {
            pd.EquipCharm(charmId);
        }
    }

    private static void UnequipCharmOnPlayer(PlayerData pd, int charmId)
    {
        pd.SetBoolInternal($"equippedCharm_{charmId}", false);
        pd.UnequipCharm(charmId);
        if (charmId == GrimmchildCharmId)
        {
            RemoveGrimmchildCompanion();
        }
    }

    private static void FinishCharmChange(PlayerData pd, GearPreset preset, bool grimmchildPutOn)
    {
        pd.CalculateNotchesUsed();
        ApplyOvercharmed(preset);
        HeroController.instance?.CharmUpdate();
        if (grimmchildPutOn)
        {
            RefreshGrimmchildCharm(pd);
        }

        PlayMakerFSM.BroadcastEvent("CHARM EQUIP CHECK");
        PlayMakerFSM.BroadcastEvent(CharmIndicatorCheckEvent);
        PlayMakerFSM.BroadcastEvent(UpdateBlueHealthEvent);
    }

    private static void EnforceCharmLoadoutWithLastPreset()
    {
        if (!IsGloballyEnabled || IsApplyingPreset || !IsSafeToApply())
        {
            return;
        }

        if (TryGetPreset(NormalizeBuiltinPresetName(GetLastPresetName()), out GearPreset preset))
        {
            ApplyCharmLoadout(preset);
        }
    }
}
