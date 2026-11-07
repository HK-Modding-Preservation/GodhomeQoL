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
        private void OnFastReloadBackClicked()
        {
            bool reopenQuick = returnToQuickOnClose;
            returnToQuickOnClose = false;

            if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            SetFastReloadVisible(false);
        }

        private void OnFastReloadResetDefaultsClicked()
        {
            SetFastReloadEnabled(false);
            waitingForReloadRebind = false;
            Modules.FastReload.reloadKeyCode = (int)KeyCode.None;
            RefreshFastReloadUi();
        }

        private void OnDreamshieldBackClicked()
        {
            bool reopenQuick = returnToQuickOnClose;
            returnToQuickOnClose = false;

            if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            SetDreamshieldVisible(false);
        }

        private void OnDreamshieldResetDefaultsClicked()
        {
            Modules.QoL.DreamshieldStartAngle.ResetDefaults();
            RefreshDreamshieldUi();
            UpdateQuickMenuEntryStateColors();
        }

        private void OnShowHpOnDeathBackClicked()
        {
            bool reopenQuick = returnToQuickOnClose;
            returnToQuickOnClose = false;

            if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            SetShowHpOnDeathVisible(false);
        }

        private void OnMaskDamageBackClicked()
        {
            bool reopenQuick = returnToQuickOnClose;
            returnToQuickOnClose = false;

            if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            SetMaskDamageVisible(false);
            SetFreezeHitboxesVisible(false);
        }

        private void OnMaskDamageResetDefaultsClicked()
        {
            waitingForMaskDamageUiRebind = false;
            maskDamageUiPrevKey = string.Empty;

            SetMaskDamageEnabled(false);
            MaskDamage.SetMultiplier(1f);
            MaskDamage.SetUiVisible(true);
            MaskDamage.SetToggleUiKeybind(string.Empty);
            RefreshMaskDamageUi();
            UpdateQuickMenuEntryStateColors();
        }

        private void OnFreezeHitboxesBackClicked()
        {
            bool reopenQuick = returnToQuickOnClose;
            returnToQuickOnClose = false;

            if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            SetFreezeHitboxesVisible(false);
        }

        private void OnFreezeHitboxesResetDefaultsClicked()
        {
            waitingForFreezeHitboxesRebind = false;
            freezeHitboxesPrevKey = string.Empty;

            SetFreezeHitboxesEnabled(false);
            FreezeHitboxes.SetAnyHitsMode(false);
            FreezeHitboxes.SetUnfreezeKeybind(string.Empty);
            RefreshFreezeHitboxesUi();
            UpdateQuickMenuEntryStateColors();
        }

        private void OnShowHpOnDeathResetDefaultsClicked()
        {
            waitingForShowHpRebind = false;
            showHpPrevBindingRaw = string.Empty;
            ShowHPOnDeath.ResetFeatureDefaults();
            ApplyShowHpBinding(ShowHpSettings.Keybinds.Hide, null);
            ApplyShowHpBinding(ShowHpSettings.Keybinds.History, null);
            RefreshShowHpOnDeathUi();
            UpdateQuickMenuEntryStateColors();
        }

        private void OnSpeedChangerBackClicked()
        {
            bool reopenQuick = returnToQuickOnClose;
            returnToQuickOnClose = false;

            if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            SetSpeedChangerVisible(false);
        }

        private void OnSpeedChangerResetDefaultsClicked()
        {
            waitingForSpeedToggleRebind = false;
            waitingForSpeedInputRebind = false;
            speedTogglePrevKey = string.Empty;
            speedInputPrevKey = string.Empty;

            SetSpeedChangerGlobalSwitch(false);
            SpeedChanger.restrictToggleToRooms = false;
            SpeedChanger.unlimitedSpeed = false;
            SpeedChanger.displayStyle = 0;
            SpeedChanger.toggleKeybind = string.Empty;
            SpeedChanger.inputSpeedKeybind = string.Empty;
            SpeedChanger.speed = 1f;
            ApplySpeedChangerDisplayStyle(0);
            RefreshSpeedChangerKeybinds();
            RefreshSpeedChangerUi();
        }

        private void OnTeleportKitBackClicked()
        {
            bool reopenQuick = returnToQuickOnClose;
            returnToQuickOnClose = false;

            if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            SetTeleportKitVisible(false);
        }

        private void OnTeleportKitResetDefaultsClicked()
        {
            waitingForTeleportKitMenuRebind = false;
            waitingForTeleportKitSaveRebind = false;
            waitingForTeleportKitTeleportRebind = false;
            SetTeleportKitEnabled(false);
            Modules.QoL.TeleportKit.MenuHotkey = KeyCode.F6;
            Modules.QoL.TeleportKit.SaveTeleportHotkey = KeyCode.R;
            Modules.QoL.TeleportKit.TeleportHotkey = KeyCode.T;
            GodhomeQoL.GlobalSettings.TeleportKitControllerButton = DefaultTeleportKitControllerButton;
            UpdateKeybindValue(teleportKitControllerButtonValue, GetTeleportKitControllerButtonLabel());
            RefreshTeleportKitUi();
        }

        private void OnCheatsBackClicked()
        {
            bool reopenQuick = returnToQuickOnClose;
            returnToQuickOnClose = false;

            if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            SetCheatsVisible(false);
        }

        private void OnCheatsKillAllClicked()
        {
            _ = Modules.Cheats.Cheats.KillAll();
        }

        private void OnCheatsResetDefaultsClicked()
        {
            cheatsMasterEnabled = false;
            cheatsMasterHasSnapshot = false;
            cheatsSavedInfiniteSoul = false;
            cheatsSavedInfiniteHp = false;
            cheatsSavedInvincibility = false;
            cheatsSavedNoclip = false;
            waitingForCheatsKillAllRebind = false;
            cheatsKillAllPrevKey = string.Empty;

            Modules.Cheats.Cheats.SetInfiniteSoulEnabled(false);
            Modules.Cheats.Cheats.SetInfiniteHpEnabled(false);
            Modules.Cheats.Cheats.SetInvincibilityEnabled(false);
            Modules.Cheats.Cheats.SetNoclipEnabled(false);
            Modules.Cheats.Cheats.SetKillAllHotkeyRaw(string.Empty);
            SetCheatsEnabled(false);
            RefreshCheatsUi();
            SaveMasterSettings();
        }

        private void OnAlwaysFuriousBackClicked()
        {
            bool reopenQuick = returnToQuickOnClose;
            returnToQuickOnClose = false;

            if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            SetAlwaysFuriousVisible(false);
        }

        private void OnAlwaysFuriousResetDefaultsClicked()
        {
            SetAlwaysFuriousEnabled(false);
            RefreshAlwaysFuriousUi();
        }

        private void OnFastSuperDashResetDefaultsClicked()
        {
            SetModuleEnabled(false);
            Modules.QoL.FastSuperDash.instantSuperDash = false;
            Modules.QoL.FastSuperDash.fastSuperDashEverywhere = false;
            Modules.QoL.FastSuperDash.fastSuperDashSpeedMultiplier = 1f;
            fastSuperDashSpeedDirty = false;
            GodhomeQoL.SaveGlobalSettingsSafe();
            RefreshFastSuperDashUi();
        }
    }
}
