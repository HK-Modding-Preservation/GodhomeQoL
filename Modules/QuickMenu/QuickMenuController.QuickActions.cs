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
        private void OpenPanelFromQuick(Action<bool> showPanel)
        {
            returnToQuickOnClose = true;
            HideAllPanelsExcept(showPanel);
            showPanel(true);
            SetQuickVisible(false);
        }

        private void HideAllPanelsExcept(Action<bool> keep)
        {
            Action<bool>[] panels =
            {
                SetOverlayVisible,
                SetCollectorVisible,
                SetFastReloadVisible,
                SetDreamshieldVisible,
                SetShowHpOnDeathVisible,
                SetMaskDamageVisible,
                SetFreezeHitboxesVisible,
                SetSpeedChangerVisible,
                SetTeleportKitVisible,
                SetBossChallengeVisible,
                SetTrueBossRushVisible,
                SetRandomPantheonsVisible,
                SetAlwaysFuriousVisible,
                SetGearSwitcherVisible,
                SetFpsBoostVisible,
                SetQolVisible,
                SetMenuAnimationVisible,
                SetBossAnimationVisible,
                SetZoteHelperVisible,
                SetGruzHelperVisible,
                SetHornetHelperVisible,
                SetMawlekHelperVisible,
                SetMassiveMossHelperVisible,
                SetCrystalGuardianHelperVisible,
                SetEnragedGuardianHelperVisible,
                SetHornetSentinelHelperVisible,
                SetAllAdditionalGhostHelpersVisible,
                SetBossManipulateVisible,
                SetCheatsVisible,
                SetSettingsPresetsVisible,
                SetBossHistoryVisible
            };

            foreach (Action<bool> panel in panels)
            {
                if (panel != keep)
                {
                    panel(false);
                }
            }

            if (gearSwitcherCharmCostVisible)
            {
                SetGearSwitcherCharmCostVisible(false);
            }

            if (gearSwitcherPresetVisible)
            {
                SetGearSwitcherPresetVisible(false);
            }
        }

        private void OnQuickFastSuperDashClicked()
        {
            OpenPanelFromQuick(SetOverlayVisible);
        }

        private void OnQuickFastReloadClicked()
        {
            OpenPanelFromQuick(SetFastReloadVisible);
        }

        private void OnQuickDreamshieldClicked()
        {
            OpenPanelFromQuick(SetDreamshieldVisible);
        }

        private void OnQuickShowHpOnDeathClicked()
        {
            OpenPanelFromQuick(SetShowHpOnDeathVisible);
        }

        private void OnQuickMaskDamageClicked()
        {
            OpenPanelFromQuick(SetMaskDamageVisible);
        }

        private void OnQuickFreezeHitboxesClicked()
        {
            OpenPanelFromQuick(SetFreezeHitboxesVisible);
        }

        private void OnQuickFpsBoostClicked()
        {
            OpenPanelFromQuick(SetFpsBoostVisible);
        }

        private void OnQuickSpeedChangerClicked()
        {
            OpenPanelFromQuick(SetSpeedChangerVisible);
        }

        private void OnQuickTeleportKitClicked()
        {
            OpenPanelFromQuick(SetTeleportKitVisible);
        }

        private void OnQuickBossChallengeClicked()
        {
            OpenPanelFromQuick(SetBossChallengeVisible);
        }

        private void OnQuickBossManipulateClicked()
        {
            returnToBossManipulateOnClose = false;
            OpenPanelFromQuick(SetBossManipulateVisible);
        }

        private void OnQuickRandomPantheonsClicked()
        {
            OpenPanelFromQuick(SetRandomPantheonsVisible);
        }

        private void OnQuickTrueBossRushClicked()
        {
            OpenPanelFromQuick(SetTrueBossRushVisible);
        }

        private void OnQuickCheatsClicked()
        {
            OpenPanelFromQuick(SetCheatsVisible);
        }

        private void OnQuickSettingsPresetsClicked()
        {
            OpenPanelFromQuick(SetSettingsPresetsVisible);
        }

        private void OnQuickAlwaysFuriousClicked()
        {
            OpenPanelFromQuick(SetAlwaysFuriousVisible);
        }

        private void OnQuickGearSwitcherClicked()
        {
            OpenPanelFromQuick(SetGearSwitcherVisible);
        }

        private void OnQuickBossAnimationClicked()
        {
            returnToQolOnClose = false;
            OpenPanelFromQuick(SetBossAnimationVisible);
        }

        private void OnQuickMenuAnimationClicked()
        {
            returnToQolOnClose = false;
            OpenPanelFromQuick(SetMenuAnimationVisible);
        }

        private void OnQuickQolClicked()
        {
            returnToQolOnClose = false;
            OpenPanelFromQuick(SetQolVisible);
        }
    }
}
