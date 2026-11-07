using System.IO;
using InControl;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController : MonoBehaviour
    {
        private void BuildFpsBoostOverlayUi()
        {
            fpsBoostRoot = new GameObject("FpsBoostOverlayCanvas");
            fpsBoostRoot.transform.SetParent(transform, false);

            Canvas canvas = fpsBoostRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = FpsBoostCanvasSortOrder;

            CanvasScaler scaler = fpsBoostRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            fpsBoostRoot.AddComponent<GraphicRaycaster>();

            CanvasGroup group = fpsBoostRoot.AddComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            GameObject dim = new GameObject("Dim");
            dim.transform.SetParent(fpsBoostRoot.transform, false);
            RectTransform dimRect = dim.AddComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;

            Image dimImage = dim.AddComponent<Image>();
            dimImage.color = OverlayDimColor;

            GameObject panel = new GameObject("FpsBoostPanel");
            panel.transform.SetParent(dim.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            Image panelImage = panel.AddComponent<Image>();
            StyleHkPanel(panelImage);

            Text title = CreateText(panel.transform, "Title", "Modules/FpsBoost".Localize(), 52, TextAnchor.MiddleCenter);
            StyleHkTitle(title);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 210f);
            titleRect.sizeDelta = new Vector2(RowWidth, 60f);

            float panelHeight = PanelHeight;
            float resetY = GetFixedResetY(panelHeight);
            float backY = GetFixedBackY(panelHeight);
            float topOffset = GetScrollTopOffset(panelHeight, titleRect);
            float bottomOffset = GetScrollBottomOffset(panelHeight, resetY);
            float viewHeight = Mathf.Max(0f, panelHeight - topOffset - bottomOffset);
            RectTransform content = CreateScrollContent(panel.transform, PanelWidth, panelHeight, topOffset, bottomOffset);
            fpsBoostContent = content;

            float rowY = GetRowStartY(panelHeight, RowStartY, topOffset);
            float lastY = rowY;
            CreateToggleRowWithIcon(
                content,
                "FpsBoostEnableRow",
                "Settings/FpsBoost/Enable".Localize(),
                rowY,
                GetFpsBoostEnabled,
                SetFpsBoostEnabled,
                out fpsBoostToggleValue,
                out fpsBoostToggleIcon
            );

            lastY = rowY;
            rowY += FpsBoostRowSpacing;
            CreateToggleRow(
                content,
                "FpsBoostBlurRow",
                "Settings/FpsBoost/NoBlur".Localize(),
                rowY,
                () => Modules.Performance.FpsBoost.GetOption(Modules.Performance.FpsBoost.Option.Blur),
                value => SetFpsBoostOption(Modules.Performance.FpsBoost.Option.Blur, value),
                out fpsBoostBlurValue
            );

            lastY = rowY;
            rowY += FpsBoostRowSpacing;
            CreateToggleRow(
                content,
                "FpsBoostFogRow",
                "Settings/FpsBoost/NoFog".Localize(),
                rowY,
                () => Modules.Performance.FpsBoost.GetOption(Modules.Performance.FpsBoost.Option.Fog),
                value => SetFpsBoostOption(Modules.Performance.FpsBoost.Option.Fog, value),
                out fpsBoostFogValue
            );

            lastY = rowY;
            rowY += FpsBoostRowSpacing;
            CreateToggleRow(
                content,
                "FpsBoostHazeRow",
                "Settings/FpsBoost/NoHaze".Localize(),
                rowY,
                () => Modules.Performance.FpsBoost.GetOption(Modules.Performance.FpsBoost.Option.Haze),
                value => SetFpsBoostOption(Modules.Performance.FpsBoost.Option.Haze, value),
                out fpsBoostHazeValue
            );

            lastY = rowY;
            rowY += FpsBoostRowSpacing;
            CreateToggleRow(
                content,
                "FpsBoostVignetteRow",
                "Settings/FpsBoost/NoVignette".Localize(),
                rowY,
                () => Modules.Performance.FpsBoost.GetOption(Modules.Performance.FpsBoost.Option.Vignette),
                value => SetFpsBoostOption(Modules.Performance.FpsBoost.Option.Vignette, value),
                out fpsBoostVignetteValue
            );

            lastY = rowY;
            rowY += FpsBoostRowSpacing;
            CreateToggleRow(
                content,
                "FpsBoostBloomRow",
                "Settings/FpsBoost/NoBloom".Localize(),
                rowY,
                () => Modules.Performance.FpsBoost.GetOption(Modules.Performance.FpsBoost.Option.Bloom),
                value => SetFpsBoostOption(Modules.Performance.FpsBoost.Option.Bloom, value),
                out fpsBoostBloomValue
            );

            lastY = rowY;
            rowY += FpsBoostRowSpacing;
            CreateToggleRow(
                content,
                "FpsBoostColorCorrectionRow",
                "Settings/FpsBoost/NoColorCorrection".Localize(),
                rowY,
                () => Modules.Performance.FpsBoost.GetOption(Modules.Performance.FpsBoost.Option.ColorCorrection),
                value => SetFpsBoostOption(Modules.Performance.FpsBoost.Option.ColorCorrection, value),
                out fpsBoostColorCorrectionValue
            );

            lastY = rowY;
            rowY += FpsBoostRowSpacing;
            CreateToggleRow(
                content,
                "FpsBoostFilmGrainRow",
                "Settings/FpsBoost/NoFilmGrain".Localize(),
                rowY,
                () => Modules.Performance.FpsBoost.GetOption(Modules.Performance.FpsBoost.Option.FilmGrain),
                value => SetFpsBoostOption(Modules.Performance.FpsBoost.Option.FilmGrain, value),
                out fpsBoostFilmGrainValue
            );

            lastY = rowY;
            rowY += FpsBoostRowSpacing;
            CreateToggleRow(
                content,
                "FpsBoostBrightnessRow",
                "Settings/FpsBoost/SkipNeutralBrightness".Localize(),
                rowY,
                () => Modules.Performance.FpsBoost.GetOption(Modules.Performance.FpsBoost.Option.Brightness),
                value => SetFpsBoostOption(Modules.Performance.FpsBoost.Option.Brightness, value),
                out fpsBoostBrightnessValue
            );

            lastY = rowY;
            rowY += FpsBoostRowSpacing;
            CreateAdjustInputRow(
                content,
                "FpsBoostParticleRow",
                "Settings/FpsBoost/ParticleAmount".Localize(),
                rowY,
                Modules.Performance.FpsBoost.GetParticleAmount,
                SetFpsBoostParticleAmount,
                0,
                100,
                5,
                out fpsBoostParticleField
            );

            lastY = rowY;
            rowY += FpsBoostRowSpacing;
            CreateAdjustInputRow(
                content,
                "FpsBoostRenderScaleRow",
                "Settings/FpsBoost/RenderScale".Localize(),
                rowY,
                Modules.Performance.FpsBoost.GetRenderScale,
                SetFpsBoostRenderScale,
                Modules.Performance.FpsBoost.MinRenderScale,
                100,
                5,
                out fpsBoostRenderScaleField
            );

            lastY = rowY;
            rowY += FpsBoostRowSpacing;
            CreateToggleRow(
                content,
                "FpsBoostMenuCacheRow",
                "Settings/FpsBoost/CacheMenuHotkeys".Localize(),
                rowY,
                () => Modules.Performance.FpsBoost.GetOption(Modules.Performance.FpsBoost.Option.MenuHotkeyCache),
                value => SetFpsBoostOption(Modules.Performance.FpsBoost.Option.MenuHotkeyCache, value),
                out fpsBoostMenuCacheValue
            );

            lastY = rowY;
            rowY += FpsBoostRowSpacing;

            SetScrollContentHeight(content, viewHeight, lastY, RowHeight);
            CreateButtonRow(panel.transform, "FpsBoostResetRow", "Settings/FpsBoost/Reset".Localize(), resetY, OnFpsBoostResetDefaultsClicked);
            CreateButtonRow(panel.transform, "FpsBoostBackRow", "Back", backY, OnFpsBoostBackClicked);
        }
    }
}
