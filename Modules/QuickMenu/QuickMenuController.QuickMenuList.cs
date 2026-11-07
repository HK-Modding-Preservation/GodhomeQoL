using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController
    {
        private List<QuickMenuItemDefinition> GetQuickMenuDefinitions()
        {
            return new List<QuickMenuItemDefinition>
            {
                new("FastSuperDash", "Modules/FastSuperDash".Localize(), OnQuickFastSuperDashClicked),
                new("FastReload", "Modules/FastReload".Localize(), OnQuickFastReloadClicked),
                new("DreamshieldSettings", "DreamshieldSettings".Localize(), OnQuickDreamshieldClicked),
                new("ShowHPOnDeath", "ShowHPOnDeath".Localize(), OnQuickShowHpOnDeathClicked),
                new("MaskDamage", "Modules/MaskDamage".Localize(), OnQuickMaskDamageClicked),
                new("FreezeHitboxes", "Freeze Hitboxes", OnQuickFreezeHitboxesClicked),
                new("SpeedChanger", "SpeedChanger".Localize(), OnQuickSpeedChangerClicked),
                new("TeleportKit", "TeleportKit".Localize(), OnQuickTeleportKitClicked),
                new("BossChallenge", "Categories/BossChallenge".Localize(), OnQuickBossChallengeClicked),
                new("BossManipulate", "Categories/BossManipulate".Localize(), OnQuickBossManipulateClicked),
                new("RandomPantheons", "Modules/RandomPantheons".Localize(), OnQuickRandomPantheonsClicked),
                new("TrueBossRush", "Modules/TrueBossRush".Localize(), OnQuickTrueBossRushClicked),
                new("Cheats", "Modules/Cheats".Localize(), OnQuickCheatsClicked),
                new("AlwaysFurious", "Modules/AlwaysFurious".Localize(), OnQuickAlwaysFuriousClicked),
                new("GearSwitcher", "Modules/GearSwitcher".Localize(), OnQuickGearSwitcherClicked),
                new("FpsBoost", "Modules/FpsBoost".Localize(), OnQuickFpsBoostClicked),
                new("QualityOfLife", "Categories/QoL".Localize(), OnQuickQolClicked),
                new("BossAnimationSkipping", "Categories/BossAnimationSkipping".Localize(), OnQuickBossAnimationClicked),
                new("MenuAnimationSkipping", "Categories/MenuAnimationSkipping".Localize(), OnQuickMenuAnimationClicked),
                new("FreeMenu", GetFreeMenuLabel(), OnQuickFreeMenuClicked),
                new("RenameMode", GetRenameModeLabel(), OnQuickRenameModeClicked),
                new("SettingsPresets", "QuickMenu/Presets".Localize(), OnQuickSettingsPresetsClicked),
                new("Settings", "QuickMenu/Settings".Localize(), OnQuickSettingsClicked)
            };
        }

        private List<QuickMenuItemDefinition> GetOrderedQuickMenuDefinitions()
        {
            List<QuickMenuItemDefinition> definitions = GetQuickMenuDefinitions();
            List<string> savedOrder = GodhomeQoL.GlobalSettings.QuickMenuOrder ?? new List<string>();
            List<QuickMenuItemDefinition> ordered = new();
            HashSet<string> used = new();
            Dictionary<string, QuickMenuItemDefinition> lookup = definitions.ToDictionary(def => def.Id);

            foreach (string id in savedOrder)
            {
                if (used.Add(id) && lookup.TryGetValue(id, out QuickMenuItemDefinition? def))
                {
                    ordered.Add(def);
                }
            }

            foreach (QuickMenuItemDefinition def in definitions)
            {
                if (used.Add(def.Id))
                {
                    ordered.Add(def);
                }
            }

            return ordered;
        }

        private List<QuickMenuItemDefinition> GetVisibleQuickMenuDefinitions()
        {
            List<QuickMenuItemDefinition> ordered = GetOrderedQuickMenuDefinitions();
            return ordered
                .Where(def => IsQuickMenuItemVisible(def.Id))
                .ToList();
        }

        private bool IsQuickMenuItemVisible(string id)
        {
            if (string.Equals(id, "Settings", StringComparison.Ordinal)
                || string.Equals(id, "FreeMenu", StringComparison.Ordinal)
                || string.Equals(id, "RenameMode", StringComparison.Ordinal))
            {
                return true;
            }

            Dictionary<string, bool> visibility = GodhomeQoL.GlobalSettings.QuickMenuVisibility ??= new Dictionary<string, bool>();
            return !visibility.TryGetValue(id, out bool isVisible) || isVisible;
        }

        private void SetQuickMenuItemVisible(string id, bool value)
        {
            if (string.Equals(id, "Settings", StringComparison.Ordinal)
                || string.Equals(id, "FreeMenu", StringComparison.Ordinal)
                || string.Equals(id, "RenameMode", StringComparison.Ordinal))
            {
                return;
            }

            Dictionary<string, bool> visibility = GodhomeQoL.GlobalSettings.QuickMenuVisibility ??= new Dictionary<string, bool>();
            visibility[id] = value;
            GodhomeQoL.SaveGlobalSettingsSafe();
            ApplyQuickMenuVisibilityToModule(id, value);
        }

        private void ApplyQuickMenuVisibilityToModule(string id, bool value)
        {
            switch (id)
            {
                case "FastSuperDash":
                    SetModuleEnabled(false);
                    break;
                case "FastReload":
                    SetFastReloadEnabled(false);
                    break;
                case "DreamshieldSettings":
                    SetDreamshieldEnabled(false);
                    break;
                case "SpeedChanger":
                    SetSpeedChangerGlobalSwitch(false);
                    break;
                case "TeleportKit":
                    SetTeleportKitEnabled(false);
                    break;
                case "ShowHPOnDeath":
                    SetShowHpOnDeathEnabled(false);
                    break;
                case "MaskDamage":
                    SetMaskDamageEnabled(false);
                    break;
                case "FreezeHitboxes":
                    SetFreezeHitboxesEnabled(false);
                    break;
                case "AlwaysFurious":
                    SetAlwaysFuriousEnabled(false);
                    break;
                case "GearSwitcher":
                    SetGearSwitcherEnabled(false);
                    break;
                case "FpsBoost":
                    SetFpsBoostEnabled(false);
                    break;
                case "BossChallenge":
                    SetBossChallengeMasterEnabled(false);
                    break;
                case "BossManipulate":
                    SetCollectorPhasesEnabled(false);
                    SetZoteHelperEnabled(false);
                    SetGruzMotherHelperEnabled(false);
                    SetHornetProtectorHelperEnabled(false);
                    SetBroodingMawlekHelperEnabled(false);
                    SetMassiveMossChargerHelperEnabled(false);
                    SetCrystalGuardianHelperEnabled(false);
                    SetEnragedGuardianHelperEnabled(false);
                    SetHornetSentinelHelperEnabled(false);
                    SetAdditionalGhostHelpersEnabled(false);
                    break;
                case "RandomPantheons":
                    SetRandomPantheonsMasterEnabled(false);
                    break;
                case "TrueBossRush":
                    SetTrueBossRushMasterEnabled(false);
                    break;
                case "Cheats":
                    SetCheatsMasterEnabled(false);
                    break;
                case "QualityOfLife":
                    SetQolMasterEnabled(false);
                    break;
                case "MenuAnimationSkipping":
                    SetMenuAnimationMasterEnabled(false);
                    break;
                case "BossAnimationSkipping":
                    SetBossAnimationMasterEnabled(false);
                    break;
            }
        }

        private string GetQuickMenuSettingsLabel(QuickMenuItemDefinition def)
        {
            if (string.Equals(def.Id, "FreeMenu", StringComparison.Ordinal))
            {
                return "QuickMenu/FreeMenu".Localize();
            }

            if (string.Equals(def.Id, "RenameMode", StringComparison.Ordinal))
            {
                return "QuickMenu/RenameMode".Localize();
            }

            string defaultLabel = def.Label;
            if (TryGetQuickMenuCustomLabel(def.Id, out string custom)
                && !string.Equals(custom, defaultLabel, StringComparison.Ordinal))
            {
                return $"{custom} ({defaultLabel})";
            }

            return defaultLabel;
        }

        private string GetFreeMenuLabel()
        {
            string label = "QuickMenu/FreeMenu".Localize();
            string state = IsQuickMenuFreeLayoutEnabled() ? "ON" : "OFF";
            return $"{label}: {state}";
        }

    }
}
