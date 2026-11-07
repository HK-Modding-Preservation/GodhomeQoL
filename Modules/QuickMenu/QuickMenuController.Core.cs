namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController
    {
        private void Awake()
        {
            SetController(this);
            LoadMasterSettings();
            BuildUi();
            SetQuickVisible(false, true);
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
        }

        private void LoadMasterSettings()
        {
            QuickMenuMasterSettings settings = GodhomeQoL.GlobalSettings.QuickMenuMasters ??= new QuickMenuMasterSettings();

            bossChallengeMasterEnabled = settings.BossChallengeEnabled;
            bossChallengeMasterHasSnapshot = settings.BossChallengeHasSnapshot;
            bossChallengeSavedInfiniteChallenge = settings.BossChallengeSavedInfiniteChallenge;
            bossChallengeSavedRestartOnSuccess = settings.BossChallengeSavedRestartOnSuccess;
            bossChallengeSavedRestartAndMusic = settings.BossChallengeSavedRestartAndMusic;
            bossChallengeSavedCarefreeMelodyMode = settings.BossChallengeSavedCarefreeMelodyMode;
            if (bossChallengeSavedCarefreeMelodyMode < 0)
            {
                bossChallengeSavedCarefreeMelodyMode = settings.BossChallengeSavedCarefreeMelody
                    ? Modules.QoL.CarefreeMelodyReset.ModeHoG
                    : Modules.QoL.CarefreeMelodyReset.ModeOff;
                settings.BossChallengeSavedCarefreeMelodyMode = bossChallengeSavedCarefreeMelodyMode;
            }
            bossChallengeSavedInfiniteGrimm = settings.BossChallengeSavedInfiniteGrimm;
            bossChallengeSavedInfiniteRadiance = settings.BossChallengeSavedInfiniteRadiance;
            bossChallengeSavedSegmentedP5 = settings.BossChallengeSavedSegmentedP5;
            bossChallengeSavedAddLifeblood = settings.BossChallengeSavedAddLifeblood;
            bossChallengeSavedAddSoul = settings.BossChallengeSavedAddSoul;
            bossChallengeSavedForceArrive = settings.BossChallengeSavedForceArriveAnimation;

            qolMasterEnabled = settings.QolEnabled;
            qolMasterHasSnapshot = settings.QolHasSnapshot;
            qolSavedFastDreamWarp = settings.QolSavedFastDreamWarp;
            qolSavedShortDeath = settings.QolSavedShortDeath;
            qolSavedUnlockAllModes = settings.QolSavedUnlockAllModes;
            qolSavedUnlockPantheons = settings.QolSavedUnlockPantheons;
            qolSavedUnlockRadiance = settings.QolSavedUnlockRadiance;
            qolSavedUnlockRadiant = settings.QolSavedUnlockRadiant;
            qolSavedInvincibleIndicator = settings.QolSavedInvincibleIndicator;
            qolSavedScreenShake = settings.QolSavedScreenShake;

            menuAnimMasterEnabled = settings.MenuAnimEnabled;
            menuAnimMasterHasSnapshot = settings.MenuAnimHasSnapshot;
            menuAnimSavedDoorDefaultBegin = settings.MenuAnimSavedDoorDefaultBegin;
            menuAnimSavedFasterLoads = settings.MenuAnimSavedFasterLoads;
            menuAnimSavedFastMenus = settings.MenuAnimSavedFastMenus;
            menuAnimSavedFastText = settings.MenuAnimSavedFastText;
            menuAnimSavedAutoSkip = settings.MenuAnimSavedAutoSkip;
            menuAnimSavedAllowSkipping = settings.MenuAnimSavedAllowSkipping;
            menuAnimSavedSkipWithoutPrompt = settings.MenuAnimSavedSkipWithoutPrompt;

            bossAnimMasterEnabled = settings.BossAnimEnabled;
            bossAnimMasterHasSnapshot = settings.BossAnimHasSnapshot;
            bossAnimSavedHallOfGods = settings.BossAnimSavedHallOfGods || settings.QolSavedHallOfGods;
            bossAnimSavedAbsoluteRadiance = settings.BossAnimSavedAbsoluteRadiance;
            bossAnimSavedPantheonVEnding = settings.BossAnimSavedPantheonVEnding;
            bossAnimSavedPureVesselRoar = settings.BossAnimSavedPureVesselRoar;
            bossAnimSavedGrimmNightmare = settings.BossAnimSavedGrimmNightmare;
            bossAnimSavedGreyPrinceZote = settings.BossAnimSavedGreyPrinceZote;
            bossAnimSavedCollector = settings.BossAnimSavedCollector;
            bossAnimSavedSoulMaster = settings.BossAnimSavedSoulMaster;
            bossAnimSavedCollectorRoar = settings.BossAnimSavedCollectorRoar;

            randomPantheonsMasterEnabled = settings.RandomPantheonsEnabled;
            randomPantheonsMasterHasSnapshot = settings.RandomPantheonsHasSnapshot;
            randomPantheonsSavedP1 = settings.RandomPantheonsSavedP1;
            randomPantheonsSavedP2 = settings.RandomPantheonsSavedP2;
            randomPantheonsSavedP3 = settings.RandomPantheonsSavedP3;
            randomPantheonsSavedP4 = settings.RandomPantheonsSavedP4;
            randomPantheonsSavedP5 = settings.RandomPantheonsSavedP5;

            trueBossRushMasterEnabled = settings.TrueBossRushEnabled;
            trueBossRushMasterHasSnapshot = settings.TrueBossRushHasSnapshot;
            trueBossRushSavedP1 = settings.TrueBossRushSavedP1;
            trueBossRushSavedP2 = settings.TrueBossRushSavedP2;
            trueBossRushSavedP3 = settings.TrueBossRushSavedP3;
            trueBossRushSavedP4 = settings.TrueBossRushSavedP4;
            trueBossRushSavedP5 = settings.TrueBossRushSavedP5;

            cheatsMasterEnabled = settings.CheatsEnabled;
            cheatsMasterHasSnapshot = settings.CheatsHasSnapshot;
            cheatsSavedInfiniteSoul = settings.CheatsSavedInfiniteSoul;
            cheatsSavedInfiniteHp = settings.CheatsSavedInfiniteHp;
            cheatsSavedInvincibility = settings.CheatsSavedInvincibility;
            cheatsSavedNoclip = settings.CheatsSavedNoclip;

            if (randomPantheonsMasterEnabled && trueBossRushMasterEnabled)
            {
                _ = Modules.BossChallenge.PantheonSequenceCompatibility.DisableTrueBossRush();

                settings = GodhomeQoL.GlobalSettings.QuickMenuMasters ??= new QuickMenuMasterSettings();
                trueBossRushMasterEnabled = settings.TrueBossRushEnabled;
                trueBossRushMasterHasSnapshot = settings.TrueBossRushHasSnapshot;
                trueBossRushSavedP1 = settings.TrueBossRushSavedP1;
                trueBossRushSavedP2 = settings.TrueBossRushSavedP2;
                trueBossRushSavedP3 = settings.TrueBossRushSavedP3;
                trueBossRushSavedP4 = settings.TrueBossRushSavedP4;
                trueBossRushSavedP5 = settings.TrueBossRushSavedP5;
            }

            if (!bossChallengeMasterEnabled)
            {
                SetBossChallengeAll(false);
            }

            if (!qolMasterEnabled)
            {
                SetQolAll(false);
            }

            if (!menuAnimMasterEnabled)
            {
                SetMenuAnimationAll(false);
            }

            if (!bossAnimMasterEnabled)
            {
                SetBossAnimationAll(false);
            }

            if (!randomPantheonsMasterEnabled)
            {
                SetRandomPantheonsAll(false);
            }
            else
            {
                SetRandomPantheonsEnabled(true);
                if (randomPantheonsMasterHasSnapshot)
                {
                    RestoreRandomPantheonsSnapshot();
                }
            }

            if (!trueBossRushMasterEnabled)
            {
                SetTrueBossRushAll(false);
                SetTrueBossRushEnabled(false);
            }
            else
            {
                SetTrueBossRushEnabled(true);
                if (trueBossRushMasterHasSnapshot)
                {
                    RestoreTrueBossRushSnapshot();
                }
            }

            if (!cheatsMasterEnabled)
            {
                SetCheatsAll(false);
                SetCheatsEnabled(false);
            }
            else
            {
                SetCheatsEnabled(true);
                if (cheatsMasterHasSnapshot)
                {
                    RestoreCheatsSnapshot();
                }
            }
        }

        private void OnDestroy()
        {
            DisableCursorHook();
            if (uiActive)
            {
                RestoreUiInteraction();
                uiActive = false;
            }

            if (controller == this)
            {
                SetController(null);
            }

            CancelGearSwitcherPresetRename();
            DestroyRoot(ref quickRoot);
            DestroyRoot(ref overlayRoot);
            DestroyRoot(ref collectorRoot);
            DestroyRoot(ref fastReloadRoot);
            DestroyRoot(ref dreamshieldRoot);
            DestroyRoot(ref showHpOnDeathRoot);
            DestroyRoot(ref maskDamageRoot);
            DestroyRoot(ref freezeHitboxesRoot);
            DestroyRoot(ref fpsBoostRoot);
            DestroyRoot(ref speedChangerRoot);
            DestroyRoot(ref teleportKitRoot);
            DestroyRoot(ref bossChallengeRoot);
            DestroyRoot(ref randomPantheonsRoot);
            DestroyRoot(ref trueBossRushRoot);
            DestroyRoot(ref cheatsRoot);
            DestroyRoot(ref settingsPresetsRoot);
            DestroyRoot(ref bossHistoryRoot);
            DestroyRoot(ref alwaysFuriousRoot);
            DestroyRoot(ref gearSwitcherRoot);
            DestroyRoot(ref gearSwitcherCharmCostRoot);
            DestroyRoot(ref gearSwitcherPresetRoot);
            gearSwitcherTitleRect = null;
            gearSwitcherHintRect = null;
            DestroyRoot(ref qolRoot);
            qolScrollRect = null;
            DestroyRoot(ref menuAnimationRoot);
            DestroyRoot(ref bossAnimationRoot);
            DestroyRoot(ref zoteHelperRoot);
            DestroyRoot(ref gruzHelperRoot);
            DestroyRoot(ref hornetHelperRoot);
            DestroyRoot(ref mawlekHelperRoot);
            DestroyRoot(ref massiveMossHelperRoot);
            DestroyRoot(ref crystalGuardianHelperRoot);
            DestroyRoot(ref enragedGuardianHelperRoot);
            DestroyRoot(ref hornetSentinelHelperRoot);
            DestroyAdditionalGhostHelperRoots();
            DestroyRoot(ref bossManipulateRoot);
            DestroyRoot(ref bossManipulateOtherRoomsRoot);
            DestroyRoot(ref gruzMotherP1HelperRoot);
            DestroyRoot(ref vengeflyKingP1HelperRoot);
            DestroyRoot(ref broodingMawlekP1HelperRoot);
            DestroyRoot(ref noskP2HelperRoot);
            DestroyRoot(ref uumuuP3HelperRoot);
            DestroyRoot(ref soulWarriorP1HelperRoot);
            DestroyRoot(ref noEyesP4HelperRoot);
            DestroyRoot(ref marmuP2HelperRoot);
            DestroyRoot(ref xeroP2HelperRoot);
            DestroyRoot(ref markothP4HelperRoot);
            DestroyRoot(ref gorbP1HelperRoot);
            DestroyRoot(ref quickSettingsRoot);
            DestroyRoot(ref statusRoot);
        }
    }
}
