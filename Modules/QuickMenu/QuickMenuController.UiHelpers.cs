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
    {        private InputField CreateInputField(Transform parent, string text, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject fieldObj = new GameObject("InputField");
            fieldObj.transform.SetParent(parent, false);

            RectTransform rect = fieldObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = fieldObj.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.08f);

            InputField input = fieldObj.AddComponent<InputField>();
            input.contentType = InputField.ContentType.IntegerNumber;
            input.lineType = InputField.LineType.SingleLine;
            input.caretColor = Color.white;
            input.selectionColor = new Color(1f, 1f, 1f, 0.25f);
            input.targetGraphic = image;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(fieldObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 0f);
            textRect.offsetMax = new Vector2(-6f, 0f);

            Text valueText = textObj.AddComponent<Text>();
            valueText.text = text;
            valueText.font = GetMenuFont();
            valueText.fontSize = 26;
            valueText.alignment = TextAnchor.MiddleCenter;
            valueText.color = Color.white;
            valueText.raycastTarget = false;

            input.textComponent = valueText;
            input.text = text;

            return input;
        }

        private Button CreateMiniButton(Transform parent, string name, string label, Vector2 anchoredPosition)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(InputControlButtonWidth, InputControlButtonHeight);

            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.08f);

            Outline outline = buttonObj.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.35f);
            outline.effectDistance = new Vector2(1f, -1f);

            Button button = buttonObj.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;

            Text text = CreateText(buttonObj.transform, "Label", label, 22, TextAnchor.MiddleCenter);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        private Button CreateCenteredMiniButton(Transform parent, string name, string label, Vector2 anchoredPosition)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(InputControlButtonWidth, InputControlButtonHeight);

            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.08f);

            Outline outline = buttonObj.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.35f);
            outline.effectDistance = new Vector2(1f, -1f);

            Button button = buttonObj.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;

            Text text = CreateText(buttonObj.transform, "Label", label, 22, TextAnchor.MiddleCenter);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        private void EnableUiInteraction()
        {
            prevCursorVisible = Cursor.visible;
            prevCursorLock = Cursor.lockState;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (UIManager.instance?.inputModule != null)
            {
                prevAllowMouseInput = UIManager.instance.inputModule.allowMouseInput;
                prevForceModuleActive = UIManager.instance.inputModule.forceModuleActive;
                UIManager.instance.inputModule.allowMouseInput = true;
                UIManager.instance.inputModule.forceModuleActive = true;
                UIManager.instance.inputModule.ActivateModule();
            }

        }

        private void RestoreUiInteraction()
        {
            if (UIManager.instance?.inputModule != null)
            {
                UIManager.instance.inputModule.allowMouseInput = prevAllowMouseInput;
                UIManager.instance.inputModule.forceModuleActive = prevForceModuleActive;
                if (!prevForceModuleActive)
                {
                    UIManager.instance.inputModule.DeactivateModule();
                }
            }

            Cursor.visible = prevCursorVisible;
            Cursor.lockState = prevCursorLock;
        }

        private void MaintainUiInteraction()
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
            }

            if (UIManager.instance?.inputModule != null)
            {
                if (!UIManager.instance.inputModule.allowMouseInput)
                {
                    UIManager.instance.inputModule.allowMouseInput = true;
                }

                UIManager.instance.inputModule.forceModuleActive = true;
                UIManager.instance.inputModule.ActivateModule();
            }
        }

        private void CaptureSelectionBeforeQuickMenuActivation()
        {
            selectionBeforeQuickMenu = null;

            EventSystem? eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            GameObject? selected = eventSystem.currentSelectedGameObject;
            if (IsValidExternalSelection(selected))
            {
                selectionBeforeQuickMenu = selected;
            }
        }

        private void TryRestoreSelectionAfterQuickMenuClose()
        {
            EventSystem? eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                selectionBeforeQuickMenu = null;
                return;
            }

            GameObject? currentSelection = eventSystem.currentSelectedGameObject;
            if (IsValidExternalSelection(currentSelection))
            {
                selectionBeforeQuickMenu = null;
                return;
            }

            GameObject? target = ResolveSelectionAfterQuickMenuClose();
            selectionBeforeQuickMenu = null;
            if (!IsSelectableTarget(target))
            {
                return;
            }

            eventSystem.SetSelectedGameObject(null);
            eventSystem.SetSelectedGameObject(target);
            EnsureUiInputReady();
        }

        private GameObject? ResolveSelectionAfterQuickMenuClose()
        {
            if (IsValidExternalSelection(selectionBeforeQuickMenu))
            {
                return selectionBeforeQuickMenu;
            }

            GameObject? challengeTarget = TryFindBossChallengeSelectionTarget();
            if (challengeTarget != null)
            {
                return challengeTarget;
            }

            return TryFindBossDoorSelectionTarget();
        }

        private static GameObject? TryFindBossChallengeSelectionTarget()
        {
            BossChallengeUI? challengeUi = UObject.FindObjectOfType<BossChallengeUI>();
            if (challengeUi == null || !challengeUi.gameObject.activeInHierarchy)
            {
                return null;
            }

            return FindFirstSelectableTarget(challengeUi.gameObject);
        }

        private static GameObject? TryFindBossDoorSelectionTarget()
        {
            BossDoorChallengeUI? doorUi = UObject.FindObjectOfType<BossDoorChallengeUI>();
            if (doorUi == null || !doorUi.gameObject.activeInHierarchy)
            {
                return null;
            }

            GameObject? beginButton = doorUi.gameObject.Child("Panel", "BeginButton");
            if (beginButton != null && IsSelectableTarget(beginButton))
            {
                return beginButton;
            }

            return FindFirstSelectableTarget(doorUi.gameObject);
        }

        private static GameObject? FindFirstSelectableTarget(GameObject root)
        {
            if (root == null || !root.activeInHierarchy)
            {
                return null;
            }

            foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(true))
            {
                if (selectable == null)
                {
                    continue;
                }

                GameObject target = selectable.gameObject;
                if (!target.activeInHierarchy || !selectable.IsInteractable())
                {
                    continue;
                }

                return target;
            }

            return null;
        }

        private static bool IsSelectableTarget(GameObject? target)
        {
            if (target == null || !target.activeInHierarchy)
            {
                return false;
            }

            Selectable? selectable = target.GetComponent<Selectable>();
            return selectable == null || (selectable.IsActive() && selectable.IsInteractable());
        }

        private bool IsValidExternalSelection(GameObject? target) =>
            target != null
            && IsSelectableTarget(target)
            && !target.transform.IsChildOf(transform);

        private static void EnsureUiInputReady()
        {
            InputHandler? inputHandler = InputHandler.Instance;
            if (inputHandler == null)
            {
                return;
            }

            inputHandler.StartAcceptingInput();
            inputHandler.StopUIInput();
            inputHandler.StartUIInput();

            if (UIManager.instance == null || EventSystem.current == null)
            {
                return;
            }

            var inputModule = UIManager.instance.inputModule;
            var actions = inputHandler.inputActions;
            if (inputModule == null || actions == null)
            {
                return;
            }

            inputModule.SubmitAction = actions.menuSubmit;
            inputModule.CancelAction = actions.menuCancel;
            inputModule.MoveAction = actions.moveVector;
            inputModule.forceModuleActive = true;
            inputModule.ActivateModule();
        }

        private static void DestroyRoot(ref GameObject? root)
        {
            if (root != null)
            {
                UObject.Destroy(root);
                root = null;
            }
        }

        private Text CreateRowLabel(Transform parent, string text)
        {
            Text label = CreateText(parent, "Label", text, 30, TextAnchor.MiddleLeft);
            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(20f, 0f);
            rect.sizeDelta = new Vector2(600f, RowHeight);
            return label;
        }

        private Text CreateRowLabelWithIcon(Transform parent, string text, bool isOn, out Image? icon)
        {
            Sprite? sprite = GetToggleIconSprite(isOn);
            if (sprite == null)
            {
                icon = null;
                return CreateRowLabel(parent, text);
            }

            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(parent, false);

            icon = iconObj.AddComponent<Image>();
            icon.sprite = sprite;
            icon.preserveAspect = true;

            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(20f, 0f);
            iconRect.sizeDelta = new Vector2(ToggleRowIconSize, ToggleRowIconSize);

            Text label = CreateRowLabel(parent, text);
            RectTransform rect = label.rectTransform;
            float labelOffset = 20f + ToggleRowIconSize + ToggleRowIconSpacing;
            rect.anchoredPosition = new Vector2(labelOffset, 0f);
            rect.sizeDelta = new Vector2(600f - (labelOffset - 20f), RowHeight);
            return label;
        }

        private void UpdateToggleIcon(Image? icon, bool isOn)
        {
            if (icon == null)
            {
                return;
            }

            Sprite? sprite = GetToggleIconSprite(isOn);
            if (sprite != null)
            {
                icon.sprite = sprite;
            }
        }

        private Text CreateRowValue(Transform parent, string text)
        {
            Text value = CreateText(parent, "Value", text, 30, TextAnchor.MiddleRight);
            RectTransform rect = value.rectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-20f, 0f);
            rect.sizeDelta = new Vector2(140f, RowHeight);
            return value;
        }

        private Text CreateText(Transform parent, string name, string text, int size, TextAnchor alignment)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            Text label = obj.AddComponent<Text>();
            label.text = text;
            label.font = GetMenuFont();
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;

            return label;
        }

        private void ShowStatusMessage(string message)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = message;
            statusText.gameObject.SetActive(true);
            statusHideTime = Time.unscaledTime + 2f;
        }

        internal void ShowStatusMessageFromExternal(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            ShowStatusMessage(message);
        }

        private void UpdateStatusMessage()
        {
            if (statusText == null || !statusText.gameObject.activeSelf)
            {
                return;
            }

            if (Time.unscaledTime >= statusHideTime)
            {
                statusText.gameObject.SetActive(false);
            }
        }
    }
}
