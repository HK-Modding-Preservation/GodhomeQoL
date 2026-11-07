using System.IO;
using InControl;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController : MonoBehaviour
    {
        private void UpdateGearSwitcherHeaderLayout()
        {
            if (gearSwitcherTitleRect != null)
            {
                gearSwitcherTitleRect.anchoredPosition = new Vector2(0f, GearSwitcherTitleY);
                gearSwitcherTitleRect.sizeDelta = new Vector2(RowWidth, 60f);
            }

            if (gearSwitcherHintRect != null)
            {
                gearSwitcherHintRect.anchoredPosition = new Vector2(0f, GearSwitcherHintY);
                gearSwitcherHintRect.sizeDelta = new Vector2(RowWidth, GearSwitcherHintHeight);
            }
        }

        private void BuildGearSwitcherOverlayUi()
        {
            gearSwitcherRoot = new GameObject("GearSwitcherOverlayCanvas");
            gearSwitcherRoot.transform.SetParent(transform, false);

            Canvas canvas = gearSwitcherRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = GearSwitcherCanvasSortOrder;

            CanvasScaler scaler = gearSwitcherRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            gearSwitcherRoot.AddComponent<GraphicRaycaster>();

            CanvasGroup group = gearSwitcherRoot.AddComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            GameObject dim = new GameObject("Dim");
            dim.transform.SetParent(gearSwitcherRoot.transform, false);
            RectTransform dimRect = dim.AddComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;

            Image dimImage = dim.AddComponent<Image>();
            dimImage.color = OverlayDimColor;

            GameObject panel = new GameObject("GearSwitcherPanel");
            panel.transform.SetParent(dim.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            Image panelImage = panel.AddComponent<Image>();
            StyleHkPanel(panelImage);

            Text title = CreateText(panel.transform, "Title", "Modules/GearSwitcher".Localize(), 52, TextAnchor.MiddleCenter);
            StyleHkTitle(title);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(RowWidth, 60f);

            Text hint = CreateText(panel.transform, "Hint", "The mod effects apply as soon as you are standing on the ground", 22, TextAnchor.MiddleCenter);
            hint.color = new Color(1f, 1f, 1f, 0.75f);
            RectTransform hintRect = hint.rectTransform;
            hintRect.anchorMin = new Vector2(0.5f, 0.5f);
            hintRect.anchorMax = new Vector2(0.5f, 0.5f);
            hintRect.pivot = new Vector2(0.5f, 0.5f);
            hintRect.sizeDelta = new Vector2(RowWidth, GearSwitcherHintHeight);

            gearSwitcherTitleRect = titleRect;
            gearSwitcherHintRect = hintRect;
            UpdateGearSwitcherHeaderLayout();

            float panelHeight = PanelHeight;
            float resetY = GetFixedResetY(panelHeight);
            float resetGearY = resetY + (FixedResetOffset - FixedBackOffset);
            float backY = GetFixedBackY(panelHeight);
            float topOffset = GetScrollTopOffset(panelHeight, hintRect);
            float bottomOffset = GetScrollBottomOffset(panelHeight, resetGearY);
            float viewHeight = Mathf.Max(0f, panelHeight - topOffset - bottomOffset);
            RectTransform content = CreateScrollContent(panel.transform, PanelWidth, panelHeight, topOffset, bottomOffset);
            gearSwitcherContent = content;

            float rowY = GetRowStartY(panelHeight, GearSwitcherContentRowStartY, topOffset) + GearSwitcherContentTopPadding;
            float lastY = rowY;

            CreateToggleRowWithIcon(
                content,
                "GearSwitcherEnableRow",
                "Enable GearSwitcher",
                rowY,
                GetGearSwitcherEnabled,
                SetGearSwitcherEnabled,
                out gearSwitcherEnableValue,
                out gearSwitcherEnableIcon
            );

            lastY = rowY;
            rowY += GearSwitcherFirstGroupSpacing;

            CreateGearSwitcherSpellsRow(content, rowY);

            lastY = rowY;
            rowY += GearSwitcherGroupSpacing;
            CreateGearSwitcherNailArtsRow(content, rowY);

            lastY = rowY;
            rowY += GearSwitcherGroupSpacing;
            CreateGearSwitcherCloakRow(content, rowY);

            lastY = rowY;
            rowY += GearSwitcherGroupSpacing;
            CreateGearSwitcherBindingsRow(content, rowY);

            lastY = rowY;
            rowY += GearSwitcherGroupSpacing;
            CreateGearSwitcherHpMaskRow(content, rowY);

            lastY = rowY;
            rowY += GearSwitcherHpSoulSpacing;
            CreateGearSwitcherSoulVesselRow(content, rowY);

            lastY = rowY;
            rowY += GearSwitcherAfterGroupsSpacing;
            CreateGearSwitcherNaillessRow(content, rowY);

            lastY = rowY;
            float charmPromptSpacing = GearSwitcherPromptSpacing;
            rowY += charmPromptSpacing;
            CreateGearSwitcherCharmPromptRow(content, rowY);

            lastY = rowY;
            rowY += charmPromptSpacing;
            CreateSelectRow(
                content,
                "GearSwitcherPresetRow",
                "GearSwitcher/SelectedPreset".Localize(),
                rowY,
                GetSelectedPresetLabel,
                OnGearSwitcherPresetClicked,
                out gearSwitcherSelectedPresetValue
            );

            lastY = rowY;
            rowY += GearSwitcherRowSpacing;
            CreateAdjustInputRow(
                content,
                "GearSwitcherNailDamageRow",
                "GearSwitcher/NailDamage".Localize(),
                rowY,
                () => GetSelectedPreset().NailDamage,
                SetSelectedPresetNailDamage,
                -99999,
                99999,
                4,
                out gearSwitcherNailDamageField
            );

            lastY = rowY;
            rowY += GearSwitcherRowSpacing;
            CreateReadOnlyValueRow(
                content,
                "GearSwitcherBaseNailDamageRow",
                "GearSwitcher/BaseNailDamage".Localize(),
                rowY,
                GetGearSwitcherBaseNailDamageDisplay,
                out gearSwitcherBaseNailDamageValue
            );
            CreateGearSwitcherBaseNailDamageEffectIcons();

            lastY = rowY;
            rowY += GearSwitcherRowSpacing;
            CreateAdjustInputRow(
                content,
                "GearSwitcherCharmSlotsRow",
                "GearSwitcher/CharmSlots".Localize(),
                rowY,
                () => GetSelectedPreset().CharmSlots,
                SetSelectedPresetCharmSlots,
                3,
                999,
                1,
                out gearSwitcherCharmSlotsField
            );
            lastY = rowY;

            rowY += GearSwitcherRowSpacing;
            CreateAdjustInputRow(
                content,
                "GearSwitcherMainSoulGainRow",
                "GearSwitcher/MainSoulGain".Localize(),
                rowY,
                () => GetSelectedPreset().MainSoulGain,
                SetSelectedPresetMainSoulGain,
                0,
                198,
                11,
                out gearSwitcherMainSoulGainField
            );
            lastY = rowY;

            rowY += GearSwitcherRowSpacing;
            CreateAdjustInputRow(
                content,
                "GearSwitcherReserveSoulGainRow",
                "GearSwitcher/ReserveSoulGain".Localize(),
                rowY,
                () => GetSelectedPreset().ReserveSoulGain,
                SetSelectedPresetReserveSoulGain,
                0,
                198,
                6,
                out gearSwitcherReserveSoulGainField
            );
            lastY = rowY;

            SetScrollContentHeight(content, viewHeight, lastY, RowHeight);
            CreateButtonRow(panel.transform, "GearSwitcherResetGearRow", "Reset Gear", resetGearY, OnGearSwitcherResetGearClicked);
            gearSwitcherResetGearRow = panel.transform.Find("GearSwitcherResetGearRow")?.gameObject;
            CreateButtonRow(panel.transform, "GearSwitcherResetRow", "GearSwitcher/Reset".Localize(), resetY, OnGearSwitcherResetDefaultsClicked);
            CreateButtonRow(panel.transform, "GearSwitcherBackRow", "Back", backY, OnGearSwitcherBackClicked);

            CreateGearSwitcherResetConfirm(panel.transform);
            SetGearSwitcherResetConfirmVisible(false);
        }

        private void BuildGearSwitcherCharmCostOverlayUi()
        {
            gearSwitcherCharmCostGlows.Clear();
            gearSwitcherCharmCostHighlightEntries.Clear();

            gearSwitcherCharmCostRoot = CreateOverlayFrame("GearSwitcherCharmCostOverlayCanvas", GearSwitcherCharmCostCanvasSortOrder, "GearSwitcherCharmCostPanel", PanelHeight, "GearSwitcher/CharmCosts".Localize(), 52, 210f, out GameObject panel, out RectTransform titleRect);

            float panelHeight = PanelHeight;
            float backY = GetFixedBackY(panelHeight);
            float buttonSpacing = 10f;
            float resetY = backY + (ButtonRowHeight + buttonSpacing) * 2f;
            gearSwitcherCharmNotchText = null;
            Text? charmNotchInfo = EnsureGearSwitcherCharmNotchText();
            float topOffset = GetScrollTopOffset(panelHeight, charmNotchInfo != null ? charmNotchInfo.rectTransform : titleRect);
            float bottomOffset = GetScrollBottomOffset(panelHeight, resetY);
            float viewHeight = Mathf.Max(0f, panelHeight - topOffset - bottomOffset);
            RectTransform content = CreateScrollContent(panel.transform, PanelWidth, panelHeight, topOffset, bottomOffset);

            float rowY = GetRowStartY(panelHeight, RowStartY, topOffset) + GearSwitcherCharmCostTopPadding;
            float lastY = rowY;

            Transform row = CreateGearSwitcherCharmCostsRow(content, "GearSwitcherCharmCostsRow1", rowY);
            for (int i = 0; i < CharmCostDefinitions.Length; i++)
            {
                if (i > 0 && i % 5 == 0)
                {
                    rowY += GearSwitcherCharmCostRowSpacing;
                    row = CreateGearSwitcherCharmCostsRow(content, "GearSwitcherCharmCostsRow" + (i / 5 + 1), rowY);
                }
                CharmCostDefinition definition = CharmCostDefinitions[i];
                int index = i;
                CreateCharmCostColumn(row, definition.Key, CharmCostColumnX(i % 5), definition.SpriteFile, definition.Key, ref gearSwitcherCharmCostValues[i], delegate(int delta)
                {
                    AdjustCharmCost(index, delta);
                });
            }
            gearSwitcherCarefreeIcon = CreateCharmCostColumn(
                row,
                "CarefreeMelody",
                CharmCostColumnX(3),
                "Carefree Melody.png",
                "CarefreeMelody",
                ref gearSwitcherCarefreeMelodyCostValue,
                AdjustCarefreeMelodyCost,
                OnGearSwitcherCarefreeToggle,
                false);
            CreateCharmCostAdjustButtons(row, "CarefreeMelody", CharmCostColumnX(3), AdjustCarefreeMelodyCost);
            gearSwitcherVoidHeartIcon = CreateCharmCostColumn(
                row,
                "VoidHeart",
                CharmCostColumnX(4),
                "Void Heart.png",
                "VoidHeart",
                ref gearSwitcherVoidHeartCostValue,
                AdjustVoidHeartCost,
                OnGearSwitcherVoidHeartToggle,
                false);
            CreateCharmCostAdjustButtons(row, "VoidHeart", CharmCostColumnX(4), AdjustVoidHeartCost);
            AddCharmSwapBadge(gearSwitcherCarefreeIcon);
            AddCharmSwapBadge(gearSwitcherVoidHeartIcon);
            lastY = rowY;

            RegisterGearSwitcherCharmCostHighlights();

            SetScrollContentHeight(content, viewHeight, lastY, GearSwitcherCharmCostRowHeight);
            CreateButtonRow(panel.transform, "GearSwitcherCharmCostResetRow", "Reset Defaults Cost", resetY, OnGearSwitcherCharmCostResetDefaultsClicked);
            CreateButtonRow(panel.transform, "GearSwitcherCharmCostAheRow", "AHE Charm Costs", backY + ButtonRowHeight + buttonSpacing, OnGearSwitcherCharmCostAheClicked);
            CreateButtonRow(panel.transform, "GearSwitcherCharmCostBackRow", "Back", backY, OnGearSwitcherCharmCostBackClicked);
        }

        private void BuildGearSwitcherPresetOverlayUi()
        {
            gearSwitcherPresetEntries.Clear();
            gearSwitcherPresetDeleteTargetName = null;
            gearSwitcherPresetDeleteVisible = false;

            gearSwitcherPresetRoot = CreateOverlayFrame("GearSwitcherPresetCanvas", GearSwitcherPresetCanvasSortOrder, "GearSwitcherPresetPanel", PanelHeight, "GearSwitcher/SelectedPreset".Localize(), 52, 210f, out GameObject panel, out RectTransform titleRect);

            float panelHeight = PanelHeight;
            float createY = GetFixedResetY(panelHeight);
            float backY = GetFixedBackY(panelHeight);
            float topOffset = GetScrollTopOffset(panelHeight, titleRect);
            float bottomOffset = GetScrollBottomOffset(panelHeight, createY);
            float viewHeight = Mathf.Max(0f, panelHeight - topOffset - bottomOffset);
            RectTransform content = CreateScrollContent(panel.transform, PanelWidth, panelHeight, topOffset, bottomOffset);

            float rowY = GetRowStartY(panelHeight, RowStartY, topOffset);
            float lastY = rowY;

            string[] presets = GetGearSwitcherPresetOptions();
            for (int i = 0; i < presets.Length; i++)
            {
                CreateGearSwitcherPresetRow(content, i, presets[i], rowY);
                lastY = rowY;
                rowY += RowSpacing;
            }

            SetScrollContentHeight(content, viewHeight, lastY, RowHeight);
            CreateButtonRow(panel.transform, "GearSwitcherPresetCreateRow", "Create Preset", createY, OnGearSwitcherPresetCreateClicked);
            CreateButtonRow(panel.transform, "GearSwitcherPresetBackRow", "Back", backY, OnGearSwitcherPresetBackClicked);
            CreateGearSwitcherPresetDeleteConfirm(panel.transform);
            SetGearSwitcherPresetDeleteVisible(false);
        }
    }
}
