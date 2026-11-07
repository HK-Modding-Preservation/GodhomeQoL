using UnityEngine;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController : MonoBehaviour
    {
        internal bool IsAnyUiVisible()
        {
            return quickVisible
                || quickSettingsVisible
                || overlayVisible
                || collectorVisible
                || fastReloadVisible
                || dreamshieldVisible
                || showHpOnDeathVisible
                || maskDamageVisible
                || freezeHitboxesVisible
                || fpsBoostVisible
                || speedChangerVisible
                || teleportKitVisible
                || bossChallengeVisible
                || randomPantheonsVisible
                || trueBossRushVisible
                || cheatsVisible
                || alwaysFuriousVisible
                || gearSwitcherVisible
                || gearSwitcherCharmCostVisible
                || gearSwitcherPresetVisible
                || settingsPresetsVisible
                || bossHistoryVisible
                || qolVisible
                || menuAnimationVisible
                || bossAnimationVisible
                || zoteHelperVisible
                || gruzHelperVisible
                || hornetHelperVisible
                || mawlekHelperVisible
                || massiveMossHelperVisible
                || crystalGuardianHelperVisible
                || enragedGuardianHelperVisible
                || hornetSentinelHelperVisible
                || IsAnyAdditionalGhostHelperVisible()
                || bossManipulateOtherRoomsVisible
                || gruzMotherP1HelperVisible
                || vengeflyKingP1HelperVisible
                || broodingMawlekP1HelperVisible
                || noskP2HelperVisible
                || uumuuP3HelperVisible
                || soulWarriorP1HelperVisible
                || noEyesP4HelperVisible
                || marmuP2HelperVisible
                || xeroP2HelperVisible
                || markothP4HelperVisible
                || gorbP1HelperVisible
                || bossManipulateVisible;
        }

        private void ToggleMenu()
        {
            if (IsAnyUiVisible())
            {
                returnToQuickOnClose = false;
                returnToBossManipulateOnClose = false;
                returnToQolOnClose = false;
                SetQuickVisible(false);
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
                SetQuickSettingsVisible(false);
                return;
            }

            if (!CanUseSettingsUiHotkeys())
            {
                return;
            }

            SetQuickVisible(true);
        }

        private void SetQuickVisible(bool value, bool instant = false)
        {
            quickVisible = value;
            if (quickRoot == null)
            {
                UpdateUiState();
                return;
            }

            if (!value)
            {
                CancelQuickMenuDrag();
                CancelQuickMenuRename();

                if (quickPanelGroup != null)
                {
                    quickPanelGroup.interactable = false;
                    quickPanelGroup.blocksRaycasts = false;
                }

                StopQuickMenuFade();
                quickMenuFadeAlpha = 0f;
                UpdateQuickMenuAlpha();
                quickRoot.SetActive(false);
            }
            else
            {
                quickRoot.SetActive(true);
                ApplyQuickMenuLayout();
                UpdateFreeMenuLabel();
                UpdateRenameModeLabel();
                UpdateQuickMenuEntryStateColors();

                if (quickPanelGroup != null)
                {
                    quickPanelGroup.interactable = true;
                    quickPanelGroup.blocksRaycasts = true;
                }

                StopQuickMenuFade();
                quickMenuFadeAlpha = 1f;
                UpdateQuickMenuAlpha();
            }

            UpdateUiState();
        }

        private void SetQuickSettingsVisible(bool value)
        {
            quickSettingsVisible = value;
            if (quickSettingsRoot != null)
            {
                quickSettingsRoot.SetActive(value);
            }

            if (value)
            {
                RefreshQuickMenuSettingsUi();
                SetQuickSettingsResetConfirmVisible(false);
            }
            else
            {
                SetQuickSettingsResetConfirmVisible(false);
                CancelOverlayHotkeyRebind();
            }

            UpdateUiState();
        }

        private void SetOverlayVisible(bool value)
        {
            if (!value)
            {
                FlushFastSuperDashSpeedSaveIfDirty();
            }

            overlayVisible = value;
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(value);
            }

            if (value)
            {
                RefreshFastSuperDashUi();
            }

            UpdateUiState();
        }

        private void UpdateUiState()
        {
            bool anyVisible = IsAnyUiVisible();

            if (anyVisible && !uiActive)
            {
                CaptureSelectionBeforeQuickMenuActivation();
                EnableUiInteraction();
                EnableCursorHook();
                uiActive = true;
                return;
            }

            if (!anyVisible && uiActive)
            {
                DisableCursorHook();
                RestoreUiInteraction();
                TryRestoreSelectionAfterQuickMenuClose();
                gamepadReleasePending = gamepadMode;
                uiActive = false;
            }
        }

        private void EnableCursorHook()
        {
            if (cursorHookActive)
            {
                return;
            }

            On.InputHandler.OnGUI += ForceCursorVisible;
            ModHooks.CursorHook += ForceCursorVisible;
            cursorHookActive = true;
        }

        private void DisableCursorHook()
        {
            if (!cursorHookActive)
            {
                return;
            }

            On.InputHandler.OnGUI -= ForceCursorVisible;
            ModHooks.CursorHook -= ForceCursorVisible;
            cursorHookActive = false;
        }

        private static void ForceCursorVisible(On.InputHandler.orig_OnGUI orig, InputHandler self)
        {
            orig(self);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private static void ForceCursorVisible()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
