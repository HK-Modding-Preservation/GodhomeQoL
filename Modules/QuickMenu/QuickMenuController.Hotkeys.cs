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
    {        private bool IsAnyRebinding()
        {
            return IsFastReloadRebinding()
                || IsShowHpOnDeathRebinding()
                || IsMaskDamageRebinding()
                || IsNailDamageCheckRebinding()
                || IsFreezeHitboxesRebinding()
                || IsCheatsKillAllRebinding()
                || IsSpeedChangerRebinding()
                || IsTeleportKitRebinding()
                || IsFastDreamWarpRebinding()
                || renameField != null
                || gearSwitcherPresetRenameField != null
                || settingsPresetRenameField != null
                || waitingForQuickMenuHotkeyRebind
                || waitingForQuickMenuControllerRebind
                || waitingForOverlayHotkeyRebind;
        }

        internal bool IsHotkeyInputBlocked()
        {
            return (quickVisible && quickRenameMode)
                || IsAnyRebinding()
                || gearSwitcherPresetDeleteVisible
                || settingsPresetConfirmVisible
                || bossHistoryConfirmVisible
                || IsBossHistorySearchFocused()
                || gearSwitcherResetConfirmVisible
                || quickSettingsResetConfirmVisible
                || IsTeleportKitGameplayMenuVisible();
        }

        private bool TryGetHotkeyConflictOwnersExceptSelf(KeyCode key, string selfOwner, out string ownersText)
        {
            if (!TryGetFastReloadHotkeyConflictOwners(key, out string allOwners))
            {
                ownersText = string.Empty;
                return false;
            }

            List<string> filtered = allOwners
                .Split(',')
                .Select(o => o.Trim())
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Where(o => !string.Equals(o, selfOwner, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            ownersText = string.Join(", ", filtered);
            return filtered.Count > 0;
        }

        internal bool TryGetHotkeyConflictOwnersExceptSelfExternal(KeyCode key, string selfOwner, out string ownersText)
        {
            return TryGetHotkeyConflictOwnersExceptSelf(key, selfOwner, out ownersText);
        }

        private static bool KeyCodeMatchesRawKeybind(string raw, KeyCode key)
        {
            return !string.IsNullOrWhiteSpace(raw)
                && Enum.TryParse(raw, true, out KeyCode parsed)
                && parsed == key;
        }

        private static string FormatKeyLabel(KeyCode key)
        {
            return key == KeyCode.None
                ? "Settings/FastReload/NotSet".Localize()
                : key.ToString();
        }

        private void UpdateKeybindValue(Text? text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static string FormatKeyOrMouseBinding(InputHandler.KeyOrMouseBinding binding)
        {
            if (TryGetBindingKey(binding, out Key key)
                && !EqualityComparer<Key>.Default.Equals(key, default))
            {
                return key.ToString();
            }

            if (TryGetBindingMouse(binding, out Mouse mouse)
                && !EqualityComparer<Mouse>.Default.Equals(mouse, default))
            {
                return mouse.ToString();
            }

            return string.Empty;
        }

        private static bool TryGetBindingKey(InputHandler.KeyOrMouseBinding binding, out Key key)
        {
            object boxed = binding;
            Type type = boxed.GetType();

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (property.PropertyType == typeof(Key))
                {
                    key = (Key)property.GetValue(boxed);
                    return true;
                }
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.FieldType == typeof(Key))
                {
                    key = (Key)field.GetValue(boxed);
                    return true;
                }
            }

            key = default;
            return false;
        }

        private static bool TryGetBindingMouse(InputHandler.KeyOrMouseBinding binding, out Mouse mouse)
        {
            object boxed = binding;
            Type type = boxed.GetType();

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (property.PropertyType == typeof(Mouse))
                {
                    mouse = (Mouse)property.GetValue(boxed);
                    return true;
                }
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.FieldType == typeof(Mouse))
                {
                    mouse = (Mouse)field.GetValue(boxed);
                    return true;
                }
            }

            mouse = default;
            return false;
        }

        private static bool TryMapKeyCodeToInControlKey(KeyCode keyCode, out Key key)
        {
            key = default;
            if (keyCode == KeyCode.None)
            {
                return false;
            }

            if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
            {
                int digit = (int)keyCode - (int)KeyCode.Alpha0;
                return Enum.TryParse($"Key{digit}", out key);
            }

            if (keyCode >= KeyCode.Keypad0 && keyCode <= KeyCode.Keypad9)
            {
                int digit = (int)keyCode - (int)KeyCode.Keypad0;
                return Enum.TryParse($"Pad{digit}", out key);
            }

            if (keyCode == KeyCode.KeypadEnter)
            {
                if (Enum.TryParse("PadEnter", out key))
                {
                    return true;
                }

                return Enum.TryParse("KeypadEnter", out key);
            }

            return Enum.TryParse(keyCode.ToString(), out key);
        }
    }
}
