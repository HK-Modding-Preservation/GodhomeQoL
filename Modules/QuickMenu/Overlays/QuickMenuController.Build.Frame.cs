using UnityEngine;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController : MonoBehaviour
    {
        private GameObject CreateOverlayFrame(
            string rootName,
            int sortOrder,
            string panelName,
            float panelHeight,
            string titleText,
            int titleFontSize,
            float titleY,
            out GameObject panel,
            out RectTransform titleRect)
        {
            GameObject root = new GameObject(rootName);
            root.transform.SetParent(transform, false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            root.AddComponent<GraphicRaycaster>();

            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            GameObject dim = new GameObject("Dim");
            dim.transform.SetParent(root.transform, false);
            RectTransform dimRect = dim.AddComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;

            Image dimImage = dim.AddComponent<Image>();
            dimImage.color = OverlayDimColor;

            panel = new GameObject(panelName);
            panel.transform.SetParent(dim.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(PanelWidth, panelHeight);

            Image panelImage = panel.AddComponent<Image>();
            StyleHkPanel(panelImage);

            Text title = CreateText(panel.transform, "Title", titleText, titleFontSize, TextAnchor.MiddleCenter);
            StyleHkTitle(title);
            titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, titleY);
            titleRect.sizeDelta = new Vector2(RowWidth, 60f);
            return root;
        }

        private RectTransform CreateOverlayContent(
            GameObject panel,
            float panelHeight,
            RectTransform titleRect,
            out float resetY,
            out float backY,
            out float topOffset,
            out float viewHeight)
        {
            resetY = GetFixedResetY(panelHeight);
            backY = GetFixedBackY(panelHeight);
            topOffset = GetScrollTopOffset(panelHeight, titleRect);
            float bottomOffset = GetScrollBottomOffset(panelHeight, resetY);
            viewHeight = Mathf.Max(0f, panelHeight - topOffset - bottomOffset);
            return CreateScrollContent(panel.transform, PanelWidth, panelHeight, topOffset, bottomOffset);
        }
    }
}
