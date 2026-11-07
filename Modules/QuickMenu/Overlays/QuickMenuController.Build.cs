using System.IO;
using InControl;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController : MonoBehaviour
    {
        private void BuildUi()
        {
            BuildStatusUi();
            BuildQuickUi();
            BuildQuickSettingsOverlayUi();
            BuildOverlayUi();
            BuildBossManipulateOverlayUi();
            BuildBossManipulateOtherRoomsOverlayUi();
            BuildCollectorOverlayUi();
            BuildFastReloadOverlayUi();
            BuildDreamshieldOverlayUi();
            BuildShowHpOnDeathOverlayUi();
            BuildMaskDamageOverlayUi();
            BuildFreezeHitboxesOverlayUi();
            BuildFpsBoostOverlayUi();
            BuildSpeedChangerOverlayUi();
            BuildTeleportKitOverlayUi();
            BuildBossChallengeOverlayUi();
            BuildRandomPantheonsOverlayUi();
            BuildTrueBossRushOverlayUi();
            BuildCheatsOverlayUi();
            BuildSettingsPresetsOverlayUi();
            BuildBossHistoryOverlayUi();
            BuildAlwaysFuriousOverlayUi();
            BuildGearSwitcherOverlayUi();
            BuildGearSwitcherCharmCostOverlayUi();
            BuildGearSwitcherPresetOverlayUi();
            BuildQolOverlayUi();
            BuildMenuAnimationOverlayUi();
            BuildBossAnimationOverlayUi();
            BuildZoteHelperOverlayUi();
            BuildGruzHelperOverlayUi();
            BuildGruzMotherP1HelperOverlayUi();
            BuildVengeflyKingP1HelperOverlayUi();
            BuildBroodingMawlekP1HelperOverlayUi();
            BuildNoskP2HelperOverlayUi();
            BuildUumuuP3HelperOverlayUi();
            BuildSoulWarriorP1HelperOverlayUi();
            BuildNoEyesP4HelperOverlayUi();
            BuildMarmuP2HelperOverlayUi();
            BuildXeroP2HelperOverlayUi();
            BuildMarkothP4HelperOverlayUi();
            BuildGorbP1HelperOverlayUi();
            BuildHornetHelperOverlayUi();
            BuildMawlekHelperOverlayUi();
            BuildMassiveMossHelperOverlayUi();
            BuildCrystalGuardianHelperOverlayUi();
            BuildEnragedGuardianHelperOverlayUi();
            BuildHornetSentinelHelperOverlayUi();
            BuildAdditionalGhostHelpersOverlayUi();
        }

        private void BuildQuickUi()
        {
            quickRoot = new GameObject("QuickMenuCanvas");
            quickRoot.transform.SetParent(transform, false);

            Canvas canvas = quickRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = QuickCanvasSortOrder;

            CanvasScaler scaler = quickRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            quickRoot.AddComponent<GraphicRaycaster>();

            CanvasGroup group = quickRoot.AddComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            GameObject panel = CreateQuickPanel(quickRoot.transform);
            quickHandleSprite ??= LoadQuickHandleSprite();
            quickPanelRect = panel.GetComponent<RectTransform>();
            GameObject backplate = CreateQuickPanelBackplate(panel.transform);
            quickPanelBackplateRect = backplate.GetComponent<RectTransform>();
            quickPanelImage = backplate.GetComponent<Image>();
            quickPanelContentRect = CreateQuickPanelContent(panel.transform);
            quickPanelGroup = quickPanelContentRect.GetComponent<CanvasGroup>() ?? quickPanelContentRect.gameObject.AddComponent<CanvasGroup>();
            quickEntries.Clear();

            List<QuickMenuItemDefinition> orderedItems = GetVisibleQuickMenuDefinitions();
            for (int i = 0; i < orderedItems.Count; i++)
            {
                QuickMenuEntry entry = CreateQuickEntry(quickPanelContentRect.transform, orderedItems[i], GetQuickRowY(i));
                quickEntries.Add(entry);
            }

            ApplyQuickMenuLayout();
            ApplyQuickMenuOpacity();
            UpdateFreeMenuLabel();
            UpdateRenameModeLabel();
            UpdateQuickMenuEntryStateColors();
        }

        private void BuildQuickSettingsOverlayUi()
        {
            quickSettingsRoot = CreateOverlayFrame("QuickMenuSettingsOverlayCanvas", QuickSettingsCanvasSortOrder, "QuickMenuSettingsPanel", QuickSettingsPanelHeight, "QuickMenu/Settings".Localize(), 52, 420f, out GameObject panel, out RectTransform titleRect);

            float panelHeight = QuickSettingsPanelHeight;
            float backY = GetFixedBackY(panelHeight);
            float resetY = GetFixedResetY(panelHeight);
            float topOffset = GetScrollTopOffset(panelHeight, titleRect);
            float bottomOffset = GetScrollBottomOffset(panelHeight, resetY);
            float viewHeight = Mathf.Max(0f, panelHeight - topOffset - bottomOffset);
            RectTransform content = CreateScrollContent(panel.transform, PanelWidth, panelHeight, topOffset, bottomOffset);

            quickSettingsToggleValues.Clear();
            quickSettingsHotkeyValues.Clear();

            float rowY = GetRowStartY(panelHeight, QuickSettingsRowStartY, topOffset);
            float lastY = rowY;
            foreach (QuickMenuItemDefinition def in GetOrderedQuickMenuDefinitions())
            {
                if (string.Equals(def.Id, "Settings", StringComparison.Ordinal)
                    || string.Equals(def.Id, "FreeMenu", StringComparison.Ordinal)
                    || string.Equals(def.Id, "RenameMode", StringComparison.Ordinal))
                {
                    continue;
                }

                string label = GetQuickMenuSettingsLabel(def);
                CreateToggleRow(
                    content,
                    $"{def.Id}VisibilityRow",
                    label,
                    rowY,
                    () => IsQuickMenuItemVisible(def.Id),
                    value =>
                    {
                        SetQuickMenuItemVisible(def.Id, value);
                        RebuildQuickMenuEntries();
                    },
                    out Text valueText
                );
                quickSettingsToggleValues[def.Id] = valueText;
                lastY = rowY;
                rowY += RowSpacing;

                if (IsOverlayHotkeySupported(def.Id))
                {
                    string hotkeyLabel = $"{label} Hotkey";
                    CreateKeybindRow(
                        content,
                        $"{def.Id}HotkeyRow",
                        hotkeyLabel,
                        rowY,
                        () => GetOverlayHotkeyLabel(def.Id),
                        () => StartOverlayHotkeyRebind(def.Id),
                        out Text hotkeyValueText
                    );
                    quickSettingsHotkeyValues[def.Id] = hotkeyValueText;
                    lastY = rowY;
                    rowY += RowSpacing;
                }
            }

            CreateKeybindRow(
                content,
                "QuickMenuGamepadButtonRow",
                "QuickMenu/GamepadButton".Localize(),
                rowY,
                GetQuickMenuControllerButtonLabel,
                StartQuickMenuControllerRebind,
                out Text gamepadButtonValue
            );
            quickMenuControllerButtonValue = gamepadButtonValue;
            lastY = rowY;
            rowY += RowSpacing;

            CreateFloatSliderRow(
                content,
                "QuickMenuOpacityRow",
                "QuickMenu/Opacity".Localize(),
                rowY,
                rowY + 40f,
                1f,
                100f,
                GetQuickMenuOpacity(),
                0,
                out quickMenuOpacityValue,
                out quickMenuOpacitySlider
            );
            if (quickMenuOpacitySlider != null)
            {
                quickMenuOpacitySlider.wholeNumbers = true;
                quickMenuOpacitySlider.onValueChanged.AddListener(OnQuickMenuOpacityChanged);
            }

            lastY = rowY + 40f;
            rowY += RowSpacing + 40f;

            SetScrollContentHeight(content, viewHeight, lastY, RowHeight);
            CreateButtonRow(panel.transform, "QuickMenuSettingsResetRow", "Reset Defaults", resetY, OnQuickSettingsResetDefaultsClicked);
            CreateButtonRow(panel.transform, "QuickMenuSettingsBackRow", "Back", backY, OnQuickMenuSettingsBackClicked);
            CreateQuickSettingsResetConfirm(panel.transform);
            SetQuickSettingsResetConfirmVisible(false);
        }

        private void BuildStatusUi()
        {
            statusRoot = new GameObject("StatusCanvas");
            statusRoot.transform.SetParent(transform, false);

            Canvas canvas = statusRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = StatusCanvasSortOrder;

            CanvasScaler scaler = statusRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            Text text = CreateText(statusRoot.transform, "StatusText", string.Empty, 26, TextAnchor.UpperLeft);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(QuickPanelDefaultLeft, QuickPanelDefaultTop);
            rect.sizeDelta = new Vector2(600f, 40f);

            statusText = text;
            StyleHkStatusText(text);
            statusText.gameObject.SetActive(false);
        }

        private void BuildOverlayUi()
        {
            overlayRoot = CreateOverlayFrame("FastSuperDashOverlayCanvas", OverlayCanvasSortOrder, "FastSuperDashPanel", PanelHeight, "Modules/FastSuperDash".Localize(), 52, 210f, out GameObject panel, out RectTransform titleRect);

            float panelHeight = PanelHeight;
            RectTransform content = CreateOverlayContent(panel, PanelHeight, titleRect, out float resetY, out float backY, out float topOffset, out float viewHeight);
            randomPantheonsContent = content;
            fastSuperDashContent = content;

            float rowY = GetRowStartY(panelHeight, RowStartY, topOffset);
            float lastY = rowY;
            CreateToggleRowWithIcon(
                content,
                "ModuleToggleRow",
                "Settings/FastSuperDash/Enable".Localize(),
                rowY,
                GetModuleEnabled,
                SetModuleEnabled,
                out moduleToggleValue,
                out moduleToggleIcon
            );

            lastY = rowY;
            rowY += RowSpacing;
            CreateToggleRow(
                content,
                "InstantToggleRow",
                "Settings/instantSuperDash".Localize(),
                rowY,
                () => Modules.QoL.FastSuperDash.instantSuperDash,
                value =>
                {
                    Modules.QoL.FastSuperDash.instantSuperDash = value;
                    GodhomeQoL.SaveGlobalSettingsSafe();
                },
                out instantToggleValue
            );

            lastY = rowY;
            rowY += RowSpacing;
            CreateToggleRow(
                content,
                "EverywhereToggleRow",
                "Settings/fastSuperDashEverywhere".Localize(),
                rowY,
                () => Modules.QoL.FastSuperDash.fastSuperDashEverywhere,
                value =>
                {
                    Modules.QoL.FastSuperDash.fastSuperDashEverywhere = value;
                    GodhomeQoL.SaveGlobalSettingsSafe();
                },
                out everywhereToggleValue
            );

            rowY += RowSpacing;
            float speedLabelY = rowY;
            CreateSpeedRow(content, speedLabelY, speedLabelY + 40f);
            lastY = speedLabelY + 40f;

            SetScrollContentHeight(content, viewHeight, lastY, RowHeight);

            CreateButtonRow(panel.transform, "FastSuperDashResetRow", "Settings/FastSuperDash/Reset".Localize(), resetY, OnFastSuperDashResetDefaultsClicked);
            CreateButtonRow(panel.transform, "BackRow", "Back", backY, OnOverlayBackClicked);
        }
    }
}
