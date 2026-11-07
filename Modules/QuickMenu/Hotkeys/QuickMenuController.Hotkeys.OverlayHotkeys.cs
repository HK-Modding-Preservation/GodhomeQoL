using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using InControl;
using UnityEngine;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController
    {
        internal void StartQuickMenuHotkeyRebind()
        {
            waitingForQuickMenuHotkeyRebind = true;
            quickMenuPrevKey = GetQuickMenuToggleKey();
            UpdateQuickMenuHotkeyButton("Settings/FastReload/SetKey".Localize());
        }

        private void StartOverlayHotkeyRebind(string id)
        {
            waitingForOverlayHotkeyRebind = true;
            overlayHotkeyRebindId = id;
            overlayHotkeyPrevKey = GetOverlayHotkeyKey(id);
            overlayHotkeyPrevButton = GetOverlayHotkeyControllerBinding(id);
            if (quickSettingsHotkeyValues.TryGetValue(id, out Text? text))
            {
                UpdateKeybindValue(text, "Settings/FastReload/SetKey".Localize());
            }
        }

        private void CancelOverlayHotkeyRebind()
        {
            if (!waitingForOverlayHotkeyRebind)
            {
                return;
            }

            waitingForOverlayHotkeyRebind = false;
            if (quickSettingsHotkeyValues.TryGetValue(overlayHotkeyRebindId, out Text? text))
            {
                UpdateKeybindValue(text, GetOverlayHotkeyLabel(overlayHotkeyRebindId));
            }

            overlayHotkeyRebindId = string.Empty;
            overlayHotkeyPrevKey = KeyCode.None;
            overlayHotkeyPrevButton = default;
        }

        private void HandleQuickMenuHotkeyRebind()
        {
            if (!waitingForQuickMenuHotkeyRebind)
            {
                return;
            }

            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (!Input.GetKeyDown(key) || key == KeyCode.None)
                {
                    continue;
                }

                if (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6)
                {
                    continue;
                }

                ApplyQuickMenuHotkeyRebind(key);
                return;
            }
        }

        private void HandleOverlayHotkeyRebind()
        {
            if (!waitingForOverlayHotkeyRebind)
            {
                return;
            }

            if (TryGetPressedControllerButton(out InputControlType button))
            {
                ApplyOverlayHotkeyControllerRebind(button);
                return;
            }

            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (!Input.GetKeyDown(key) || key == KeyCode.None)
                {
                    continue;
                }

                if (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6)
                {
                    continue;
                }

                ApplyOverlayHotkeyRebind(key);
                return;
            }
        }

        private void ApplyOverlayHotkeyRebind(KeyCode key)
        {
            if (string.IsNullOrEmpty(overlayHotkeyRebindId))
            {
                waitingForOverlayHotkeyRebind = false;
                return;
            }

            if (key == KeyCode.Escape)
            {
                waitingForOverlayHotkeyRebind = false;
                if (quickSettingsHotkeyValues.TryGetValue(overlayHotkeyRebindId, out Text? text))
                {
                    UpdateKeybindValue(text, GetOverlayHotkeyLabel(overlayHotkeyRebindId));
                }
                overlayHotkeyRebindId = string.Empty;
                overlayHotkeyPrevKey = KeyCode.None;
                return;
            }

            bool clear = key == overlayHotkeyPrevKey;
            if (!clear && TryGetHotkeyConflictOwnersExceptSelf(key, GetOverlayHotkeyOwnerLabel(overlayHotkeyRebindId), out string conflictOwners))
            {
                ShowStatusMessage(string.Format("QuickMenu/HotkeyInUse".Localize(), FormatKeyLabel(key), conflictOwners));
                if (quickSettingsHotkeyValues.TryGetValue(overlayHotkeyRebindId, out Text? text))
                {
                    UpdateKeybindValue(text, "Settings/FastReload/SetKey".Localize());
                }
                return;
            }

            string raw = clear ? string.Empty : key.ToString();
            SetOverlayHotkeyRaw(overlayHotkeyRebindId, raw);
            GodhomeQoL.SaveGlobalSettingsSafe();
            GodhomeQoL.MarkMenuDirty();
            SetOverlayHotkeySuppress(raw, key, default);
            overlayHotkeySuppressFrames = 2;

            waitingForOverlayHotkeyRebind = false;
            if (quickSettingsHotkeyValues.TryGetValue(overlayHotkeyRebindId, out Text? valueText))
            {
                UpdateKeybindValue(valueText, GetOverlayHotkeyLabel(overlayHotkeyRebindId));
            }

            overlayHotkeyRebindId = string.Empty;
            overlayHotkeyPrevKey = KeyCode.None;
            overlayHotkeyPrevButton = default;
        }

        private void ApplyOverlayHotkeyControllerRebind(InputControlType button)
        {
            if (string.IsNullOrEmpty(overlayHotkeyRebindId))
            {
                waitingForOverlayHotkeyRebind = false;
                return;
            }

            bool clear = !EqualityComparer<InputControlType>.Default.Equals(overlayHotkeyPrevButton, default)
                && EqualityComparer<InputControlType>.Default.Equals(overlayHotkeyPrevButton, button);

            string raw = clear || IsOverlayControllerHotkeyInUse(overlayHotkeyRebindId, button)
                ? string.Empty
                : $"{OverlayControllerPrefix}{button}";

            SetOverlayHotkeyRaw(overlayHotkeyRebindId, raw);
            GodhomeQoL.SaveGlobalSettingsSafe();
            GodhomeQoL.MarkMenuDirty();
            SetOverlayHotkeySuppress(raw, KeyCode.None, button);
            overlayHotkeySuppressFrames = 2;

            waitingForOverlayHotkeyRebind = false;
            if (quickSettingsHotkeyValues.TryGetValue(overlayHotkeyRebindId, out Text? valueText))
            {
                UpdateKeybindValue(valueText, GetOverlayHotkeyLabel(overlayHotkeyRebindId));
            }

            overlayHotkeyRebindId = string.Empty;
            overlayHotkeyPrevKey = KeyCode.None;
            overlayHotkeyPrevButton = default;
        }

        private void ApplyQuickMenuHotkeyRebind(KeyCode key)
        {
            if (key == KeyCode.Escape)
            {
                waitingForQuickMenuHotkeyRebind = false;
                UpdateQuickMenuHotkeyButton(GetQuickMenuHotkeyLabel());
                return;
            }

            bool clear = key == quickMenuPrevKey;
            if (!clear && TryGetHotkeyConflictOwnersExceptSelf(key, "Settings/QuickMenu/Hotkey".Localize(), out string conflictOwners))
            {
                ShowStatusMessage(string.Format("QuickMenu/HotkeyInUse".Localize(), FormatKeyLabel(key), conflictOwners));
                UpdateQuickMenuHotkeyButton("Settings/FastReload/SetKey".Localize());
                return;
            }

            GodhomeQoL.GlobalSettings.QuickMenuHotkey = clear ? string.Empty : key.ToString();
            GodhomeQoL.SaveGlobalSettingsSafe();
            GodhomeQoL.MarkMenuDirty();
            overlayHotkeySuppressFrames = 2;

            waitingForQuickMenuHotkeyRebind = false;
            UpdateQuickMenuHotkeyButton(GetQuickMenuHotkeyLabel());
        }

        private string GetOverlayHotkeyOwnerLabel(string id)
        {
            QuickMenuItemDefinition? definition = GetOrderedQuickMenuDefinitions()
                .FirstOrDefault(def => string.Equals(def.Id, id, StringComparison.Ordinal));

            if (definition == null)
            {
                return "Overlay Hotkey";
            }

            return $"{definition.Label} Overlay Hotkey";
        }

        private void HandleOverlayHotkeys()
        {
            if (!CanUseSettingsUiHotkeys())
            {
                return;
            }

            if (IsOverlayHotkeySuppressed())
            {
                return;
            }

            if (TryUseOverlayHotkeyCache())
            {
                HandleOverlayHotkeysCached();
                return;
            }

            foreach (QuickMenuItemDefinition def in GetOrderedQuickMenuDefinitions())
            {
                if (!IsOverlayHotkeySupported(def.Id))
                {
                    continue;
                }

                InputControlType button = GetOverlayHotkeyControllerBinding(def.Id);
                if (!EqualityComparer<InputControlType>.Default.Equals(button, default) && IsControllerButtonPressed(button))
                {
                    ToggleOverlayFromHotkey(def.Id);
                    break;
                }

                KeyCode key = GetOverlayHotkeyKey(def.Id);
                if (key == KeyCode.None)
                {
                    continue;
                }

                if (Input.GetKeyDown(key))
                {
                    ToggleOverlayFromHotkey(def.Id);
                    break;
                }
            }
        }

        private bool IsOverlayHotkeyEnabled(string id)
        {
            return id switch
            {
                "FastSuperDash" => GetModuleEnabled(),
                "FastReload" => GetFastReloadEnabled(),
                "DreamshieldSettings" => GetDreamshieldEnabled(),
                "ShowHPOnDeath" => GetShowHpOnDeathEnabled(),
                "MaskDamage" => GetMaskDamageEnabled(),
                "FreezeHitboxes" => GetFreezeHitboxesEnabled(),
                "FpsBoost" => GetFpsBoostEnabled(),
                "SpeedChanger" => GetSpeedChangerEnabled(),
                "TeleportKit" => GetTeleportKitEnabled(),
                "BossChallenge" => GetBossChallengeMasterEnabled(),
                "BossManipulate" => GetCollectorPhasesEnabled() || GetZoteHelperEnabled() || GetGruzMotherHelperEnabled() || GetHornetProtectorHelperEnabled() || GetBroodingMawlekHelperEnabled() || GetMassiveMossChargerHelperEnabled() || GetCrystalGuardianHelperEnabled() || GetEnragedGuardianHelperEnabled() || GetHornetSentinelHelperEnabled() || GetAdditionalGhostHelpersEnabled() || GetGruzMotherP1HelperEnabled() || GetVengeflyKingP1HelperEnabled() || GetBroodingMawlekP1HelperEnabled() || GetNoskP2HelperEnabled() || GetUumuuP3HelperEnabled() || GetSoulWarriorP1HelperEnabled() || GetNoEyesP4HelperEnabled() || GetMarmuP2HelperEnabled() || GetXeroP2HelperEnabled() || GetMarkothP4HelperEnabled() || GetGorbP1HelperEnabled(),
                "RandomPantheons" => GetRandomPantheonsMasterEnabled(),
                "TrueBossRush" => GetTrueBossRushMasterEnabled() && GetTrueBossRushEnabled(),
                "Cheats" => GetCheatsMasterEnabled(),
                "AlwaysFurious" => GetAlwaysFuriousEnabled(),
                "GearSwitcher" => GetGearSwitcherEnabled(),
                "QualityOfLife" => GetQolMasterEnabled(),
                "BossAnimationSkipping" => GetBossAnimationMasterEnabled(),
                "MenuAnimationSkipping" => GetMenuAnimationMasterEnabled(),
                _ => false
            };
        }

        private bool IsOverlayVisible(string id)
        {
            return id switch
            {
                "FastSuperDash" => overlayVisible,
                "FastReload" => fastReloadVisible,
                "DreamshieldSettings" => dreamshieldVisible,
                "ShowHPOnDeath" => showHpOnDeathVisible,
                "MaskDamage" => maskDamageVisible,
                "FreezeHitboxes" => freezeHitboxesVisible,
                "FpsBoost" => fpsBoostVisible,
                "SpeedChanger" => speedChangerVisible,
                "TeleportKit" => teleportKitVisible,
                "BossChallenge" => bossChallengeVisible,
                "BossManipulate" => bossManipulateVisible || bossManipulateOtherRoomsVisible || gruzMotherP1HelperVisible || vengeflyKingP1HelperVisible || broodingMawlekP1HelperVisible || noskP2HelperVisible || uumuuP3HelperVisible || soulWarriorP1HelperVisible || noEyesP4HelperVisible || marmuP2HelperVisible || xeroP2HelperVisible || markothP4HelperVisible || gorbP1HelperVisible || collectorVisible || zoteHelperVisible || gruzHelperVisible || hornetHelperVisible || mawlekHelperVisible || massiveMossHelperVisible || crystalGuardianHelperVisible || enragedGuardianHelperVisible || hornetSentinelHelperVisible || IsAnyAdditionalGhostHelperVisible(),
                "RandomPantheons" => randomPantheonsVisible,
                "TrueBossRush" => trueBossRushVisible,
                "Cheats" => cheatsVisible,
                "AlwaysFurious" => alwaysFuriousVisible,
                "GearSwitcher" => gearSwitcherVisible || gearSwitcherCharmCostVisible || gearSwitcherPresetVisible,
                "QualityOfLife" => qolVisible,
                "BossAnimationSkipping" => bossAnimationVisible,
                "MenuAnimationSkipping" => menuAnimationVisible,
                _ => false
            };
        }

        private void ToggleOverlayFromHotkey(string id)
        {
            if (IsOverlayVisible(id))
            {
                CloseAllOverlaysForHotkey();
                return;
            }

            if (!IsOverlayHotkeyEnabled(id))
            {
                return;
            }

            returnToQuickOnClose = false;
            returnToBossManipulateOnClose = false;
            returnToQolOnClose = false;

            SetQuickVisible(false);
            SetQuickSettingsVisible(false);
            CloseAllOverlaysForHotkey();

            switch (id)
            {
                case "FastSuperDash":
                    SetOverlayVisible(true);
                    break;
                case "FastReload":
                    SetFastReloadVisible(true);
                    break;
                case "DreamshieldSettings":
                    SetDreamshieldVisible(true);
                    break;
                case "ShowHPOnDeath":
                    SetShowHpOnDeathVisible(true);
                    break;
                case "MaskDamage":
                    SetMaskDamageVisible(true);
                    break;
                case "FreezeHitboxes":
                    SetFreezeHitboxesVisible(true);
                    break;
                case "FpsBoost":
                    SetFpsBoostVisible(true);
                    break;
                case "SpeedChanger":
                    SetSpeedChangerVisible(true);
                    break;
                case "TeleportKit":
                    SetTeleportKitVisible(true);
                    break;
                case "BossChallenge":
                    SetBossChallengeVisible(true);
                    break;
                case "BossManipulate":
                    SetBossManipulateVisible(true);
                    break;
                case "RandomPantheons":
                    SetRandomPantheonsVisible(true);
                    break;
                case "TrueBossRush":
                    SetTrueBossRushVisible(true);
                    break;
                case "Cheats":
                    SetCheatsVisible(true);
                    break;
                case "AlwaysFurious":
                    SetAlwaysFuriousVisible(true);
                    break;
                case "GearSwitcher":
                    SetGearSwitcherVisible(true);
                    break;
                case "QualityOfLife":
                    SetQolVisible(true);
                    break;
                case "BossAnimationSkipping":
                    SetBossAnimationVisible(true);
                    break;
                case "MenuAnimationSkipping":
                    SetMenuAnimationVisible(true);
                    break;
            }
        }

        private void CloseAllOverlaysForHotkey()
        {
            returnToBossManipulateOnClose = false;
            SetOverlayVisible(false);
            SetCollectorVisible(false);
            SetFastReloadVisible(false);
            SetDreamshieldVisible(false);
            SetShowHpOnDeathVisible(false);
            SetMaskDamageVisible(false);
            SetFreezeHitboxesVisible(false);
            SetFpsBoostVisible(false);
            SetSpeedChangerVisible(false);
            SetTeleportKitVisible(false);
            SetBossChallengeVisible(false);
            SetRandomPantheonsVisible(false);
            SetTrueBossRushVisible(false);
            SetCheatsVisible(false);
            SetSettingsPresetsVisible(false);
            SetBossHistoryVisible(false);
            SetAlwaysFuriousVisible(false);
            SetGearSwitcherVisible(false);
            SetGearSwitcherCharmCostVisible(false);
            SetGearSwitcherPresetVisible(false);
            SetQolVisible(false);
            SetMenuAnimationVisible(false);
            SetBossAnimationVisible(false);
            SetZoteHelperVisible(false);
            SetGruzHelperVisible(false);
            SetHornetHelperVisible(false);
            SetMawlekHelperVisible(false);
            SetMassiveMossHelperVisible(false);
            SetCrystalGuardianHelperVisible(false);
            SetEnragedGuardianHelperVisible(false);
            SetHornetSentinelHelperVisible(false);
            SetAllAdditionalGhostHelpersVisible(false);
            SetBossManipulateVisible(false);
            SetBossManipulateOtherRoomsVisible(false);
            SetGruzMotherP1HelperVisible(false);
            SetVengeflyKingP1HelperVisible(false);
            SetBroodingMawlekP1HelperVisible(false);
            SetNoskP2HelperVisible(false);
            SetUumuuP3HelperVisible(false);
            SetSoulWarriorP1HelperVisible(false);
            SetNoEyesP4HelperVisible(false);
            SetMarmuP2HelperVisible(false);
            SetXeroP2HelperVisible(false);
            SetMarkothP4HelperVisible(false);
            SetGorbP1HelperVisible(false);
        }
    }
}
