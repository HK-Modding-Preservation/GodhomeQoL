using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController
    {
        private const int SettingsPresetsCanvasSortOrder = 10032;

        private GameObject? settingsPresetsRoot;
        private bool settingsPresetsVisible;
        private GameObject? settingsPresetConfirmRoot;
        private Text? settingsPresetConfirmLabel;
        private Action? settingsPresetConfirmAction;
        private bool settingsPresetConfirmVisible;
        private UnityEngine.UI.InputField? settingsPresetRenameField;
        private Text? settingsPresetRenameLabel;
        private SettingsPreset? settingsPresetRenameTarget;
        private bool settingsPresetRenameSubmitPending;
        private bool settingsPresetRenameCancelled;

        private void BuildSettingsPresetsOverlayUi()
        {
            settingsPresetConfirmAction = null;
            settingsPresetConfirmVisible = false;

            settingsPresetsRoot = CreateOverlayFrame("SettingsPresetsCanvas", SettingsPresetsCanvasSortOrder, "SettingsPresetsPanel", PanelHeight, "Presets/Title".Localize(), 52, 210f, out GameObject panel, out RectTransform titleRect);

            float panelHeight = PanelHeight;
            float createY = GetFixedResetY(panelHeight);
            float backY = GetFixedBackY(panelHeight);
            float topOffset = GetScrollTopOffset(panelHeight, titleRect);
            float bottomOffset = GetScrollBottomOffset(panelHeight, createY);
            float viewHeight = Mathf.Max(0f, panelHeight - topOffset - bottomOffset);
            RectTransform content = CreateScrollContent(panel.transform, PanelWidth, panelHeight, topOffset, bottomOffset);

            float rowY = GetRowStartY(panelHeight, RowStartY, topOffset);
            float lastY = rowY;

            List<SettingsPreset> presets = GodhomeQoL.SettingsPresetList;
            if (presets.Count == 0)
            {
                GameObject emptyRow = CreateRow(content, "SettingsPresetsEmptyRow", rowY, new Vector2(RowWidth, RowHeight));
                Text emptyLabel = CreateRowLabel(emptyRow.transform, "Presets/Empty".Localize());
                emptyLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            }

            for (int i = 0; i < presets.Count; i++)
            {
                CreateSettingsPresetRow(content, i, presets[i], rowY);
                lastY = rowY;
                rowY += RowSpacing;
            }

            SetScrollContentHeight(content, viewHeight, lastY, RowHeight);
            CreateButtonRow(panel.transform, "SettingsPresetsCreateRow", "Presets/SaveNew".Localize(), createY, OnSettingsPresetCreateClicked);
            CreateButtonRow(panel.transform, "SettingsPresetsBackRow", "Back", backY, OnSettingsPresetsBackClicked);
            CreateSettingsPresetConfirm(panel.transform);
            SetSettingsPresetConfirmVisible(false);
        }

        private void CreateSettingsPresetRow(Transform parent, int index, SettingsPreset preset, float y)
        {
            GameObject row = CreateRow(parent, $"SettingsPresetRow{index}", y, new Vector2(RowWidth, RowHeight));
            Image image = row.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            Button button = row.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;
            Text label = CreateRowLabel(row.transform, GetSettingsPresetRowLabel(preset));
            button.onClick.AddListener(() => OnSettingsPresetClicked(preset));
            RowHighlight highlight = CreateRowHighlight(row, image);
            AttachRowHighlight(row, highlight);

            Button save = CreateSettingsPresetTextButton(row.transform, "PresetSave", "Presets/Save".Localize(), new Vector2(270f, 0f), () => StartSettingsPresetOverwrite(preset));
            AttachRowHighlight(save.gameObject, highlight);

            Button? edit = CreateGearSwitcherPresetEditButton(row.transform, () => StartSettingsPresetRename(label, preset));
            if (edit != null)
            {
                AttachRowHighlight(edit.gameObject, highlight);
            }

            Button? delete = CreateGearSwitcherPresetDeleteButton(row.transform, () => StartSettingsPresetDelete(preset));
            if (delete != null)
            {
                AttachRowHighlight(delete.gameObject, highlight);
            }
        }

        private static string GetSettingsPresetRowLabel(SettingsPreset preset)
        {
            if (GodhomeQoL.SettingsPresetMatchesCurrent(preset))
            {
                return $"{preset.Name} ({"Presets/Active".Localize()})";
            }

            if (string.Equals(GodhomeQoL.GlobalSettings.ActiveSettingsPreset, preset.Name, StringComparison.OrdinalIgnoreCase))
            {
                return $"{preset.Name} ({"Presets/Changed".Localize()})";
            }

            return preset.Name;
        }

        private Button CreateSettingsPresetTextButton(Transform parent, string name, string text, Vector2 position, Action onClick)
        {
            GameObject buttonObject = new(name);
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(76f, 32f);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.08f);
            Button button = buttonObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());
            Text label = CreateText(buttonObject.transform, "Label", text, 22, TextAnchor.MiddleCenter);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            return button;
        }

        private void CreateSettingsPresetConfirm(Transform parent)
        {
            settingsPresetConfirmRoot = new GameObject("SettingsPresetConfirm");
            settingsPresetConfirmRoot.transform.SetParent(parent, false);
            RectTransform rootRect = settingsPresetConfirmRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            Image dim = settingsPresetConfirmRoot.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            CanvasGroup group = settingsPresetConfirmRoot.AddComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            GameObject dialog = new("Dialog");
            dialog.transform.SetParent(settingsPresetConfirmRoot.transform, false);
            RectTransform dialogRect = dialog.AddComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = Vector2.zero;
            dialogRect.sizeDelta = new Vector2(680f, 280f);
            Image dialogImage = dialog.AddComponent<Image>();
            StyleHkDialog(dialogImage);

            Text text = CreateText(dialog.transform, "Label", string.Empty, 26, TextAnchor.MiddleCenter);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0.5f, 1f);
            textRect.anchorMax = new Vector2(0.5f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = new Vector2(0f, -24f);
            textRect.sizeDelta = new Vector2(620f, 130f);
            settingsPresetConfirmLabel = text;

            CreateButtonRow(dialog.transform, "SettingsPresetConfirmYesRow", "Yes", -70f, OnSettingsPresetConfirmYes);
            CreateButtonRow(dialog.transform, "SettingsPresetConfirmNoRow", "No", -122f, OnSettingsPresetConfirmNo);
        }

        private void SetSettingsPresetConfirmVisible(bool value)
        {
            settingsPresetConfirmVisible = value;
            if (settingsPresetConfirmRoot != null)
            {
                settingsPresetConfirmRoot.SetActive(value);
            }

            if (!value)
            {
                settingsPresetConfirmAction = null;
            }
        }

        private void ShowSettingsPresetConfirm(string message, Action onYes)
        {
            CancelSettingsPresetRename();
            if (settingsPresetConfirmLabel != null)
            {
                settingsPresetConfirmLabel.text = message;
            }

            SetSettingsPresetConfirmVisible(true);
            settingsPresetConfirmAction = onYes;
        }

        private void OnSettingsPresetConfirmYes()
        {
            Action? action = settingsPresetConfirmAction;
            SetSettingsPresetConfirmVisible(false);
            action?.Invoke();
        }

        private void OnSettingsPresetConfirmNo()
        {
            SetSettingsPresetConfirmVisible(false);
        }

        private void SetSettingsPresetsVisible(bool value)
        {
            if (!value && settingsPresetRenameField != null)
            {
                CommitSettingsPresetRename(settingsPresetRenameField.text);
            }

            settingsPresetsVisible = value;
            if (value)
            {
                RebuildSettingsPresetsOverlay();
            }
            else
            {
                SetSettingsPresetConfirmVisible(false);
                if (settingsPresetsRoot != null)
                {
                    settingsPresetsRoot.SetActive(false);
                }
            }

            UpdateUiState();
        }

        private void RebuildSettingsPresetsOverlay()
        {
            DestroySettingsPresetRenameField();
            DestroyRoot(ref settingsPresetsRoot);
            BuildSettingsPresetsOverlayUi();
            if (settingsPresetsRoot != null)
            {
                settingsPresetsRoot.SetActive(settingsPresetsVisible);
            }
        }

        private void OnSettingsPresetsBackClicked()
        {
            bool reopenQuick = returnToQuickOnClose;
            returnToQuickOnClose = false;

            if (reopenQuick)
            {
                SetQuickVisible(true);
            }

            SetSettingsPresetsVisible(false);
        }

        private void OnSettingsPresetClicked(SettingsPreset preset)
        {
            CancelSettingsPresetRename();
            if (!GodhomeQoL.CanApplySettingsPreset())
            {
                ShowStatusMessage("Presets/NotNow".Localize());
                return;
            }

            if (GodhomeQoL.SettingsPresetList.Any(GodhomeQoL.SettingsPresetMatchesCurrent))
            {
                ApplySettingsPresetNow(preset);
                return;
            }

            ShowSettingsPresetConfirm(string.Format("Presets/ConfirmApply".Localize(), preset.Name), () => ApplySettingsPresetNow(preset));
        }

        private void ApplySettingsPresetNow(SettingsPreset preset)
        {
            if (!GodhomeQoL.CanApplySettingsPreset())
            {
                ShowStatusMessage("Presets/NotNow".Localize());
                return;
            }

            GodhomeQoL.ApplySettingsPreset(preset);
            RefreshUiAfterSettingsPreset();
            RebuildSettingsPresetsOverlay();
            ShowStatusMessage(string.Format("Presets/Applied".Localize(), preset.Name));
        }

        private void RefreshUiAfterSettingsPreset()
        {
            LoadMasterSettings();
            UpdateQuickMenuEntryStateColors();
            RefreshQuickMenuSettingsUi();
            RebuildGearSwitcherPresetOverlay();
            RefreshModulePanelsUi();
        }

        private void OnSettingsPresetCreateClicked()
        {
            CancelSettingsPresetRename();
            SettingsPreset preset = GodhomeQoL.CreateSettingsPresetFromCurrent();
            RebuildSettingsPresetsOverlay();
            ShowStatusMessage(string.Format("Presets/Created".Localize(), preset.Name));
        }

        private void StartSettingsPresetOverwrite(SettingsPreset preset)
        {
            ShowSettingsPresetConfirm(string.Format("Presets/ConfirmOverwrite".Localize(), preset.Name), () =>
            {
                GodhomeQoL.OverwriteSettingsPreset(preset);
                RebuildSettingsPresetsOverlay();
                ShowStatusMessage(string.Format("Presets/Saved".Localize(), preset.Name));
            });
        }

        private void StartSettingsPresetDelete(SettingsPreset preset)
        {
            ShowSettingsPresetConfirm(string.Format("Presets/ConfirmDelete".Localize(), preset.Name), () =>
            {
                GodhomeQoL.DeleteSettingsPreset(preset);
                RebuildSettingsPresetsOverlay();
                ShowStatusMessage(string.Format("Presets/Deleted".Localize(), preset.Name));
            });
        }

        private void StartSettingsPresetRename(Text label, SettingsPreset preset)
        {
            if (settingsPresetRenameField != null)
            {
                CommitSettingsPresetRename(settingsPresetRenameField.text);
                return;
            }

            SetSettingsPresetConfirmVisible(false);
            settingsPresetRenameCancelled = false;
            settingsPresetRenameSubmitPending = false;
            settingsPresetRenameTarget = preset;
            settingsPresetRenameLabel = label;

            UnityEngine.UI.InputField field = CreateGearSwitcherPresetRenameField(label.transform.parent, preset.Name, label.fontSize);
            field.characterLimit = GodhomeQoL.SettingsPresetNameLimit;
            field.onEndEdit.AddListener(OnSettingsPresetRenameEndEdit);
            settingsPresetRenameField = field;
            label.gameObject.SetActive(false);
            field.ActivateInputField();
            field.Select();
            field.MoveTextEnd(false);
        }

        private void HandleSettingsPresetRename()
        {
            if (settingsPresetRenameField == null)
            {
                return;
            }

            if (IsRenameConfirmPressed())
            {
                settingsPresetRenameSubmitPending = true;
                settingsPresetRenameField.DeactivateInputField();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                settingsPresetRenameCancelled = true;
                CancelSettingsPresetRename();
            }
        }

        private void OnSettingsPresetRenameEndEdit(string value)
        {
            if (settingsPresetRenameCancelled)
            {
                settingsPresetRenameCancelled = false;
                return;
            }

            if (!settingsPresetRenameSubmitPending)
            {
                if (settingsPresetRenameField != null)
                {
                    settingsPresetRenameField.ActivateInputField();
                    settingsPresetRenameField.Select();
                }

                return;
            }

            settingsPresetRenameSubmitPending = false;
            CommitSettingsPresetRename(value);
        }

        private void CommitSettingsPresetRename(string value)
        {
            SettingsPreset? target = settingsPresetRenameTarget;
            DestroySettingsPresetRenameField();
            if (target != null
                && !string.Equals((value ?? string.Empty).Trim(), target.Name, StringComparison.Ordinal)
                && !GodhomeQoL.RenameSettingsPreset(target, value ?? string.Empty))
            {
                ShowStatusMessage("Presets/NameInvalid".Localize());
            }

            RebuildSettingsPresetsOverlay();
        }

        private void CancelSettingsPresetRename()
        {
            if (settingsPresetRenameLabel != null)
            {
                settingsPresetRenameLabel.gameObject.SetActive(true);
            }

            DestroySettingsPresetRenameField();
        }

        private void DestroySettingsPresetRenameField()
        {
            if (settingsPresetRenameField != null)
            {
                UObject.Destroy(settingsPresetRenameField.gameObject);
            }

            settingsPresetRenameField = null;
            settingsPresetRenameLabel = null;
            settingsPresetRenameTarget = null;
        }
    }
}
