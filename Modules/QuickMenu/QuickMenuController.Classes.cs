using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController
    {
        private sealed class QuickMenuItemDefinition
        {
            public QuickMenuItemDefinition(string id, string label, Action onClick)
            {
                Id = id;
                Label = label;
                OnClick = onClick;
            }

            public string Id { get; }
            public string Label { get; }
            public Action OnClick { get; }
        }

        private sealed class QuickMenuEntry
        {
            public QuickMenuEntry(string id, RectTransform rect, Button button, Text label, string defaultLabel)
            {
                Id = id;
                Rect = rect;
                Button = button;
                Label = label;
                DefaultLabel = defaultLabel;
            }

            public string Id { get; }
            public RectTransform Rect { get; }
            public Button Button { get; }
            public Text Label { get; }
            public string DefaultLabel { get; }
        }

        private sealed class QuickMenuDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            public QuickMenuController? Owner { get; set; }
            public QuickMenuEntry? Entry { get; set; }

            public void OnBeginDrag(PointerEventData eventData)
            {
                if (eventData.button != PointerEventData.InputButton.Left)
                {
                    return;
                }

                Owner?.BeginQuickMenuDrag(Entry, eventData);
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (eventData.button != PointerEventData.InputButton.Left)
                {
                    return;
                }

                Owner?.UpdateQuickMenuDrag(eventData);
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                if (eventData.button != PointerEventData.InputButton.Left)
                {
                    return;
                }

                Owner?.EndQuickMenuDrag();
            }
        }

        private sealed class RowHighlight : MonoBehaviour
        {
            private Image? background;
            private Color baseColor;
            private Color highlightColor;
            private int hoverCount;
            private int selectCount;
            private bool manualActive;

            public void Initialize(Image backgroundImage, Color highlight)
            {
                background = backgroundImage;
                baseColor = backgroundImage.color;
                highlightColor = highlight;
                SetActive(false);
            }

            public void AdjustHover(int delta)
            {
                hoverCount = Mathf.Max(0, hoverCount + delta);
                UpdateActive();
            }

            public void AdjustSelected(int delta)
            {
                selectCount = Mathf.Max(0, selectCount + delta);
                UpdateActive();
            }

            public void SetManualActive(bool active)
            {
                manualActive = active;
                UpdateActive();
            }

            private void OnDisable()
            {
                hoverCount = 0;
                selectCount = 0;
                manualActive = false;
                SetActive(false);
            }

            private void UpdateActive()
            {
                SetActive(manualActive || hoverCount > 0 || selectCount > 0);
            }

            public bool UseCursors { get; set; } = true;

            public Image? Frame { get; set; }

            private void SetActive(bool active)
            {
                if (background != null)
                {
                    background.color = active ? highlightColor : baseColor;
                }

                if (Frame != null)
                {
                    Frame.enabled = active;
                }

                if (active && UseCursors)
                {
                    ShowHkCursors(this);
                }
                else
                {
                    HideHkCursors(this);
                }
            }
        }

        private sealed class RowHighlightTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
        {
            public RowHighlight? Highlight { get; set; }

            public void OnPointerEnter(PointerEventData eventData)
            {
                Highlight?.AdjustHover(1);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                Highlight?.AdjustHover(-1);
            }

            public void OnSelect(BaseEventData eventData)
            {
                Highlight?.AdjustSelected(1);
            }

            public void OnDeselect(BaseEventData eventData)
            {
                Highlight?.AdjustSelected(-1);
            }
        }

        private sealed class CharmCostHighlightEntry
        {
            public Text ValueText { get; }
            public Outline Glow { get; }
            public Func<GearPreset, int> CurrentCost { get; }
            public Func<GearPreset, int> DefaultCost { get; }

            public CharmCostHighlightEntry(
                Text valueText,
                Outline glow,
                Func<GearPreset, int> currentCost,
                Func<GearPreset, int> defaultCost)
            {
                ValueText = valueText;
                Glow = glow;
                CurrentCost = currentCost;
                DefaultCost = defaultCost;
            }
        }

        private sealed class GearSwitcherPresetEntry
        {
            public int Index { get; }
            public Text Label { get; }
            public RowHighlight Highlight { get; }

            public GearSwitcherPresetEntry(int index, Text label, RowHighlight highlight)
            {
                Index = index;
                Label = label;
                Highlight = highlight;
            }
        }

        private sealed class BossManipulateCardVisual
        {
            public BossManipulateCardVisual(CanvasGroup group, Type moduleType)
            {
                Group = group;
                ModuleType = moduleType;
            }

            public CanvasGroup Group { get; }
            public Type ModuleType { get; }
        }
    }
}
