using System.IO;
using InControl;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController : MonoBehaviour
    {
        private void UpdateFreeMenuLabel()
        {
            QuickMenuEntry? entry = quickEntries.FirstOrDefault(item => item.Id == "FreeMenu");
            if (entry == null)
            {
                return;
            }

            entry.Label.text = GetFreeMenuLabel();
        }

        private string GetRenameModeLabel()
        {
            string label = "QuickMenu/RenameMode".Localize();
            string state = quickRenameMode ? "ON" : "OFF";
            return $"{label}: {state}";
        }

        private void UpdateRenameModeLabel()
        {
            QuickMenuEntry? entry = quickEntries.FirstOrDefault(item => item.Id == "RenameMode");
            if (entry == null)
            {
                return;
            }

            entry.Label.text = GetRenameModeLabel();
        }

        private static bool IsQuickMenuRenameAllowed(string id)
        {
            return id != "FreeMenu" && id != "RenameMode" && id != "Settings";
        }

        private static bool IsOverlayHotkeySupported(string id)
        {
            return id != "FreeMenu" && id != "RenameMode" && id != "Settings";
        }

        private Dictionary<string, string> GetOverlayHotkeyMap()
        {
            return GodhomeQoL.GlobalSettings.QuickMenuOverlayHotkeys ??= new Dictionary<string, string>();
        }

        private string GetOverlayHotkeyRaw(string id)
        {
            Dictionary<string, string> map = GetOverlayHotkeyMap();
            return map.TryGetValue(id, out string? raw) ? raw : string.Empty;
        }

        private void SetOverlayHotkeyRaw(string id, string raw)
        {
            Dictionary<string, string> map = GetOverlayHotkeyMap();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                if (TryParseOverlayControllerBinding(raw, out InputControlType button))
                {
                    if (IsOverlayControllerHotkeyInUse(id, button))
                    {
                        raw = string.Empty;
                    }
                }
                else if (Enum.TryParse(raw, true, out KeyCode key))
                {
                    if (IsOverlayKeyboardHotkeyInUse(id, key) || key == GetQuickMenuToggleKey())
                    {
                        raw = string.Empty;
                    }
                }
            }
            map[id] = raw;
        }

        private string GetOverlayHotkeyLabel(string id)
        {
            string raw = GetOverlayHotkeyRaw(id);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "Settings/FastReload/NotSet".Localize();
            }

            if (TryParseOverlayControllerBinding(raw, out InputControlType button))
            {
                return button.ToString();
            }

            if (Enum.TryParse(raw, true, out KeyCode key))
            {
                return FormatKeyLabel(key);
            }

            return raw;
        }

        private KeyCode GetOverlayHotkeyKey(string id)
        {
            string raw = GetOverlayHotkeyRaw(id);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return KeyCode.None;
            }

            if (TryParseOverlayControllerBinding(raw, out _))
            {
                return KeyCode.None;
            }

            return Enum.TryParse(raw, true, out KeyCode key) ? key : KeyCode.None;
        }

        private InputControlType GetOverlayHotkeyControllerBinding(string id)
        {
            string raw = GetOverlayHotkeyRaw(id);
            return TryParseOverlayControllerBinding(raw, out InputControlType button) ? button : default;
        }

        private static bool TryParseOverlayControllerBinding(string raw, out InputControlType button)
        {
            button = default;
            if (string.IsNullOrWhiteSpace(raw) || !raw.StartsWith(OverlayControllerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string value = raw.Substring(OverlayControllerPrefix.Length);
            return Enum.TryParse(value, true, out button);
        }

        private bool IsOverlayKeyboardHotkeyInUse(string id, KeyCode key)
        {
            if (key == KeyCode.None)
            {
                return false;
            }

            foreach (KeyValuePair<string, string> entry in GetOverlayHotkeyMap())
            {
                if (string.Equals(entry.Key, id, StringComparison.Ordinal))
                {
                    continue;
                }

                string raw = entry.Value;
                if (string.IsNullOrWhiteSpace(raw) || TryParseOverlayControllerBinding(raw, out _))
                {
                    continue;
                }

                if (Enum.TryParse(raw, true, out KeyCode existing) && existing == key)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsOverlayControllerHotkeyInUse(string id, InputControlType button)
        {
            if (EqualityComparer<InputControlType>.Default.Equals(button, default))
            {
                return false;
            }

            foreach (KeyValuePair<string, string> entry in GetOverlayHotkeyMap())
            {
                if (string.Equals(entry.Key, id, StringComparison.Ordinal))
                {
                    continue;
                }

                string raw = entry.Value;
                if (TryParseOverlayControllerBinding(raw, out InputControlType existing)
                    && EqualityComparer<InputControlType>.Default.Equals(existing, button))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsControllerButtonPressed(InputControlType button)
        {
            foreach (InputDevice device in InputManager.Devices)
            {
                if (device == null)
                {
                    continue;
                }

                InputControl control = device.GetControl(button);
                if (control != null && control.IsButton && control.WasPressed)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetOverlayHotkeySuppress(string raw, KeyCode key, InputControlType button)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                overlayHotkeySuppressActive = false;
                overlayHotkeySuppressKey = KeyCode.None;
                overlayHotkeySuppressButton = default;
                return;
            }

            if (key != KeyCode.None)
            {
                overlayHotkeySuppressActive = true;
                overlayHotkeySuppressKey = key;
                overlayHotkeySuppressButton = default;
                return;
            }

            if (!EqualityComparer<InputControlType>.Default.Equals(button, default))
            {
                overlayHotkeySuppressActive = true;
                overlayHotkeySuppressKey = KeyCode.None;
                overlayHotkeySuppressButton = button;
            }
        }

        private bool IsOverlayHotkeySuppressed()
        {
            if (!overlayHotkeySuppressActive)
            {
                return false;
            }

            if (overlayHotkeySuppressKey != KeyCode.None)
            {
                if (Input.GetKey(overlayHotkeySuppressKey))
                {
                    return true;
                }

                overlayHotkeySuppressActive = false;
                overlayHotkeySuppressKey = KeyCode.None;
                return false;
            }

            if (!EqualityComparer<InputControlType>.Default.Equals(overlayHotkeySuppressButton, default))
            {
                if (IsControllerButtonDown(overlayHotkeySuppressButton))
                {
                    return true;
                }

                overlayHotkeySuppressActive = false;
                overlayHotkeySuppressButton = default;
            }

            return false;
        }

        private static bool IsControllerButtonDown(InputControlType button)
        {
            foreach (InputDevice device in InputManager.Devices)
            {
                if (device == null)
                {
                    continue;
                }

                InputControl control = device.GetControl(button);
                if (control != null && control.IsPressed)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetQuickMenuDisplayLabel(string id, string defaultLabel)
        {
            if (!IsQuickMenuRenameAllowed(id))
            {
                return defaultLabel;
            }

            Dictionary<string, string> labels = GodhomeQoL.GlobalSettings.QuickMenuCustomLabels ??= new Dictionary<string, string>();
            if (labels.TryGetValue(id, out string? custom) && !string.IsNullOrWhiteSpace(custom))
            {
                return custom;
            }

            return defaultLabel;
        }

        private static bool TryGetQuickMenuCustomLabel(string id, out string customLabel)
        {
            customLabel = string.Empty;
            if (!IsQuickMenuRenameAllowed(id))
            {
                return false;
            }

            Dictionary<string, string> labels = GodhomeQoL.GlobalSettings.QuickMenuCustomLabels ??= new Dictionary<string, string>();
            if (labels.TryGetValue(id, out string? custom) && !string.IsNullOrWhiteSpace(custom))
            {
                customLabel = custom;
                return true;
            }

            return false;
        }

        private float GetQuickRowY(int index)
        {
            return QuickButtonTop - index * (QuickButtonHeight + QuickButtonSpacing);
        }

        private QuickMenuEntry CreateQuickEntry(Transform parent, QuickMenuItemDefinition definition, float y)
        {
            GameObject rowObj = new GameObject($"{definition.Id}Row");
            rowObj.transform.SetParent(parent, false);

            RectTransform rowRect = rowObj.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(0f, 1f);
            rowRect.pivot = new Vector2(0f, 1f);
            rowRect.anchoredPosition = new Vector2(QuickButtonLeft, y);
            rowRect.sizeDelta = new Vector2(QuickRowWidth, QuickButtonHeight);

            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(rowObj.transform, false);

            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.pivot = new Vector2(0f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(QuickHandleWidth, QuickButtonHeight);

            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(1f, 1f, 1f, 0f);

            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(handleObj.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(QuickHandleWidth, QuickButtonHeight);

            Sprite? handleSprite = quickHandleSprite ??= LoadQuickHandleSprite();
            if (handleSprite != null)
            {
                Image iconImage = iconObj.AddComponent<Image>();
                iconImage.sprite = handleSprite;
                iconImage.preserveAspect = true;
                iconImage.color = Color.white;
                iconImage.raycastTarget = false;
            }
            else
            {
                Text handleText = CreateText(iconObj.transform, "Label", "|||", 18, TextAnchor.MiddleCenter);
                handleText.raycastTarget = false;
                RectTransform handleTextRect = handleText.rectTransform;
                handleTextRect.anchorMin = Vector2.zero;
                handleTextRect.anchorMax = Vector2.one;
                handleTextRect.offsetMin = Vector2.zero;
                handleTextRect.offsetMax = Vector2.zero;
            }

            GameObject buttonObj = new GameObject("Button");
            buttonObj.transform.SetParent(rowObj.transform, false);

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 0.5f);
            buttonRect.anchorMax = new Vector2(0f, 0.5f);
            buttonRect.pivot = new Vector2(0f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(QuickHandleWidth + QuickHandleGap, 0f);
            buttonRect.sizeDelta = new Vector2(QuickButtonWidth, QuickButtonHeight);

            Image buttonImage = buttonObj.AddComponent<Image>();
            ApplyHkChamferPlate(buttonImage, QuickMenuBaseColor);
            AddHkPlateHover(buttonObj, buttonImage, QuickMenuBaseColor);

            GameObject fillObj = new GameObject("StateFill");
            fillObj.transform.SetParent(buttonObj.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0.12f);
            fillRect.anchorMax = new Vector2(0f, 0.88f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(6f, 0f);
            fillRect.sizeDelta = new Vector2(5f, 0f);

            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0f, 0f, 0f, 0f);
            fillImage.raycastTarget = false;

            Outline outline = buttonObj.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.enabled = false;

            Button button = buttonObj.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = buttonImage;

            string displayLabel = GetQuickMenuDisplayLabel(definition.Id, definition.Label);
            Text text = CreateText(buttonObj.transform, "Text", displayLabel, 24, TextAnchor.MiddleCenter);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            StyleHkQuickEntryText(text);
            text.color = HkQuickEntryTextColor;
            CreateHkQuickEntryFrame(buttonObj.transform);
            CreateHkQuickEntryStateDot(buttonObj.transform);

            QuickMenuEntry entry = new(definition.Id, rowRect, button, text, definition.Label);
            QuickMenuDragHandle dragHandle = handleObj.AddComponent<QuickMenuDragHandle>();
            dragHandle.Owner = this;
            dragHandle.Entry = entry;
            button.onClick.AddListener(() => OnQuickEntryClicked(definition, entry));

            return entry;
        }

        private void UpdateQuickMenuEntryIcons(QuickMenuEntry entry)
        {
        }

        private void UpdateQuickMenuEntryColor(string id, bool enabled)
        {
            QuickMenuEntry? entry = quickEntries.FirstOrDefault(item => item.Id == id);
            if (entry == null)
            {
                return;
            }

            Image? image = entry.Button.targetGraphic as Image;
            if (image == null)
            {
                return;
            }

            Color targetColor = enabled ? QuickMenuZoteEnabledColor : QuickMenuZoteDisabledColor;
            ApplyHkQuickEntryState(entry, image, enabled);

            Transform? fill = entry.Button.transform.Find("StateFill");
            if (fill != null && fill.TryGetComponent(out Image fillImage))
            {
                fillImage.color = targetColor;
            }

            Outline? outline = entry.Button.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = Color.white;
                outline.effectDistance = new Vector2(1f, -1f);
                outline.useGraphicAlpha = true;
                outline.enabled = false;
            }
        }

        private static bool IsBossManipulateModuleEnabled(Type moduleType)
        {
            if (ModuleManager.TryGetModule(moduleType, out Module? module))
            {
                return module?.Enabled ?? false;
            }

            return false;
        }

        private void RefreshBossManipulateCardVisuals()
        {
            foreach (BossManipulateCardVisual card in bossManipulateCards)
            {
                if (card.Group == null)
                {
                    continue;
                }

                card.Group.alpha = IsBossManipulateModuleEnabled(card.ModuleType) ? 1f : BossManipulateCardDisabledAlpha;
            }
        }

        private void RefreshBossManipulateGlobalUi()
        {
            UpdateToggleValue(bossManipulateGlobalP5Value, GetBossManipulateGlobalP5Enabled());
        }

        private void UpdateQuickMenuEntryStateColors()
        {
            UpdateQuickMenuEntryColor("FastSuperDash", GetModuleEnabled());
            UpdateQuickMenuEntryColor("FastReload", GetFastReloadEnabled());
            UpdateQuickMenuEntryColor("DreamshieldSettings", GetDreamshieldEnabled());
            UpdateQuickMenuEntryColor("SpeedChanger", SpeedChanger.globalSwitch);
            UpdateQuickMenuEntryColor("TeleportKit", GetTeleportKitEnabled());
            UpdateQuickMenuEntryColor("ShowHPOnDeath", GetShowHpOnDeathEnabled());
            UpdateQuickMenuEntryColor("MaskDamage", GetMaskDamageEnabled());
            UpdateQuickMenuEntryColor("FreezeHitboxes", GetFreezeHitboxesEnabled());
            UpdateQuickMenuEntryColor("FpsBoost", GetFpsBoostEnabled());
            UpdateQuickMenuEntryColor("AlwaysFurious", GetAlwaysFuriousEnabled());
            UpdateQuickMenuEntryColor("GearSwitcher", GetGearSwitcherEnabled());
            UpdateQuickMenuEntryColor("BossChallenge", GetBossChallengeMasterEnabled());
            UpdateQuickMenuEntryColor("BossManipulate", GetCollectorPhasesEnabled() || GetZoteHelperEnabled() || GetGruzMotherHelperEnabled() || GetHornetProtectorHelperEnabled() || GetBroodingMawlekHelperEnabled() || GetMassiveMossChargerHelperEnabled() || GetCrystalGuardianHelperEnabled() || GetEnragedGuardianHelperEnabled() || GetHornetSentinelHelperEnabled() || GetAdditionalGhostHelpersEnabled() || GetGruzMotherP1HelperEnabled() || GetVengeflyKingP1HelperEnabled() || GetBroodingMawlekP1HelperEnabled() || GetNoskP2HelperEnabled() || GetUumuuP3HelperEnabled() || GetSoulWarriorP1HelperEnabled() || GetNoEyesP4HelperEnabled() || GetMarmuP2HelperEnabled() || GetXeroP2HelperEnabled() || GetMarkothP4HelperEnabled() || GetGorbP1HelperEnabled());
            UpdateQuickMenuEntryColor("RandomPantheons", GetRandomPantheonsMasterEnabled());
            UpdateQuickMenuEntryColor("TrueBossRush", GetTrueBossRushMasterEnabled() && GetTrueBossRushEnabled());
            UpdateQuickMenuEntryColor("Cheats", GetCheatsMasterEnabled());
            UpdateQuickMenuEntryColor("QualityOfLife", GetQolMasterEnabled());
            UpdateQuickMenuEntryColor("BossAnimationSkipping", GetBossAnimationMasterEnabled());
            UpdateQuickMenuEntryColor("MenuAnimationSkipping", GetMenuAnimationMasterEnabled());
            RefreshBossManipulateCardVisuals();
            RefreshBossManipulateGlobalUi();
        }

        internal void RefreshQuickMenuEntryColors()
        {
            UpdateQuickMenuEntryStateColors();
        }

    }
}
