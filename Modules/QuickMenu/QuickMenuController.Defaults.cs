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
    {        internal static void ApplyInitialDefaults()
        {
            GodhomeQoL.GlobalSettings.QuickMenuMasters ??= new QuickMenuMasterSettings();

            SetModuleEnabled<Modules.QoL.FastSuperDash>(false);
            Modules.QoL.FastSuperDash.instantSuperDash = false;
            Modules.QoL.FastSuperDash.fastSuperDashEverywhere = false;
            Modules.QoL.FastSuperDash.fastSuperDashSpeedMultiplier = 1f;

            ResetCollectorPhasesDefaults();
            SetModuleEnabled<Modules.QoL.CollectorRoarMute>(false);
            SetModuleEnabled<Modules.CollectorPhases.CollectorPhases>(false);

            SetModuleEnabled<Modules.FastReload>(false);
            Modules.FastReload.reloadKeyCode = (int)KeyCode.None;

            Modules.QoL.DreamshieldStartAngle.ResetDefaults();

            ShowHPOnDeath.ResetFeatureDefaults();
            ApplyShowHpBinding(ShowHpSettings.Keybinds.Hide, null);
            ApplyShowHpBinding(ShowHpSettings.Keybinds.History, null);

            GodhomeQoL.GlobalSettings.MaskDamage ??= new MaskDamageSettings();
            GodhomeQoL.GlobalSettings.MaskDamage.Enabled = false;
            GodhomeQoL.GlobalSettings.MaskDamage.DamageMultiplier = 1f;
            GodhomeQoL.GlobalSettings.MaskDamage.ShowUI = true;
            GodhomeQoL.GlobalSettings.MaskDamage.ToggleUiKeybind = string.Empty;

            GodhomeQoL.GlobalSettings.FreezeHitboxes ??= new FreezeHitboxesSettings();
            GodhomeQoL.GlobalSettings.FreezeHitboxes.Enabled = false;
            GodhomeQoL.GlobalSettings.FreezeHitboxes.AnyHits = false;
            GodhomeQoL.GlobalSettings.FreezeHitboxes.UnfreezeKeybind = string.Empty;

            GodhomeQoL.GlobalSettings.NailDamageCheckKeybind = string.Empty;

            SetModuleEnabled<Modules.Performance.FpsBoost>(false);
            ResetFpsBoostDefaults();

            SpeedChanger.globalSwitch = false;
            SpeedChanger.restrictToggleToRooms = false;
            SpeedChanger.unlimitedSpeed = false;
            SpeedChanger.displayStyle = 0;
            SpeedChanger.toggleKeybind = string.Empty;
            SpeedChanger.inputSpeedKeybind = string.Empty;
            SpeedChanger.speed = 1f;

            SetModuleEnabled<Modules.QoL.TeleportKit>(false);
            Modules.QoL.TeleportKit.MenuHotkey = KeyCode.F6;
            Modules.QoL.TeleportKit.SaveTeleportHotkey = KeyCode.R;
            Modules.QoL.TeleportKit.TeleportHotkey = KeyCode.T;

            SetModuleEnabled<Modules.BossChallenge.InfiniteChallenge>(false);
            SetModuleEnabled<Modules.BossChallenge.AlwaysFurious>(false);
            SetModuleEnabled<Modules.BossChallenge.RandomPantheons>(false);
            SetModuleEnabled<Modules.BossChallenge.TrueBossRush>(false);
            SetModuleEnabled<Modules.Cheats.Cheats>(false);
            SetModuleEnabled<Modules.BossChallenge.InfiniteGrimmPufferfish>(false);
            SetModuleEnabled<Modules.BossChallenge.InfiniteRadianceClimbing>(false);
            Modules.QoL.CarefreeMelodyReset.SetMode(Modules.QoL.CarefreeMelodyReset.ModeOff);
            SetModuleEnabled<Modules.BossChallenge.SegmentedP5>(false);
            SetModuleEnabled<Modules.BossChallenge.AddLifeblood>(false);
            SetModuleEnabled<Modules.BossChallenge.AddSoul>(false);
            SetModuleEnabled<Modules.BossChallenge.ForceArriveAnimation>(false);

            Modules.BossChallenge.InfiniteChallenge.restartFightOnSuccess = false;
            Modules.BossChallenge.InfiniteChallenge.restartFightAndMusic = false;
            Modules.BossChallenge.AddLifeblood.lifebloodAmount = 0;
            Modules.BossChallenge.AddSoul.soulAmount = 0;
            Modules.BossChallenge.SegmentedP5.selectedP5Segment = 0;

            ResetZoteHelperDefaults();
            Modules.BossChallenge.ForceGreyPrinceEnterType.gpzEnterType =
                Modules.BossChallenge.ForceGreyPrinceEnterType.EnterType.Off;
            SetModuleEnabled<Modules.BossChallenge.ForceGreyPrinceEnterType>(false);
            SetModuleEnabled<Modules.BossChallenge.ZoteHelper>(false);
            ResetGruzMotherHelperDefaults();
            SetModuleEnabled<Modules.BossChallenge.GruzMotherHelper>(false);
            ResetGruzMotherP1HelperDefaults();
            SetModuleEnabled<Modules.BossChallenge.GruzMotherP1Helper>(false);
            ResetVengeflyKingP1HelperDefaults();
            SetModuleEnabled<Modules.BossChallenge.VengeflyKingP1Helper>(false);
            ResetBroodingMawlekP1HelperDefaults();
            SetModuleEnabled<Modules.BossChallenge.BroodingMawlekP1Helper>(false);
            ResetNoskP2HelperDefaults();
            SetModuleEnabled<Modules.BossChallenge.NoskP2Helper>(false);
            ResetUumuuP3HelperDefaults();
            SetModuleEnabled<Modules.BossChallenge.UumuuP3Helper>(false);
            ResetSoulWarriorP1HelperDefaults();
            SetModuleEnabled<Modules.BossChallenge.SoulWarriorP1Helper>(false);
            ResetNoEyesP4HelperDefaults();
            SetModuleEnabled<Modules.BossChallenge.NoEyesP4Helper>(false);
            ResetMarmuP2HelperDefaults();
            SetModuleEnabled<Modules.BossChallenge.MarmuP2Helper>(false);
            ResetXeroP2HelperDefaults();
            SetModuleEnabled<Modules.BossChallenge.XeroP2Helper>(false);
            ResetMarkothP4HelperDefaults();
            SetModuleEnabled<Modules.BossChallenge.MarkothP4Helper>(false);
            ResetGorbP1HelperDefaults();
            SetModuleEnabled<Modules.BossChallenge.GorbP1Helper>(false);
            ResetHornetProtectorHelperDefaults();
            SetModuleEnabled<Modules.BossChallenge.HornetProtectorHelper>(false);
            ResetBroodingMawlekHelperDefaults();
            SetModuleEnabled<Modules.BossChallenge.BroodingMawlekHelper>(false);
            ResetMassiveMossChargerHelperDefaults();
            SetModuleEnabled<Modules.BossChallenge.MassiveMossChargerHelper>(false);
            ResetCrystalGuardianHelperDefaults();
            SetModuleEnabled<Modules.BossChallenge.CrystalGuardianHelper>(false);
            ResetEnragedGuardianHelperDefaults();
            SetModuleEnabled<Modules.BossChallenge.EnragedGuardianHelper>(false);
            ResetHornetSentinelHelperDefaults();
            SetModuleEnabled<Modules.BossChallenge.HornetSentinelHelper>(false);
            ResetAdditionalGhostHelperDefaultsGlobal();
            SetAdditionalGhostHelpersModulesEnabled(false);

            SetModuleEnabled<Modules.QoL.FastDreamWarp>(false);
            FastDreamWarpSettings.Keybinds.Toggle.ClearBindings();
            SetModuleEnabled<Modules.QoL.ShortDeathAnimation>(true);
            SetModuleEnabled<Modules.QoL.InvincibleIndicator>(true);
            SetModuleEnabled<Modules.QoL.ScreenShake>(true);
            Modules.QoL.SkipCutscenes.HallOfGodsStatues = true;
            SetModuleEnabled<Modules.QoL.UnlockAllModes>(true);
            SetModuleEnabled<Modules.QoL.UnlockPantheons>(true);
            SetModuleEnabled<Modules.QoL.UnlockRadiance>(true);
            SetModuleEnabled<Modules.QoL.UnlockRadiant>(true);

            GodhomeQoL.GlobalSettings.QuickMenuOpacity = 100;
            GodhomeQoL.GlobalSettings.GearSwitcher ??= new GearSwitcherSettings();
            GodhomeQoL.GlobalSettings.GearSwitcher.Enabled = false;

            SetModuleEnabled<Modules.QoL.DoorDefaultBegin>(true);
            SetModuleEnabled<Modules.QoL.FasterLoads>(true);
            SetModuleEnabled<Modules.QoL.FastMenus>(true);
            SetModuleEnabled<Modules.QoL.FastText>(true);
            Modules.QoL.SkipCutscenes.AutoSkipCinematics = true;
            Modules.QoL.SkipCutscenes.AllowSkippingNonskippable = true;
            Modules.QoL.SkipCutscenes.SkipCutscenesWithoutPrompt = true;

            Modules.QoL.SkipCutscenes.AbsoluteRadiance = true;
            Modules.QoL.SkipCutscenes.PureVesselRoar = true;
            Modules.QoL.SkipCutscenes.GrimmNightmare = true;
            Modules.QoL.SkipCutscenes.GreyPrinceZote = true;
            Modules.QoL.SkipCutscenes.Collector = true;
            Modules.QoL.SkipCutscenes.SoulMasterPhaseTransitionSkip = true;

            Modules.BossChallenge.RandomPantheons.Pantheon1Enabled = false;
            Modules.BossChallenge.RandomPantheons.Pantheon2Enabled = false;
            Modules.BossChallenge.RandomPantheons.Pantheon3Enabled = false;
            Modules.BossChallenge.RandomPantheons.Pantheon4Enabled = false;
            Modules.BossChallenge.RandomPantheons.Pantheon5Enabled = false;
            Modules.BossChallenge.TrueBossRush.TrueBossRushPantheon1Enabled = false;
            Modules.BossChallenge.TrueBossRush.TrueBossRushPantheon2Enabled = false;
            Modules.BossChallenge.TrueBossRush.TrueBossRushPantheon3Enabled = false;
            Modules.BossChallenge.TrueBossRush.TrueBossRushPantheon4Enabled = false;
            Modules.BossChallenge.TrueBossRush.TrueBossRushPantheon5Enabled = false;
            Modules.Cheats.Cheats.SetInfiniteSoulEnabled(false);
            Modules.Cheats.Cheats.SetInfiniteHpEnabled(false);
            Modules.Cheats.Cheats.SetInvincibilityEnabled(false);
            Modules.Cheats.Cheats.SetNoclipEnabled(false);
            Modules.Cheats.Cheats.SetKillAllHotkeyRaw(string.Empty);

            GodhomeQoL.GlobalSettings.QuickMenuMasters.BossChallengeEnabled = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.BossChallengeHasSnapshot = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.QolEnabled = true;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.QolHasSnapshot = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.MenuAnimEnabled = true;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.MenuAnimHasSnapshot = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.BossAnimEnabled = true;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.BossAnimHasSnapshot = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.BossAnimSavedHallOfGods = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.RandomPantheonsEnabled = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.RandomPantheonsHasSnapshot = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.RandomPantheonsSavedP1 = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.RandomPantheonsSavedP2 = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.RandomPantheonsSavedP3 = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.RandomPantheonsSavedP4 = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.RandomPantheonsSavedP5 = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.TrueBossRushEnabled = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.TrueBossRushHasSnapshot = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.TrueBossRushSavedP1 = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.TrueBossRushSavedP2 = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.TrueBossRushSavedP3 = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.TrueBossRushSavedP4 = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.TrueBossRushSavedP5 = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.CheatsEnabled = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.CheatsHasSnapshot = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.CheatsSavedInfiniteSoul = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.CheatsSavedInfiniteHp = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.CheatsSavedInvincibility = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.CheatsSavedNoclip = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.BossManipulateGlobalP5Enabled = false;
            GodhomeQoL.GlobalSettings.QuickMenuMasters.BossManipulateGlobalP5TouchedModules ??= new List<string>();
            GodhomeQoL.GlobalSettings.QuickMenuMasters.BossManipulateGlobalP5TouchedModules.Clear();
            GodhomeQoL.GlobalSettings.QuickMenuMasters.BossManipulateGlobalP5EnabledModules ??= new List<string>();
            GodhomeQoL.GlobalSettings.QuickMenuMasters.BossManipulateGlobalP5EnabledModules.Clear();
        }

        private bool GetModuleEnabled()
        {
            Module? module = GetFastSuperDashModule();
            return module?.Enabled ?? false;
        }

        private void SetModuleEnabled(bool value)
        {
            Module? module = GetFastSuperDashModule();
            if (module != null)
            {
                module.Enabled = value;
            }

            GodhomeQoL.SaveGlobalSettingsSafe();
            UpdateFastSuperDashInteractivity();
            UpdateQuickMenuEntryStateColors();
        }

        private bool GetFastReloadEnabled()
        {
            return GetFastReloadModule()?.Enabled ?? false;
        }

        private void SetFastReloadEnabled(bool value)
        {
            SetModuleEnabledFlag(GetFastReloadModule(), value);
            UpdateFastReloadInteractivity();
            UpdateQuickMenuEntryStateColors();
        }

        private bool GetDreamshieldEnabled()
        {
            return Modules.QoL.DreamshieldStartAngle.startAngleEnabled;
        }

        private void SetDreamshieldEnabled(bool value)
        {
            Modules.QoL.DreamshieldStartAngle.SetEnabled(value);
            UpdateDreamshieldSliderState();
            UpdateDreamshieldInteractivity();
            UpdateQuickMenuEntryStateColors();
        }

        private bool GetTeleportKitEnabled()
        {
            return GetTeleportKitModule()?.Enabled ?? false;
        }

        private void SetTeleportKitEnabled(bool value)
        {
            SetModuleEnabledFlag(GetTeleportKitModule(), value);
            UpdateTeleportKitInteractivity();
            UpdateQuickMenuEntryStateColors();
        }

        private bool GetShowHpOnDeathEnabled() => ShowHpSettings.EnabledMod;

        private void SetShowHpOnDeathEnabled(bool value)
        {
            ShowHPOnDeath.SetFeatureEnabled(value);
            UpdateShowHpOnDeathInteractivity();
            UpdateQuickMenuEntryStateColors();
        }

        private bool GetMaskDamageEnabled()
        {
            return MaskDamage.GetEnabled();
        }

        private void SetMaskDamageEnabled(bool value)
        {
            MaskDamage.SetEnabled(value);

            UpdateMaskDamageInteractivity();
            UpdateQuickMenuEntryStateColors();
        }

        private static bool GetMaskDamageUiVisible()
        {
            return MaskDamage.GetUiVisible();
        }

        private void SetMaskDamageUiVisible(bool value)
        {
            MaskDamage.SetUiVisible(value);
            RefreshMaskDamageUi();
        }

        private bool GetFreezeHitboxesEnabled()
        {
            return FreezeHitboxes.GetEnabled();
        }

        private void SetFreezeHitboxesEnabled(bool value)
        {
            FreezeHitboxes.SetEnabled(value);

            UpdateFreezeHitboxesInteractivity();
            UpdateQuickMenuEntryStateColors();
        }

        private static string GetFreezeHitboxesModeLabel()
        {
            return FreezeHitboxes.GetAnyHitsMode() ? "Any Hits" : "Death";
        }

        private void ToggleFreezeHitboxesMode()
        {
            FreezeHitboxes.SetAnyHitsMode(!FreezeHitboxes.GetAnyHitsMode());
            if (freezeHitboxesModeValue != null)
            {
                freezeHitboxesModeValue.text = GetFreezeHitboxesModeLabel();
            }
        }

        private bool GetInfiniteChallengeEnabled()
        {
            return GetInfiniteChallengeModule()?.Enabled ?? false;
        }

        private void SetInfiniteChallengeEnabled(bool value)
        {
            Module? module = GetInfiniteChallengeModule();
            if (module != null)
            {
                module.Enabled = value;
            }
        }

        private bool GetAlwaysFuriousEnabled()
        {
            return GetAlwaysFuriousModule()?.Enabled ?? false;
        }

        private void SetAlwaysFuriousEnabled(bool value)
        {
            Module? module = GetAlwaysFuriousModule();
            if (module != null)
            {
                module.Enabled = value;
            }

            UpdateQuickMenuEntryStateColors();
            RefreshAlwaysFuriousUi();
            RefreshGearSwitcherUi();
        }

        private bool GetInfiniteGrimmPufferfishEnabled()
        {
            return GetInfiniteGrimmPufferfishModule()?.Enabled ?? false;
        }
    }
}
