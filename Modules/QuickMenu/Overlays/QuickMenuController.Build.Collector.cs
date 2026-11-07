using System.IO;
using InControl;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController : MonoBehaviour
    {
        private void BuildCollectorOverlayUi()
        {
            collectorRoot = new GameObject("CollectorPhasesOverlayCanvas");
            collectorRoot.transform.SetParent(transform, false);

            Canvas canvas = collectorRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CollectorCanvasSortOrder;

            CanvasScaler scaler = collectorRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            collectorRoot.AddComponent<GraphicRaycaster>();

            CanvasGroup group = collectorRoot.AddComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            GameObject dim = new GameObject("Dim");
            dim.transform.SetParent(collectorRoot.transform, false);
            RectTransform dimRect = dim.AddComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;

            Image dimImage = dim.AddComponent<Image>();
            dimImage.color = OverlayDimColor;

            GameObject panel = new GameObject("CollectorPhasesPanel");
            panel.transform.SetParent(dim.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(CollectorPanelWidth, CollectorPanelHeight);

            Image panelImage = panel.AddComponent<Image>();
            StyleHkPanel(panelImage);

            Text title = CreateText(panel.transform, "Title", "Modules/CollectorPhases".Localize(), CollectorTitleFontSize, TextAnchor.MiddleCenter);
            StyleHkTitle(title);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 420f);
            titleRect.sizeDelta = new Vector2(CollectorRowWidth, 60f);

            float panelHeight = CollectorPanelHeight;
            float resetY = GetFixedResetY(panelHeight);
            float backY = GetFixedBackY(panelHeight);
            float topOffset = GetScrollTopOffset(panelHeight, titleRect);
            float bottomOffset = GetScrollBottomOffset(panelHeight, resetY);
            float viewHeight = Mathf.Max(0f, panelHeight - topOffset - bottomOffset);
            RectTransform content = CreateScrollContent(panel.transform, CollectorPanelWidth, panelHeight, topOffset, bottomOffset);
            collectorContent = content;

            float rowY = GetRowStartY(panelHeight, CollectorRowStartY, topOffset);
            float lastY = rowY;
            CreateCollectorToggleRowWithIcon(
                content,
                "CollectorModuleToggleRow",
                "Settings/CollectorHelper/Enable".Localize(),
                rowY,
                GetCollectorPhasesEnabled,
                SetCollectorPhasesEnabled,
                out collectorModuleToggleValue,
                out collectorModuleToggleIcon
            );

            lastY = rowY;
            rowY += CollectorRowSpacing;
            CreateCollectorAdjustRow(
                content,
                "CollectorPhaseRow",
                "Settings/CollectorPhases/CollectorPhase".Localize(),
                rowY,
                () => Modules.CollectorPhases.CollectorPhases.collectorPhase,
                SetCollectorPhase,
                1,
                3,
                1,
                out collectorPhaseValue
            );

            lastY = rowY;
            rowY += CollectorRowSpacing;
            CreateCollectorToggleRow(
                content,
                "CollectorImmortalRow",
                "Settings/CollectorPhases/CollectorImmortal".Localize(),
                rowY,
                () => Modules.CollectorPhases.CollectorPhases.CollectorImmortal,
                SetCollectorImmortalEnabled,
                out collectorImmortalValue
            );

            lastY = rowY;
            rowY += CollectorRowSpacing;
            CreateCollectorToggleRow(
                content,
                "IgnoreInitialJarLimitRow",
                "Settings/CollectorPhases/IgnoreInitialJarLimit".Localize(),
                rowY,
                () => Modules.CollectorPhases.CollectorPhases.IgnoreInitialJarLimit,
                SetCollectorIgnoreInitialJarLimitEnabled,
                out ignoreInitialJarLimitValue
            );

            lastY = rowY;
            rowY += CollectorRowSpacing;
            CreateCollectorToggleRow(
                content,
                "UseCustomPhase2ThresholdRow",
                "Settings/CollectorPhases/UseCustomPhase2Threshold".Localize(),
                rowY,
                () => Modules.CollectorPhases.CollectorPhases.UseCustomPhase2Threshold,
                SetCollectorUseCustomPhase2ThresholdEnabled,
                out useCustomPhase2ThresholdValue
            );

            lastY = rowY;
            rowY += CollectorRowSpacing;
            CreateCollectorAdjustInputRow(
                content,
                "CustomPhase2ThresholdRow",
                "Settings/CollectorPhases/CustomPhase2Threshold".Localize(),
                rowY,
                () => Modules.CollectorPhases.CollectorPhases.CustomPhase2Threshold,
                SetCollectorCustomPhase2Threshold,
                1,
                99999,
                1,
                out customPhase2ThresholdField
            );

            lastY = rowY;
            rowY += CollectorRowSpacing;
            CreateCollectorToggleRow(
                content,
                "CollectorP5HpRow",
                "Settings/CollectorPhases/P5HP".Localize(),
                rowY,
                () => Modules.CollectorPhases.CollectorPhases.collectorP5Hp,
                SetCollectorP5HpEnabled,
                out collectorP5HpValue
            );

            lastY = rowY;
            rowY += CollectorRowSpacing;
            CreateCollectorToggleRow(
                content,
                "UseMaxHpRow",
                "Settings/CollectorPhases/UseMaxHP".Localize(),
                rowY,
                () => Modules.CollectorPhases.CollectorPhases.UseMaxHP,
                SetCollectorUseMaxHpEnabled,
                out useMaxHpValue
            );

            lastY = rowY;
            rowY += CollectorRowSpacing;
            CreateCollectorAdjustInputRow(
                content,
                "CollectorMaxHpRow",
                "Settings/CollectorPhases/CollectorMaxHP".Localize(),
                rowY,
                () => Modules.CollectorPhases.CollectorPhases.collectorMaxHP,
                SetCollectorMaxHp,
                100,
                99999,
                1,
                out collectorMaxHpField
            );

            lastY = rowY;
            rowY += CollectorRowSpacing;
            CreateCollectorAdjustInputRow(
                content,
                "BuzzerHpRow",
                "Settings/CollectorPhases/BuzzerHP".Localize(),
                rowY,
                () => Modules.CollectorPhases.CollectorPhases.buzzerHP,
                SetCollectorBuzzerHp,
                1,
                9999,
                1,
                out buzzerHpField
            );

            lastY = rowY;
            rowY += CollectorRowSpacing;
            CreateCollectorToggleRow(
                content,
                "SpawnBuzzerRow",
                "Settings/CollectorPhases/SpawnBuzzer".Localize(),
                rowY,
                () => Modules.CollectorPhases.CollectorPhases.spawnBuzzer,
                SetCollectorSpawnBuzzerEnabled,
                out spawnBuzzerValue
            );

            lastY = rowY;
            rowY += CollectorRowSpacing;
            CreateCollectorAdjustInputRow(
                content,
                "RollerHpRow",
                "Settings/CollectorPhases/RollerHP".Localize(),
                rowY,
                () => Modules.CollectorPhases.CollectorPhases.rollerHP,
                SetCollectorRollerHp,
                1,
                9999,
                1,
                out rollerHpField
            );

            lastY = rowY;
            rowY += CollectorRowSpacing;
            CreateCollectorToggleRow(
                content,
                "SpawnRollerRow",
                "Settings/CollectorPhases/SpawnRoller".Localize(),
                rowY,
                () => Modules.CollectorPhases.CollectorPhases.spawnRoller,
                SetCollectorSpawnRollerEnabled,
                out spawnRollerValue
            );

            lastY = rowY;
            rowY += CollectorRowSpacing;
            CreateCollectorAdjustInputRow(
                content,
                "SpitterHpRow",
                "Settings/CollectorPhases/SpitterHP".Localize(),
                rowY,
                () => Modules.CollectorPhases.CollectorPhases.spitterHP,
                SetCollectorSpitterHp,
                1,
                9999,
                1,
                out spitterHpField
            );

            lastY = rowY;
            rowY += CollectorRowSpacing;
            CreateCollectorToggleRow(
                content,
                "SpawnSpitterRow",
                "Settings/CollectorPhases/SpawnSpitter".Localize(),
                rowY,
                () => Modules.CollectorPhases.CollectorPhases.spawnSpitter,
                SetCollectorSpawnSpitterEnabled,
                out spawnSpitterValue
            );

            lastY = rowY;
            rowY += CollectorRowSpacing;
            CreateCollectorToggleRow(
                content,
                "DisableSummonLimitRow",
                "Settings/CollectorPhases/DisableSummonLimit".Localize(),
                rowY,
                () => Modules.CollectorPhases.CollectorPhases.DisableSummonLimit,
                SetCollectorDisableSummonLimitEnabled,
                out disableSummonLimitValue
            );

            lastY = rowY;
            rowY += CollectorRowSpacing;
            CreateCollectorAdjustInputRow(
                content,
                "CustomSummonLimitRow",
                "Settings/CollectorPhases/CustomSummonLimit".Localize(),
                rowY,
                () => Modules.CollectorPhases.CollectorPhases.CustomSummonLimit,
                SetCollectorCustomSummonLimit,
                2,
                999,
                1,
                out customSummonLimitField
            );

            lastY = rowY;
            rowY += CollectorRowSpacing;

            SetScrollContentHeight(content, viewHeight, lastY, CollectorRowHeight);
            CreateButtonRow(panel.transform, "CollectorResetRow", "Settings/CollectorPhases/Reset".Localize(), resetY, OnCollectorResetClicked);
            CreateButtonRow(panel.transform, "CollectorBackRow", "Back", backY, OnCollectorBackClicked);
        }
    }
}
