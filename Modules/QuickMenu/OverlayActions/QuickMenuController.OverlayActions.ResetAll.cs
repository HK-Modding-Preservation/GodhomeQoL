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
        private void OnQuickSettingsResetDefaultsClicked()
        {
            SetQuickSettingsResetConfirmVisible(true);
        }

        private void OnQuickSettingsResetConfirmYes()
        {
            ResetAllModSettingsToDefaults();
            SetQuickSettingsResetConfirmVisible(false);
        }

        private void OnQuickSettingsResetConfirmNo()
        {
            SetQuickSettingsResetConfirmVisible(false);
        }

        private void ResetAllModSettingsToDefaults()
        {
            quickRenameMode = false;
            if (renameField != null)
            {
                UObject.Destroy(renameField.gameObject);
                renameField = null;
            }

            if (gearSwitcherPresetRenameField != null)
            {
                UObject.Destroy(gearSwitcherPresetRenameField.gameObject);
            }
            gearSwitcherPresetRenameField = null;
            gearSwitcherPresetRenameLabel = null;
            gearSwitcherPresetRenameOriginalLabel = null;
            gearSwitcherPresetRenameTargetName = null;

            SetGearSwitcherPresetDeleteVisible(false);
            SetGearSwitcherResetConfirmVisible(false);
            SetGearSwitcherPresetVisible(false);
            SetGearSwitcherCharmCostVisible(false);
            SetGearSwitcherVisible(false);
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
            SetGruzHelperVisible(false);
            SetHornetHelperVisible(false);
            SetMawlekHelperVisible(false);
            SetMassiveMossHelperVisible(false);
            SetCrystalGuardianHelperVisible(false);
            SetEnragedGuardianHelperVisible(false);
            SetHornetSentinelHelperVisible(false);
            SetAllAdditionalGhostHelpersVisible(false);
            SetCheatsVisible(false);
            SetSettingsPresetsVisible(false);
            SetBossHistoryVisible(false);
            SetTrueBossRushVisible(false);

            ApplyInitialDefaults();
            GearSwitcher.ResetDefaults();

            GodhomeQoL.GlobalSettings.QuickMenuOrder = new List<string>();
            GodhomeQoL.GlobalSettings.QuickMenuPositions = new Dictionary<string, QuickMenuEntryPosition>();
            GodhomeQoL.GlobalSettings.QuickMenuCustomLabels = new Dictionary<string, string>();
            GodhomeQoL.GlobalSettings.QuickMenuVisibility = new Dictionary<string, bool>();
            GodhomeQoL.GlobalSettings.QuickMenuFreeLayout = false;
            GodhomeQoL.GlobalSettings.QuickMenuHotkey = DefaultQuickMenuHotkey;
            GodhomeQoL.GlobalSettings.QuickMenuControllerButton = DefaultQuickMenuControllerButton;
            GodhomeQoL.GlobalSettings.TeleportKitControllerButton = DefaultTeleportKitControllerButton;
            GodhomeQoL.GlobalSettings.QuickMenuOverlayHotkeys = new Dictionary<string, string>();

            GodhomeQoL.SaveGlobalSettingsSafe();

            LoadMasterSettings();

            RebuildQuickMenuEntries();
            ApplyQuickMenuLayout();
            ApplyQuickMenuOpacity();
            UpdateFreeMenuLabel();
            UpdateRenameModeLabel();
            UpdateQuickMenuEntryStateColors();
            RefreshQuickMenuSettingsUi();

            RebuildGearSwitcherPresetOverlay();
            SetGearSwitcherPresetVisible(false);

            RefreshModulePanelsUi();
        }

        private void RefreshModulePanelsUi()
        {
            RefreshFastSuperDashUi();
            RefreshCollectorPhasesUi();
            RefreshFastReloadUi();
            RefreshDreamshieldUi();
            RefreshShowHpOnDeathUi();
            RefreshMaskDamageUi();
            RefreshFreezeHitboxesUi();
            RefreshFpsBoostUi();
            RefreshSpeedChangerUi();
            RefreshTeleportKitUi();
            RefreshBossChallengeUi();
            RefreshRandomPantheonsUi();
            RefreshTrueBossRushUi();
            RefreshCheatsUi();
            RefreshAlwaysFuriousUi();
            RefreshGearSwitcherUi();
            RefreshQolUi();
            RefreshMenuAnimationUi();
            RefreshBossAnimationUi();
            RefreshZoteHelperUi();
            RefreshGruzHelperUi();
            RefreshHornetHelperUi();
            RefreshMawlekHelperUi();
            RefreshMassiveMossHelperUi();
            RefreshCrystalGuardianHelperUi();
            RefreshEnragedGuardianHelperUi();
            RefreshHornetSentinelHelperUi();
            RefreshAdditionalGhostHelpersUi();
        }

        private void OnQuickMenuSettingsBackClicked()
        {
            bool reopenQuick = returnToQuickOnClose;
            returnToQuickOnClose = false;

            if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            SetQuickSettingsVisible(false);
            UpdateQuickMenuEntryStateColors();
        }
    }
}
