using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController
    {
        private void BeginQuickMenuDrag(QuickMenuEntry? entry, PointerEventData eventData)
        {
            if (entry == null || quickPanelRect == null || !quickVisible || quickRenameMode || renameField != null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    quickPanelRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            draggingQuickEntry = entry;
            draggingQuickIndex = quickEntries.IndexOf(entry);
            draggingQuickOffset = entry.Rect.anchoredPosition - localPoint;
            entry.Rect.SetAsLastSibling();
        }

        private void UpdateQuickMenuDrag(PointerEventData eventData)
        {
            if (draggingQuickEntry == null || quickPanelRect == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    quickPanelRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            if (IsQuickMenuFreeLayoutEnabled())
            {
                Vector2 newPos = localPoint + draggingQuickOffset;
                draggingQuickEntry.Rect.anchoredPosition = ClampQuickMenuFreePosition(newPos);
                return;
            }

            float minY = GetQuickRowY(quickEntries.Count - 1);
            float maxY = GetQuickRowY(0);
            float newY = Mathf.Clamp(localPoint.y + draggingQuickOffset.y, minY, maxY);
            draggingQuickEntry.Rect.anchoredPosition = new Vector2(QuickButtonLeft, newY);

            int newIndex = Mathf.Clamp(
                Mathf.RoundToInt((QuickButtonTop - newY) / (QuickButtonHeight + QuickButtonSpacing)),
                0,
                quickEntries.Count - 1);

            if (newIndex != draggingQuickIndex && draggingQuickIndex >= 0)
            {
                quickEntries.Remove(draggingQuickEntry);
                quickEntries.Insert(newIndex, draggingQuickEntry);
                draggingQuickIndex = newIndex;
                UpdateQuickEntryPositions(false);
            }
        }

        private void EndQuickMenuDrag()
        {
            if (draggingQuickEntry == null)
            {
                return;
            }

            draggingQuickEntry = null;
            draggingQuickIndex = -1;
            if (IsQuickMenuFreeLayoutEnabled())
            {
                SaveQuickMenuPositions();
                return;
            }

            UpdateQuickEntryPositions(true);
            SaveQuickMenuOrder();
        }

        private void CancelQuickMenuDrag()
        {
            if (draggingQuickEntry == null)
            {
                return;
            }

            draggingQuickEntry = null;
            draggingQuickIndex = -1;
            if (IsQuickMenuFreeLayoutEnabled())
            {
                ApplyQuickMenuFreePositions();
                return;
            }

            UpdateQuickEntryPositions(true);
        }

        private void UpdateQuickEntryPositions(bool includeDragging)
        {
            if (IsQuickMenuFreeLayoutEnabled())
            {
                ApplyQuickMenuFreePositions();
                return;
            }

            for (int i = 0; i < quickEntries.Count; i++)
            {
                QuickMenuEntry entry = quickEntries[i];
                if (!includeDragging && entry == draggingQuickEntry)
                {
                    continue;
                }

                entry.Rect.anchoredPosition = new Vector2(QuickButtonLeft, GetQuickRowY(i));
            }
        }

        private void ApplyQuickMenuLayout()
        {
            if (quickPanelRect == null)
            {
                return;
            }

            if (quickPanelBackplateRect != null)
            {
                quickPanelBackplateRect.anchoredPosition = Vector2.zero;
                quickPanelBackplateRect.sizeDelta = new Vector2(QuickPanelWidth, QuickPanelHeight);
            }

            if (IsQuickMenuFreeLayoutEnabled())
            {
                quickPanelRect.anchoredPosition = Vector2.zero;
                quickPanelRect.sizeDelta = new Vector2(QuickPanelFreeWidth, QuickPanelFreeHeight);

                if (quickPanelImage != null)
                {
                    Color color = quickPanelImage.color;
                    quickPanelImage.color = new Color(color.r, color.g, color.b, QuickPanelBackplateAlpha);
                }

                EnsureQuickMenuPositions();
                ApplyQuickMenuFreePositions();
            }
            else
            {
                quickPanelRect.anchoredPosition = new Vector2(QuickPanelDefaultLeft, QuickPanelDefaultTop);
                quickPanelRect.sizeDelta = new Vector2(QuickPanelWidth, QuickPanelHeight);

                if (quickPanelImage != null)
                {
                    Color color = quickPanelImage.color;
                    quickPanelImage.color = new Color(color.r, color.g, color.b, QuickPanelBackplateAlpha);
                }

                UpdateQuickEntryPositions(true);
            }

            ApplyQuickMenuOpacity();
        }

        private void EnsureQuickMenuPositions()
        {
            Dictionary<string, QuickMenuEntryPosition> positions = GodhomeQoL.GlobalSettings.QuickMenuPositions ??= new Dictionary<string, QuickMenuEntryPosition>();
            bool changed = false;

            for (int i = 0; i < quickEntries.Count; i++)
            {
                QuickMenuEntry entry = quickEntries[i];
                if (!positions.ContainsKey(entry.Id))
                {
                    positions[entry.Id] = new QuickMenuEntryPosition(QuickButtonLeft, GetQuickRowY(i));
                    changed = true;
                }
            }

            if (changed)
            {
                GodhomeQoL.SaveGlobalSettingsSafe();
            }
        }

        private void ApplyQuickMenuFreePositions()
        {
            Dictionary<string, QuickMenuEntryPosition> positions = GodhomeQoL.GlobalSettings.QuickMenuPositions ??= new Dictionary<string, QuickMenuEntryPosition>();

            for (int i = 0; i < quickEntries.Count; i++)
            {
                QuickMenuEntry entry = quickEntries[i];
                Vector2 fallback = new(QuickButtonLeft, GetQuickRowY(i));
                if (positions.TryGetValue(entry.Id, out QuickMenuEntryPosition? stored))
                {
                    entry.Rect.anchoredPosition = ClampQuickMenuFreePosition(new Vector2(stored.X, stored.Y));
                }
                else
                {
                    entry.Rect.anchoredPosition = ClampQuickMenuFreePosition(fallback);
                }
            }
        }

        private void SaveQuickMenuPositions()
        {
            Dictionary<string, QuickMenuEntryPosition> positions = new();
            foreach (QuickMenuEntry entry in quickEntries)
            {
                Vector2 pos = entry.Rect.anchoredPosition;
                positions[entry.Id] = new QuickMenuEntryPosition(pos.x, pos.y);
            }

            GodhomeQoL.GlobalSettings.QuickMenuPositions = positions;
            GodhomeQoL.SaveGlobalSettingsSafe();
        }

        private void SaveQuickMenuOrder()
        {
            List<string> order = quickEntries.Select(entry => entry.Id).ToList();
            GodhomeQoL.GlobalSettings.QuickMenuOrder = order;
            GodhomeQoL.SaveGlobalSettingsSafe();
        }

        private void RebuildQuickMenuEntries()
        {
            if (quickPanelRect == null)
            {
                return;
            }

            CancelQuickMenuRename();

            foreach (QuickMenuEntry entry in quickEntries)
            {
                UObject.Destroy(entry.Rect.gameObject);
            }

            quickEntries.Clear();
            quickHandleSprite ??= LoadQuickHandleSprite();

            List<QuickMenuItemDefinition> orderedItems = GetVisibleQuickMenuDefinitions();
            Transform parent = quickPanelContentRect != null ? quickPanelContentRect.transform : quickPanelRect.transform;
            for (int i = 0; i < orderedItems.Count; i++)
            {
                QuickMenuEntry entry = CreateQuickEntry(parent, orderedItems[i], GetQuickRowY(i));
                quickEntries.Add(entry);
            }

            ApplyQuickMenuLayout();
            UpdateFreeMenuLabel();
            UpdateRenameModeLabel();
            UpdateQuickMenuEntryStateColors();
        }

        private Vector2 ClampQuickMenuFreePosition(Vector2 position)
        {
            if (quickPanelRect == null)
            {
                return position;
            }

            float width = quickPanelRect.rect.width;
            float height = quickPanelRect.rect.height;
            float minX = 0f;
            float maxX = Mathf.Max(0f, width - QuickRowWidth);
            float maxY = 0f;
            float minY = -(height - QuickButtonHeight);

            float x = Mathf.Clamp(position.x, minX, maxX);
            float y = Mathf.Clamp(position.y, minY, maxY);
            return new Vector2(x, y);
        }

        private sealed class ScrollContentMarker : MonoBehaviour
        {
        }

        private static RectTransform CreateScrollContent(Transform panel, float panelWidth, float panelHeight, float topOffset, float bottomOffset)
        {
            float viewHeight = Mathf.Max(0f, panelHeight - topOffset - bottomOffset);

            GameObject scrollObj = new("Scroll");
            scrollObj.transform.SetParent(panel, false);

            RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            float topEdgeY = (panelHeight * 0.5f) - topOffset;
            float centerY = topEdgeY - (viewHeight * 0.5f);
            scrollRect.anchoredPosition = new Vector2(0f, centerY);
            scrollRect.sizeDelta = new Vector2(panelWidth, viewHeight);

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.scrollSensitivity = 30f;

            GameObject viewport = new("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.pivot = new Vector2(0.5f, 0.5f);
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0f);
            viewportImage.raycastTarget = true;

            _ = viewport.AddComponent<RectMask2D>();

            GameObject contentObj = new("Content");
            contentObj.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, viewHeight);

            contentObj.AddComponent<ScrollContentMarker>();

            GameObject scrollbarObj = new("Scrollbar");
            scrollbarObj.transform.SetParent(scrollObj.transform, false);
            RectTransform scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.anchoredPosition = new Vector2(-ScrollbarRightPadding, 0f);
            scrollbarRect.sizeDelta = new Vector2(ScrollbarWidth, 0f);

            Image scrollbarImage = scrollbarObj.AddComponent<Image>();
            scrollbarImage.color = new Color(1f, 1f, 1f, 0.12f);

            GameObject handleObj = new("Handle");
            handleObj.transform.SetParent(scrollbarObj.transform, false);
            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(1f, 1f, 1f, 0.55f);

            Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };

            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            StyleHkScrollbar(scrollbarImage, handleImage, handleRect);
            scroll.verticalNormalizedPosition = 1f;

            return contentRect;
        }

        private static void ScrollToTop(ScrollRect? scrollRect)
        {
            if (scrollRect?.content == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            scrollRect.verticalNormalizedPosition = 1f;
            Canvas.ForceUpdateCanvases();
        }

        private static bool IsScrollContent(Transform parent) => parent.GetComponent<ScrollContentMarker>() != null;

        private static float GetFixedBackY(float panelHeight) => -(panelHeight * 0.5f) + FixedBackOffset;

        private static float GetFixedResetY(float panelHeight) => -(panelHeight * 0.5f) + FixedResetOffset;

        private static float GetScrollTopOffset(float panelHeight, RectTransform titleRect)
        {
            float titleBottomY = titleRect.anchoredPosition.y - (titleRect.sizeDelta.y * 0.5f);
            return Mathf.Max(0f, (panelHeight * 0.5f) - titleBottomY + ScrollTopPadding);
        }

        private static float GetScrollBottomOffset(float panelHeight, float highestButtonY)
        {
            float buttonTopY = highestButtonY + (ButtonRowHeight * 0.5f);
            return Mathf.Max(0f, (panelHeight * 0.5f) + buttonTopY + ScrollBottomPadding);
        }

        private static float ToTopY(float panelHeight, float centerY) => (panelHeight * 0.5f) - centerY;

        private static float GetRowStartY(float panelHeight, float rowStartCenterY, float topOffset)
        {
            return Mathf.Max(0f, ToTopY(panelHeight, rowStartCenterY) - topOffset);
        }

        private static void SetScrollContentHeight(RectTransform content, float viewHeight, float lastCenterY, float rowHeight)
        {
            float contentHeight = lastCenterY + (rowHeight * 0.5f) + 40f;
            content.sizeDelta = new Vector2(0f, Mathf.Max(contentHeight, viewHeight));
        }

        private static void ConfigureRowRect(RectTransform rect, Transform parent, float y, Vector2 size)
        {
            if (IsScrollContent(parent))
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, -y);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, y);
            }

            rect.sizeDelta = size;
        }

        private static GameObject CreateRow(Transform parent, string name, float y, Vector2 size)
        {
            GameObject row = new GameObject(name);
            row.transform.SetParent(parent, false);

            RectTransform rect = row.AddComponent<RectTransform>();
            ConfigureRowRect(rect, parent, y, size);

            return row;
        }

        private RowHighlight CreateRowHighlight(GameObject row, Image? baseImage)
        {
            Image image = baseImage ?? row.GetComponent<Image>() ?? row.AddComponent<Image>();
            Color highlightColor = HkRowHighlightColor;

            RowHighlight highlight = row.AddComponent<RowHighlight>();
            highlight.Initialize(image, highlightColor);
            return highlight;
        }

        private static readonly Color HkCardHoverColor = new(1f, 0.92f, 0.75f, 0.13f);

        private static RowHighlight CreateHkCardHighlight(GameObject card, Image cardImage)
        {
            GameObject frameObject = new("HoverFrame");
            frameObject.transform.SetParent(card.transform, false);
            RectTransform frameRect = frameObject.AddComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            Image frame = frameObject.AddComponent<Image>();
            frame.sprite = GetHkChamferSprite(true);
            frame.type = Image.Type.Sliced;
            frame.color = HkFrameOnColor;
            frame.raycastTarget = false;
            frame.enabled = false;

            RowHighlight highlight = card.AddComponent<RowHighlight>();
            highlight.UseCursors = false;
            highlight.Frame = frame;
            highlight.Initialize(cardImage, HkCardHoverColor);
            return highlight;
        }

        private static void AttachRowHighlight(GameObject target, RowHighlight highlight)
        {
            RowHighlightTrigger trigger = target.AddComponent<RowHighlightTrigger>();
            trigger.Highlight = highlight;
        }
    }
}
