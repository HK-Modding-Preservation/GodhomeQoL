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
        private void OnCollectorBackClicked()
        {
            bool reopenBossManipulate = returnToBossManipulateOnClose;
            bool reopenQuick = returnToQuickOnClose;
            returnToBossManipulateOnClose = false;
            returnToQuickOnClose = false;

            if (reopenBossManipulate)
            {
                returnToQuickOnClose = reopenQuick;
                SetBossManipulateVisible(true);
            }
            else if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            SetCollectorVisible(false);
        }

        private void OnZoteHelperBackClicked()
        {
            bool reopenBossManipulate = returnToBossManipulateOnClose;
            bool reopenQuick = returnToQuickOnClose;
            returnToBossManipulateOnClose = false;
            returnToQuickOnClose = false;

            if (reopenBossManipulate)
            {
                returnToQuickOnClose = reopenQuick;
                SetBossManipulateVisible(true);
            }
            else if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            SetZoteHelperVisible(false);
        }

        private void OnZoteHelperResetDefaultsClicked()
        {
            bool wasEnabled = GetZoteHelperEnabled();
            ResetZoteHelperDefaults();

            if (wasEnabled)
            {
                Modules.BossChallenge.ZoteHelper.ApplyBossHealthIfPresent();
                Modules.BossChallenge.ZoteHelper.ApplyZotelingHealthIfPresent();
            }

            SetZoteHelperEnabled(false);
            RefreshZoteHelperUi();
        }

        private void OnGruzHelperBackClicked()
        {
            bool reopenBossManipulate = returnToBossManipulateOnClose;
            bool reopenQuick = returnToQuickOnClose;
            returnToBossManipulateOnClose = false;
            returnToQuickOnClose = false;

            if (reopenBossManipulate)
            {
                returnToQuickOnClose = reopenQuick;
                SetBossManipulateVisible(true);
            }
            else if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            SetGruzHelperVisible(false);
        }

        private void OnGruzHelperResetDefaultsClicked()
        {
            ResetGruzMotherHelperDefaults();
            SetGruzMotherHelperEnabled(false);
            Modules.BossChallenge.GruzMotherHelper.RestoreVanillaHealthIfPresent();
            RefreshGruzHelperUi();
        }

        private void OnHornetHelperBackClicked()
        {
            bool reopenBossManipulate = returnToBossManipulateOnClose;
            bool reopenQuick = returnToQuickOnClose;
            returnToBossManipulateOnClose = false;
            returnToQuickOnClose = false;

            if (reopenBossManipulate)
            {
                returnToQuickOnClose = reopenQuick;
                SetBossManipulateVisible(true);
            }
            else if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            SetHornetHelperVisible(false);
        }

        private void OnHornetHelperResetDefaultsClicked()
        {
            ResetHornetProtectorHelperDefaults();
            SetHornetProtectorHelperEnabled(false);
            Modules.BossChallenge.HornetProtectorHelper.RestoreVanillaHealthIfPresent();
            RefreshHornetHelperUi();
        }

        private void OnMawlekHelperBackClicked()
        {
            bool reopenBossManipulate = returnToBossManipulateOnClose;
            bool reopenQuick = returnToQuickOnClose;
            returnToBossManipulateOnClose = false;
            returnToQuickOnClose = false;

            if (reopenBossManipulate)
            {
                returnToQuickOnClose = reopenQuick;
                SetBossManipulateVisible(true);
            }
            else if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            SetMawlekHelperVisible(false);
        }

        private void OnMawlekHelperResetDefaultsClicked()
        {
            ResetBroodingMawlekHelperDefaults();
            SetBroodingMawlekHelperEnabled(false);
            Modules.BossChallenge.BroodingMawlekHelper.RestoreVanillaHealthIfPresent();
            RefreshMawlekHelperUi();
        }

        private void OnMassiveMossHelperBackClicked()
        {
            bool reopenBossManipulate = returnToBossManipulateOnClose;
            bool reopenQuick = returnToQuickOnClose;
            returnToBossManipulateOnClose = false;
            returnToQuickOnClose = false;

            if (reopenBossManipulate)
            {
                returnToQuickOnClose = reopenQuick;
                SetBossManipulateVisible(true);
            }
            else if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            SetMassiveMossHelperVisible(false);
        }

        private void OnMassiveMossHelperResetDefaultsClicked()
        {
            ResetMassiveMossChargerHelperDefaults();
            SetMassiveMossChargerHelperEnabled(false);
            Modules.BossChallenge.MassiveMossChargerHelper.RestoreVanillaHealthIfPresent();
            RefreshMassiveMossHelperUi();
        }

        private void OnCrystalGuardianHelperBackClicked()
        {
            bool reopenBossManipulate = returnToBossManipulateOnClose;
            bool reopenQuick = returnToQuickOnClose;
            returnToBossManipulateOnClose = false;
            returnToQuickOnClose = false;

            if (reopenBossManipulate)
            {
                returnToQuickOnClose = reopenQuick;
                SetBossManipulateVisible(true);
            }
            else if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            SetCrystalGuardianHelperVisible(false);
        }

        private void OnCrystalGuardianHelperResetDefaultsClicked()
        {
            ResetCrystalGuardianHelperDefaults();
            SetCrystalGuardianHelperEnabled(false);
            Modules.BossChallenge.CrystalGuardianHelper.RestoreVanillaHealthIfPresent();
            RefreshCrystalGuardianHelperUi();
        }

        private void OnEnragedGuardianHelperBackClicked()
        {
            bool reopenBossManipulate = returnToBossManipulateOnClose;
            bool reopenQuick = returnToQuickOnClose;
            returnToBossManipulateOnClose = false;
            returnToQuickOnClose = false;

            if (reopenBossManipulate)
            {
                returnToQuickOnClose = reopenQuick;
                SetBossManipulateVisible(true);
            }
            else if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            SetEnragedGuardianHelperVisible(false);
        }

        private void OnEnragedGuardianHelperResetDefaultsClicked()
        {
            ResetEnragedGuardianHelperDefaults();
            SetEnragedGuardianHelperEnabled(false);
            Modules.BossChallenge.EnragedGuardianHelper.RestoreVanillaHealthIfPresent();
            RefreshEnragedGuardianHelperUi();
        }

        private void OnHornetSentinelHelperBackClicked()
        {
            bool reopenBossManipulate = returnToBossManipulateOnClose;
            bool reopenQuick = returnToQuickOnClose;
            returnToBossManipulateOnClose = false;
            returnToQuickOnClose = false;

            if (reopenBossManipulate)
            {
                returnToQuickOnClose = reopenQuick;
                SetBossManipulateVisible(true);
            }
            else if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            SetHornetSentinelHelperVisible(false);
            SetAllAdditionalGhostHelpersVisible(false);
        }

        private void OnHornetSentinelHelperResetDefaultsClicked()
        {
            ResetHornetSentinelHelperDefaults();
            SetHornetSentinelHelperEnabled(false);
            Modules.BossChallenge.HornetSentinelHelper.RestoreVanillaHealthIfPresent();
            RefreshHornetSentinelHelperUi();
        }

        private void OnCollectorResetClicked()
        {
            ResetCollectorPhasesDefaults();
            SetCollectorPhasesEnabled(false);
            RefreshCollectorPhasesUi();
        }
    }
}
