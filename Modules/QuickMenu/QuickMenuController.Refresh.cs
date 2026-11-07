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
    {        private void RefreshFastSuperDashUi()
        {
            Module? module = GetFastSuperDashModule();
            UpdateToggleValue(moduleToggleValue, module?.Enabled ?? false);
            UpdateToggleIcon(moduleToggleIcon, module?.Enabled ?? false);
            UpdateToggleValue(instantToggleValue, Modules.QoL.FastSuperDash.instantSuperDash);
            UpdateToggleValue(everywhereToggleValue, Modules.QoL.FastSuperDash.fastSuperDashEverywhere);

            float speed = Modules.QoL.FastSuperDash.fastSuperDashSpeedMultiplier;
            UpdateSpeedValueText(speed);
            if (speedSlider != null)
            {
                suppressFastSuperDashSpeedCallback = true;
                try
                {
                    speedSlider.value = Mathf.Clamp(speed, speedSlider.minValue, speedSlider.maxValue);
                }
                finally
                {
                    suppressFastSuperDashSpeedCallback = false;
                }
            }

            UpdateFastSuperDashInteractivity();
        }

        private void RefreshCollectorPhasesUi()
        {
            Module? module = GetCollectorPhasesModule();
            UpdateToggleValue(collectorModuleToggleValue, module?.Enabled ?? false);
            UpdateToggleIcon(collectorModuleToggleIcon, module?.Enabled ?? false);
            UpdateToggleValue(collectorImmortalValue, Modules.CollectorPhases.CollectorPhases.CollectorImmortal);
            UpdateToggleValue(ignoreInitialJarLimitValue, Modules.CollectorPhases.CollectorPhases.IgnoreInitialJarLimit);
            UpdateToggleValue(useCustomPhase2ThresholdValue, Modules.CollectorPhases.CollectorPhases.UseCustomPhase2Threshold);
            UpdateToggleValue(collectorP5HpValue, Modules.CollectorPhases.CollectorPhases.collectorP5Hp);
            UpdateToggleValue(useMaxHpValue, Modules.CollectorPhases.CollectorPhases.UseMaxHP);
            UpdateToggleValue(spawnBuzzerValue, Modules.CollectorPhases.CollectorPhases.spawnBuzzer);
            UpdateToggleValue(spawnRollerValue, Modules.CollectorPhases.CollectorPhases.spawnRoller);
            UpdateToggleValue(spawnSpitterValue, Modules.CollectorPhases.CollectorPhases.spawnSpitter);
            UpdateToggleValue(disableSummonLimitValue, Modules.CollectorPhases.CollectorPhases.DisableSummonLimit);

            UpdateIntValueText(collectorPhaseValue, Modules.CollectorPhases.CollectorPhases.collectorPhase);
            UpdateIntInputValue(customPhase2ThresholdField, Modules.CollectorPhases.CollectorPhases.CustomPhase2Threshold);
            UpdateIntInputValue(collectorMaxHpField, Modules.CollectorPhases.CollectorPhases.collectorMaxHP);
            UpdateIntInputValue(buzzerHpField, Modules.CollectorPhases.CollectorPhases.buzzerHP);
            UpdateIntInputValue(rollerHpField, Modules.CollectorPhases.CollectorPhases.rollerHP);
            UpdateIntInputValue(spitterHpField, Modules.CollectorPhases.CollectorPhases.spitterHP);
            UpdateIntInputValue(customSummonLimitField, Modules.CollectorPhases.CollectorPhases.CustomSummonLimit);

            UpdateCollectorInteractivity();
        }

        private void RefreshFastReloadUi()
        {
            Module? module = GetFastReloadModule();
            UpdateToggleValue(fastReloadToggleValue, module?.Enabled ?? false);
            UpdateToggleIcon(fastReloadToggleIcon, module?.Enabled ?? false);
            UpdateKeybindValue(reloadKeyValue, FormatKeyLabel(GetReloadKey()));

            UpdateFastReloadInteractivity();
        }

        private void RefreshDreamshieldUi()
        {
            bool enabled = Modules.QoL.DreamshieldStartAngle.startAngleEnabled;
            UpdateToggleValue(dreamshieldToggleValue, enabled);
            UpdateToggleIcon(dreamshieldToggleIcon, enabled);

            float delay = Modules.QoL.DreamshieldStartAngle.rotationDelay;
            UpdateFloatValueText(dreamshieldDelayValue, delay, 2);
            if (dreamshieldDelaySlider != null)
            {
                dreamshieldDelaySlider.value = Mathf.Clamp(delay, dreamshieldDelaySlider.minValue, dreamshieldDelaySlider.maxValue);
            }

            float speed = Modules.QoL.DreamshieldStartAngle.rotationSpeed;
            UpdateFloatValueText(dreamshieldSpeedValue, speed, 2);
            if (dreamshieldSpeedSlider != null)
            {
                dreamshieldSpeedSlider.value = Mathf.Clamp(speed, dreamshieldSpeedSlider.minValue, dreamshieldSpeedSlider.maxValue);
            }

            UpdateDreamshieldSliderState();
            UpdateDreamshieldInteractivity();
        }

        private void RefreshShowHpOnDeathUi()
        {
            UpdateToggleValue(showHpGlobalValue, ShowHpSettings.EnabledMod);
            UpdateToggleIcon(showHpGlobalIcon, ShowHpSettings.EnabledMod);
            UpdateToggleValue(showHpShowPbValue, ShowHpSettings.ShowPB);
            UpdateToggleValue(showHpAutoHideValue, ShowHpSettings.HideAfter10Sec);

            UpdateKeybindValue(showHpHudToggleKeyValue, GetShowHpBindingLabel());
            UpdateKeybindValue(showHpHistoryKeyValue, GetShowHpBindingLabel(ShowHpSettings.Keybinds.History));

            float fade = ClampShowHpFade(ShowHpSettings.HudFadeSeconds);
            UpdateFloatValueText(showHpFadeValue, fade, 1);
            if (showHpFadeSlider != null)
            {
                showHpFadeSlider.value = Mathf.Clamp(fade, showHpFadeSlider.minValue, showHpFadeSlider.maxValue);
            }

            UpdateShowHpOnDeathInteractivity();
        }

        private void RefreshMaskDamageUi()
        {
            bool enabled = GetMaskDamageEnabled();
            bool showUi = GetMaskDamageUiVisible();
            UpdateToggleValue(maskDamageToggleValue, enabled);
            UpdateToggleIcon(maskDamageToggleIcon, enabled);
            UpdateToggleValue(maskDamageShowUiValue, showUi);
            UpdateToggleIcon(maskDamageShowUiIcon, showUi);
            UpdateFloatInputValue(maskDamageMultiplierField, MaskDamage.GetMultiplier());
            UpdateKeybindValue(maskDamageToggleUiKeyValue, GetMaskDamageToggleUiKeyLabel());

            UpdateMaskDamageInteractivity();
        }

        private void RefreshFreezeHitboxesUi()
        {
            bool enabled = GetFreezeHitboxesEnabled();
            UpdateToggleValue(freezeHitboxesToggleValue, enabled);
            UpdateToggleIcon(freezeHitboxesToggleIcon, enabled);
            UpdateKeybindValue(freezeHitboxesModeValue, GetFreezeHitboxesModeLabel());
            UpdateKeybindValue(freezeHitboxesUnfreezeKeyValue, GetFreezeHitboxesKeyLabel());

            UpdateFreezeHitboxesInteractivity();
        }

        private void RefreshSpeedChangerUi()
        {
            UpdateToggleValue(speedChangerGlobalValue, SpeedChanger.globalSwitch);
            UpdateToggleIcon(speedChangerGlobalIcon, SpeedChanger.globalSwitch);
            UpdateToggleValue(speedChangerRestrictValue, SpeedChanger.restrictToggleToRooms);
            UpdateToggleValue(speedChangerUnlimitedValue, SpeedChanger.unlimitedSpeed);

            UpdateKeybindValue(speedChangerToggleKeyValue, FormatSpeedChangerKeyLabel(SpeedChanger.toggleKeybind));
            UpdateKeybindValue(speedChangerInputKeyValue, FormatSpeedChangerKeyLabel(SpeedChanger.inputSpeedKeybind));

            UpdateKeybindValue(speedChangerDisplayValue, GetSpeedChangerDisplayValue());

            UpdateSpeedChangerInteractivity();
        }

        private void RefreshTeleportKitUi()
        {
            Module? module = GetTeleportKitModule();
            UpdateToggleValue(teleportKitToggleValue, module?.Enabled ?? false);
            UpdateToggleIcon(teleportKitToggleIcon, module?.Enabled ?? false);
            UpdateKeybindValue(teleportKitMenuKeyValue, GetTeleportKitMenuKeyLabel());
            UpdateKeybindValue(teleportKitSaveKeyValue, GetTeleportKitSaveKeyLabel());
            UpdateKeybindValue(teleportKitTeleportKeyValue, GetTeleportKitTeleportKeyLabel());

            UpdateTeleportKitInteractivity();
        }

        private void RefreshBossChallengeUi()
        {
            UpdateToggleValue(bossChallengeEnableValue, GetBossChallengeMasterEnabled());
            UpdateToggleIcon(bossChallengeEnableIcon, GetBossChallengeMasterEnabled());
            UpdateToggleValue(bossInfiniteChallengeValue, GetInfiniteChallengeEnabled());
            UpdateToggleValue(bossRestartOnSuccessValue, Modules.BossChallenge.InfiniteChallenge.restartFightOnSuccess);
            UpdateToggleValue(bossRestartAndMusicValue, Modules.BossChallenge.InfiniteChallenge.restartFightAndMusic);
            if (qolCarefreeMelodyValue != null)
            {
                qolCarefreeMelodyValue.text = GetCarefreeMelodyModeLabel();
            }
            UpdateToggleValue(bossForceArriveValue, GetForceArriveAnimationEnabled());
            UpdateToggleValue(bossInfiniteGrimmValue, GetInfiniteGrimmPufferfishEnabled());
            UpdateToggleValue(bossInfiniteRadianceValue, GetInfiniteRadianceClimbingEnabled());
            UpdateToggleValue(bossSegmentedP5Value, GetSegmentedP5Enabled());
            UpdateToggleValue(bossAddLifebloodValue, GetAddLifebloodEnabled());
            UpdateToggleValue(bossAddSoulValue, GetAddSoulEnabled());

            UpdateIntInputValue(bossLifebloodAmountField, Modules.BossChallenge.AddLifeblood.lifebloodAmount);
            UpdateIntInputValue(bossSoulAmountField, Modules.BossChallenge.AddSoul.soulAmount);

            if (bossGpzValue != null)
            {
                bossGpzValue.text = GetBossGpzLabel();
            }
            UpdateBossChallengeInteractivity();
        }

        private void RefreshRandomPantheonsUi()
        {
            UpdateToggleValue(randomPantheonsToggleValue, GetRandomPantheonsMasterEnabled());
            UpdateToggleIcon(randomPantheonsToggleIcon, GetRandomPantheonsMasterEnabled());
            UpdateToggleValue(randomPantheonsP1Value, GetRandomPantheonsP1Enabled());
            UpdateToggleValue(randomPantheonsP2Value, GetRandomPantheonsP2Enabled());
            UpdateToggleValue(randomPantheonsP3Value, GetRandomPantheonsP3Enabled());
            UpdateToggleValue(randomPantheonsP4Value, GetRandomPantheonsP4Enabled());
            UpdateToggleValue(randomPantheonsP5Value, GetRandomPantheonsP5Enabled());
            UpdateRandomPantheonsInteractivity();
        }

	        private void RefreshTrueBossRushUi()
	        {
	            UpdateToggleValue(trueBossRushToggleValue, GetTrueBossRushMasterEnabled());
	            UpdateToggleIcon(trueBossRushToggleIcon, GetTrueBossRushMasterEnabled());
	            UpdateToggleValue(trueBossRushP1Value, GetTrueBossRushP1Enabled());
	            UpdateToggleValue(trueBossRushP2Value, GetTrueBossRushP2Enabled());
	            UpdateToggleValue(trueBossRushP3Value, GetTrueBossRushP3Enabled());
	            UpdateToggleValue(trueBossRushP4Value, GetTrueBossRushP4Enabled());
	            UpdateToggleValue(trueBossRushP5Value, GetTrueBossRushP5Enabled());
	            UpdateTrueBossRushInteractivity();
	        }

        internal void RefreshPantheonCompatibilityUiFromExternal()
        {
            RefreshRandomPantheonsUi();
            RefreshTrueBossRushUi();
            RefreshBossChallengeUi();
            UpdateQuickMenuEntryStateColors();
        }

        private void RefreshCheatsUi()
        {
            UpdateToggleValue(cheatsEnableValue, GetCheatsMasterEnabled());
            UpdateToggleIcon(cheatsEnableIcon, GetCheatsMasterEnabled());
            UpdateToggleValue(cheatsInfiniteSoulValue, GetCheatsInfiniteSoulEnabled());
            UpdateToggleValue(cheatsInfiniteHpValue, GetCheatsInfiniteHpEnabled());
            UpdateToggleValue(cheatsInvincibilityValue, GetCheatsInvincibilityEnabled());
            UpdateToggleValue(cheatsNoclipValue, GetCheatsNoclipEnabled());
            UpdateKeybindValue(cheatsKillAllHotkeyValue, GetCheatsKillAllHotkeyLabel());
            UpdateCheatsInteractivity();
        }

        private void RefreshAlwaysFuriousUi()
        {
            UpdateToggleValue(alwaysFuriousToggleValue, GetAlwaysFuriousEnabled());
            UpdateToggleIcon(alwaysFuriousToggleIcon, GetAlwaysFuriousEnabled());
        }

        private void RefreshQolUi()
        {
            UpdateToggleValue(qolEnableValue, GetQolMasterEnabled());
            UpdateToggleIcon(qolEnableIcon, GetQolMasterEnabled());
            UpdateToggleValue(bossAnimFastDreamWarpValue, GetFastDreamWarpEnabled());
            UpdateKeybindValue(fastDreamWarpKeyValue, GetFastDreamWarpBindingLabel());
            UpdateToggleValue(bossAnimShortDeathValue, GetShortDeathAnimationEnabled());
            UpdateToggleValue(qolUnlockAllModesValue, GetUnlockAllModesEnabled());
            UpdateToggleValue(qolUnlockPantheonsValue, GetUnlockPantheonsEnabled());
            UpdateToggleValue(qolUnlockRadianceValue, GetUnlockRadianceEnabled());
            UpdateToggleValue(qolUnlockRadiantValue, GetUnlockRadiantEnabled());
            UpdateToggleValue(qolInvincibleIndicatorValue, GetInvincibleIndicatorEnabled());
            UpdateToggleValue(qolScreenShakeValue, GetScreenShakeEnabled());
            UpdateKeybindValue(nailDamageCheckKeyValue, GetNailDamageCheckKeyLabel());
            UpdateQolInteractivity();
        }

        private void RefreshMenuAnimationUi()
        {
            UpdateToggleValue(menuAnimEnableValue, GetMenuAnimationMasterEnabled());
            UpdateToggleIcon(menuAnimEnableIcon, GetMenuAnimationMasterEnabled());
            UpdateToggleValue(menuAnimDoorDefaultValue, GetDoorDefaultBeginEnabled());
            UpdateToggleValue(menuAnimFasterLoadsValue, GetFasterLoadsEnabled());
            UpdateToggleValue(menuAnimFastMenusValue, GetFastMenusEnabled());
            UpdateToggleValue(menuAnimFastTextValue, GetFastTextEnabled());
            UpdateToggleValue(menuAnimAutoSkipValue, Modules.QoL.SkipCutscenes.AutoSkipCinematics);
            UpdateToggleValue(menuAnimAllowSkippingValue, Modules.QoL.SkipCutscenes.AllowSkippingNonskippable);
            UpdateToggleValue(menuAnimSkipWithoutPromptValue, Modules.QoL.SkipCutscenes.SkipCutscenesWithoutPrompt);
            UpdateMenuAnimationInteractivity();
        }

        private void RefreshBossAnimationUi()
        {
            UpdateToggleValue(bossAnimEnableValue, GetBossAnimationMasterEnabled());
            UpdateToggleIcon(bossAnimEnableIcon, GetBossAnimationMasterEnabled());
            UpdateToggleValue(bossAnimHallOfGodsValue, Modules.QoL.SkipCutscenes.HallOfGodsStatues);
            UpdateToggleValue(bossAnimAbsoluteRadianceValue, Modules.QoL.SkipCutscenes.AbsoluteRadiance);
            UpdateToggleValue(bossAnimPantheonVEndingValue, Modules.QoL.SkipCutscenes.PantheonVEnding);
            UpdateToggleValue(bossAnimPureVesselValue, Modules.QoL.SkipCutscenes.PureVesselRoar);
            UpdateToggleValue(bossAnimGrimmNightmareValue, Modules.QoL.SkipCutscenes.GrimmNightmare);
            UpdateToggleValue(bossAnimGreyPrinceValue, Modules.QoL.SkipCutscenes.GreyPrinceZote);
            UpdateToggleValue(bossAnimCollectorValue, Modules.QoL.SkipCutscenes.Collector);
            UpdateToggleValue(bossAnimSoulMasterValue, Modules.QoL.SkipCutscenes.SoulMasterPhaseTransitionSkip);
            UpdateToggleValue(bossAnimCollectorRoarValue, GetCollectorRoarEnabled());
            UpdateBossAnimationInteractivity();
        }

        private void RefreshZoteHelperUi()
        {
            Module? module = GetZoteHelperModule();
            UpdateToggleValue(zoteHelperToggleValue, module?.Enabled ?? false);
            UpdateToggleIcon(zoteHelperToggleIcon, module?.Enabled ?? false);
            UpdateToggleValue(zoteUseCustomBossHpValue, Modules.BossChallenge.ZoteHelper.zoteUseCustomBossHp);
            UpdateIntInputValue(zoteBossHpField, Modules.BossChallenge.ZoteHelper.zoteBossHp);
            UpdateToggleValue(zoteImmortalValue, Modules.BossChallenge.ZoteHelper.zoteImmortal);
            UpdateToggleValue(zoteSpawnFlyingValue, Modules.BossChallenge.ZoteHelper.zoteSpawnFlying);
            UpdateToggleValue(zoteSpawnHoppingValue, Modules.BossChallenge.ZoteHelper.zoteSpawnHopping);
            UpdateToggleValue(zoteUseCustomFlyingHpValue, Modules.BossChallenge.ZoteHelper.zoteUseCustomFlyingHp);
            UpdateIntInputValue(zoteSummonFlyingHpField, Modules.BossChallenge.ZoteHelper.zoteSummonFlyingHp);
            UpdateToggleValue(zoteUseCustomHoppingHpValue, Modules.BossChallenge.ZoteHelper.zoteUseCustomHoppingHp);
            UpdateIntInputValue(zoteSummonHoppingHpField, Modules.BossChallenge.ZoteHelper.zoteSummonHoppingHp);
            UpdateToggleValue(zoteUseCustomSummonLimitValue, Modules.BossChallenge.ZoteHelper.zoteUseCustomSummonLimit);
            UpdateIntInputValue(zoteSummonLimitField, Modules.BossChallenge.ZoteHelper.zoteSummonLimit);
            UpdateToggleValue(zoteDoubleSummonsValue, Modules.BossChallenge.ZoteHelper.zoteDoubleSummons);
            UpdateToggleValue(zoteAttackSummonsOnlyOnStartValue, Modules.BossChallenge.ZoteHelper.zoteAttackSummonsOnlyOnStart);

            if (bossGpzValue != null)
            {
                bossGpzValue.text = GetBossGpzLabel();
            }

            UpdateZoteHelperInteractivity();
        }

        private void RefreshGruzHelperUi()
        {
            Module? module = GetGruzMotherHelperModule();
            UpdateToggleValue(gruzHelperToggleValue, module?.Enabled ?? false);
            UpdateToggleIcon(gruzHelperToggleIcon, module?.Enabled ?? false);
            UpdateToggleValue(gruzP5HpValue, Modules.BossChallenge.GruzMotherHelper.gruzP5Hp);
            UpdateToggleValue(gruzUseMaxHpValue, Modules.BossChallenge.GruzMotherHelper.gruzUseMaxHp);
            UpdateIntInputValue(gruzMaxHpField, Modules.BossChallenge.GruzMotherHelper.gruzMaxHp);

            UpdateGruzHelperInteractivity();
        }

        private void RefreshHornetHelperUi()
        {
            Module? module = GetHornetProtectorHelperModule();
            UpdateToggleValue(hornetHelperToggleValue, module?.Enabled ?? false);
            UpdateToggleIcon(hornetHelperToggleIcon, module?.Enabled ?? false);
            UpdateToggleValue(hornetP5HpValue, Modules.BossChallenge.HornetProtectorHelper.hornetP5Hp);
            UpdateToggleValue(hornetUseMaxHpValue, Modules.BossChallenge.HornetProtectorHelper.hornetUseMaxHp);
            UpdateIntInputValue(hornetMaxHpField, Modules.BossChallenge.HornetProtectorHelper.hornetMaxHp);

            UpdateHornetHelperInteractivity();
        }

        private void RefreshMawlekHelperUi()
        {
            Module? module = GetBroodingMawlekHelperModule();
            UpdateToggleValue(mawlekHelperToggleValue, module?.Enabled ?? false);
            UpdateToggleIcon(mawlekHelperToggleIcon, module?.Enabled ?? false);
            UpdateToggleValue(mawlekP5HpValue, Modules.BossChallenge.BroodingMawlekHelper.mawlekP5Hp);
            UpdateToggleValue(mawlekUseMaxHpValue, Modules.BossChallenge.BroodingMawlekHelper.mawlekUseMaxHp);
            UpdateIntInputValue(mawlekMaxHpField, Modules.BossChallenge.BroodingMawlekHelper.mawlekMaxHp);

            UpdateMawlekHelperInteractivity();
        }

        private void RefreshMassiveMossHelperUi()
        {
            Module? module = GetMassiveMossChargerHelperModule();
            UpdateToggleValue(massiveMossHelperToggleValue, module?.Enabled ?? false);
            UpdateToggleIcon(massiveMossHelperToggleIcon, module?.Enabled ?? false);
            UpdateToggleValue(massiveMossP5HpValue, Modules.BossChallenge.MassiveMossChargerHelper.massiveMossP5Hp);
            UpdateToggleValue(massiveMossUseMaxHpValue, Modules.BossChallenge.MassiveMossChargerHelper.massiveMossUseMaxHp);
            UpdateIntInputValue(massiveMossMaxHpField, Modules.BossChallenge.MassiveMossChargerHelper.massiveMossMaxHp);

            UpdateMassiveMossHelperInteractivity();
        }

        private void RefreshCrystalGuardianHelperUi()
        {
            Module? module = GetCrystalGuardianHelperModule();
            UpdateToggleValue(crystalGuardianHelperToggleValue, module?.Enabled ?? false);
            UpdateToggleIcon(crystalGuardianHelperToggleIcon, module?.Enabled ?? false);
            UpdateToggleValue(crystalGuardianP5HpValue, Modules.BossChallenge.CrystalGuardianHelper.crystalGuardianP5Hp);
            UpdateToggleValue(crystalGuardianUseMaxHpValue, Modules.BossChallenge.CrystalGuardianHelper.crystalGuardianUseMaxHp);
            UpdateIntInputValue(crystalGuardianMaxHpField, Modules.BossChallenge.CrystalGuardianHelper.crystalGuardianMaxHp);

            UpdateCrystalGuardianHelperInteractivity();
        }

        private void RefreshEnragedGuardianHelperUi()
        {
            Module? module = GetEnragedGuardianHelperModule();
            UpdateToggleValue(enragedGuardianHelperToggleValue, module?.Enabled ?? false);
            UpdateToggleIcon(enragedGuardianHelperToggleIcon, module?.Enabled ?? false);
            UpdateToggleValue(enragedGuardianP5HpValue, Modules.BossChallenge.EnragedGuardianHelper.enragedGuardianP5Hp);
            UpdateToggleValue(enragedGuardianUseMaxHpValue, Modules.BossChallenge.EnragedGuardianHelper.enragedGuardianUseMaxHp);
            UpdateIntInputValue(enragedGuardianMaxHpField, Modules.BossChallenge.EnragedGuardianHelper.enragedGuardianMaxHp);

            UpdateEnragedGuardianHelperInteractivity();
        }

        private void RefreshHornetSentinelHelperUi()
        {
            Module? module = GetHornetSentinelHelperModule();
            UpdateToggleValue(hornetSentinelHelperToggleValue, module?.Enabled ?? false);
            UpdateToggleIcon(hornetSentinelHelperToggleIcon, module?.Enabled ?? false);
            UpdateToggleValue(hornetSentinelP5HpValue, Modules.BossChallenge.HornetSentinelHelper.hornetSentinelP5Hp);
            UpdateToggleValue(hornetSentinelUseMaxHpValue, Modules.BossChallenge.HornetSentinelHelper.hornetSentinelUseMaxHp);
            UpdateIntInputValue(hornetSentinelMaxHpField, Modules.BossChallenge.HornetSentinelHelper.hornetSentinelMaxHp);
            UpdateToggleValue(hornetSentinelUseCustomPhaseValue, Modules.BossChallenge.HornetSentinelHelper.hornetSentinelUseCustomPhase);
            UpdateIntInputValue(hornetSentinelPhase2HpField, Modules.BossChallenge.HornetSentinelHelper.hornetSentinelPhase2Hp);

            UpdateHornetSentinelHelperInteractivity();
        }

        private void RefreshQuickMenuSettingsUi()
        {
            foreach (KeyValuePair<string, Text> entry in quickSettingsToggleValues)
            {
                UpdateToggleValue(entry.Value, IsQuickMenuItemVisible(entry.Key));
                QuickMenuItemDefinition? def = GetOrderedQuickMenuDefinitions()
                    .FirstOrDefault(item => item.Id == entry.Key);
                if (def != null)
                {
                    Transform? row = entry.Value.transform.parent;
                    Text? label = row?.Find("Label")?.GetComponent<Text>();
                    if (label != null)
                    {
                        label.text = GetQuickMenuSettingsLabel(def);
                    }
                }
            }

            foreach (KeyValuePair<string, Text> entry in quickSettingsHotkeyValues)
            {
                UpdateKeybindValue(entry.Value, GetOverlayHotkeyLabel(entry.Key));
            }

            int opacity = GetQuickMenuOpacity();
            UpdateFloatValueText(quickMenuOpacityValue, opacity, 0);
            if (quickMenuOpacitySlider != null && Math.Abs(quickMenuOpacitySlider.value - opacity) > 0.0001f)
            {
                quickMenuOpacitySlider.value = opacity;
            }
        }

        private void UpdateDreamshieldSliderState()
        {
            bool enabled = Modules.QoL.DreamshieldStartAngle.startAngleEnabled;
            if (dreamshieldDelaySlider != null)
            {
                dreamshieldDelaySlider.interactable = enabled;
            }

            if (dreamshieldSpeedSlider != null)
            {
                dreamshieldSpeedSlider.interactable = enabled;
            }
        }
    }
}
