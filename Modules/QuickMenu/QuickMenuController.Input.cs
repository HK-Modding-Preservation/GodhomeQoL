using System.IO;
using InControl;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController : MonoBehaviour
    {
        private void Update()
        {
            bool wasRebinding = IsAnyRebinding();
            bool suppressHotkeysThisFrame = overlayHotkeySuppressFrames > 0;
            if (overlayHotkeySuppressFrames > 0)
            {
                overlayHotkeySuppressFrames--;
            }
            if (fastReloadVisible)
            {
                HandleFastReloadRebind();
            }

            if (showHpOnDeathVisible)
            {
                HandleShowHpOnDeathRebind();
            }

            if (maskDamageVisible)
            {
                HandleMaskDamageUiRebind();
            }

            if (qolVisible)
            {
                HandleNailDamageCheckRebind();
            }

            if (freezeHitboxesVisible)
            {
                HandleFreezeHitboxesRebind();
            }

            if (cheatsVisible)
            {
                HandleCheatsKillAllRebind();
            }

            if (speedChangerVisible)
            {
                HandleSpeedChangerRebind();
            }

            if (teleportKitVisible)
            {
                HandleTeleportKitRebind();
            }

            if (waitingForFastDreamWarpRebind)
            {
                HandleFastDreamWarpRebind();
            }

            if (waitingForQuickMenuHotkeyRebind)
            {
                HandleQuickMenuHotkeyRebind();
            }

            if (waitingForOverlayHotkeyRebind)
            {
                HandleOverlayHotkeyRebind();
            }

            if (quickVisible)
            {
                HandleQuickMenuRename();
            }

            if (gearSwitcherPresetVisible)
            {
                HandleGearSwitcherPresetRename();
            }

            if (settingsPresetsVisible)
            {
                HandleSettingsPresetRename();
            }

            if (gearSwitcherVisible)
            {
                UpdateGearSwitcherBaseNailDamage();
            }

            bool teleportKitGameplayMenuVisible = IsTeleportKitGameplayMenuVisible();
            bool anyUiVisible = IsAnyUiVisible();
            bool canUseSettingsUiHotkeys = CanUseSettingsUiHotkeys();

            if (!canUseSettingsUiHotkeys && anyUiVisible)
            {
                ToggleMenu();
                anyUiVisible = IsAnyUiVisible();
            }

            if (!wasRebinding
                && !IsAnyRebinding()
                && !IsHotkeyInputBlocked()
                && !suppressHotkeysThisFrame
                && !teleportKitGameplayMenuVisible
                && canUseSettingsUiHotkeys)
            {
                if (!anyUiVisible)
                {
                    HandleFastDreamWarpHotkey();
                    HandleCheatsKillAllHotkey();
                }

                HandleBossHistoryHotkey();
                HandleOverlayHotkeys();
            }

            KeyCode toggleKey = GetQuickMenuToggleKeyForFrame();
            if (!wasRebinding
                && !IsHotkeyInputBlocked()
                && !suppressHotkeysThisFrame
                && !teleportKitGameplayMenuVisible
                && canUseSettingsUiHotkeys
                && toggleKey != KeyCode.None
                && Input.GetKeyDown(toggleKey))
            {
                ToggleMenu();
            }

            HandleQuickMenuGamepad(!wasRebinding
                && !IsHotkeyInputBlocked()
                && !suppressHotkeysThisFrame
                && !teleportKitGameplayMenuVisible
                && canUseSettingsUiHotkeys);

            if (quickVisible || quickSettingsVisible || overlayVisible || collectorVisible || fastReloadVisible || dreamshieldVisible || showHpOnDeathVisible || maskDamageVisible || freezeHitboxesVisible || fpsBoostVisible || speedChangerVisible || teleportKitVisible || bossChallengeVisible || randomPantheonsVisible || trueBossRushVisible || cheatsVisible || alwaysFuriousVisible || gearSwitcherVisible || gearSwitcherCharmCostVisible || gearSwitcherPresetVisible || settingsPresetsVisible || bossHistoryVisible || qolVisible || menuAnimationVisible || bossAnimationVisible || zoteHelperVisible || gruzHelperVisible || hornetHelperVisible || mawlekHelperVisible || massiveMossHelperVisible || crystalGuardianHelperVisible || enragedGuardianHelperVisible || hornetSentinelHelperVisible || IsAnyAdditionalGhostHelperVisible() || bossManipulateVisible || bossManipulateOtherRoomsVisible || gruzMotherP1HelperVisible || vengeflyKingP1HelperVisible || broodingMawlekP1HelperVisible || noskP2HelperVisible || uumuuP3HelperVisible || soulWarriorP1HelperVisible || noEyesP4HelperVisible || marmuP2HelperVisible || xeroP2HelperVisible || markothP4HelperVisible || gorbP1HelperVisible)
            {
                MaintainUiInteraction();
            }

            UpdateStatusMessage();
        }

        private static bool CanUseSettingsUiHotkeys()
        {
            GameManager? manager = GameManager.instance;
            return manager != null && manager.IsGameplayScene();
        }

        private void LateUpdate()
        {
            if (quickVisible || quickSettingsVisible || overlayVisible || collectorVisible || fastReloadVisible || dreamshieldVisible || showHpOnDeathVisible || maskDamageVisible || freezeHitboxesVisible || fpsBoostVisible || speedChangerVisible || teleportKitVisible || bossChallengeVisible || randomPantheonsVisible || trueBossRushVisible || cheatsVisible || alwaysFuriousVisible || gearSwitcherVisible || gearSwitcherCharmCostVisible || gearSwitcherPresetVisible || settingsPresetsVisible || bossHistoryVisible || qolVisible || menuAnimationVisible || bossAnimationVisible || zoteHelperVisible || gruzHelperVisible || hornetHelperVisible || mawlekHelperVisible || massiveMossHelperVisible || crystalGuardianHelperVisible || enragedGuardianHelperVisible || hornetSentinelHelperVisible || IsAnyAdditionalGhostHelperVisible() || bossManipulateVisible || bossManipulateOtherRoomsVisible || gruzMotherP1HelperVisible || vengeflyKingP1HelperVisible || broodingMawlekP1HelperVisible || noskP2HelperVisible || uumuuP3HelperVisible || soulWarriorP1HelperVisible || noEyesP4HelperVisible || marmuP2HelperVisible || xeroP2HelperVisible || markothP4HelperVisible || gorbP1HelperVisible)
            {
                MaintainUiInteraction();
            }

            if (IsOverlayHighlightActive())
            {
                UpdateManualHighlight();
            }
            else
            {
                ClearManualHighlight();
            }
        }

        private bool IsOverlayHighlightActive()
        {
            return quickSettingsVisible
                || overlayVisible
                || collectorVisible
                || fastReloadVisible
                || dreamshieldVisible
                || showHpOnDeathVisible
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

        private void UpdateManualHighlight()
        {
            EventSystem? eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                ClearManualHighlight();
                return;
            }

            RowHighlight? target = null;
            PointerEventData data = GetHoverEventData(eventSystem);
            hoverResults.Clear();
            eventSystem.RaycastAll(data, hoverResults);

            for (int i = 0; i < hoverResults.Count; i++)
            {
                RowHighlight? highlight = GetHighlightFromObject(hoverResults[i].gameObject);
                if (highlight != null)
                {
                    target = highlight;
                    break;
                }
            }

            if (target == null)
            {
                GameObject? selected = eventSystem.currentSelectedGameObject;
                if (selected != null)
                {
                    target = GetHighlightFromObject(selected);
                }
            }

            SetManualHighlight(target);
        }

        private void ClearManualHighlight()
        {
            SetManualHighlight(null);
        }

        private void SetManualHighlight(RowHighlight? highlight)
        {
            if (manualHighlight == highlight)
            {
                return;
            }

            if (manualHighlight != null)
            {
                manualHighlight.SetManualActive(false);
            }

            manualHighlight = highlight;

            if (manualHighlight != null)
            {
                manualHighlight.SetManualActive(true);
            }
        }

        private PointerEventData GetHoverEventData(EventSystem eventSystem)
        {
            if (hoverEventData == null || hoverEventSystem != eventSystem)
            {
                hoverEventData = new PointerEventData(eventSystem);
                hoverEventSystem = eventSystem;
            }

            hoverEventData.position = Input.mousePosition;
            hoverEventData.delta = Vector2.zero;
            hoverEventData.scrollDelta = Vector2.zero;
            hoverEventData.button = PointerEventData.InputButton.Left;
            return hoverEventData;
        }

        private static RowHighlight? GetHighlightFromObject(GameObject obj)
        {
            RowHighlight? highlight = obj.GetComponentInParent<RowHighlight>();
            if (highlight != null)
            {
                return highlight;
            }

            RowHighlightTrigger? trigger = obj.GetComponentInParent<RowHighlightTrigger>();
            return trigger?.Highlight;
        }
    }
}
