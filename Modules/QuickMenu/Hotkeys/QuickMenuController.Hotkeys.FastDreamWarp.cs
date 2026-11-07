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
    {
        private bool IsFastDreamWarpRebinding()
        {
            return waitingForFastDreamWarpRebind;
        }

        private void StartFastDreamWarpRebind()
        {
            waitingForFastDreamWarpRebind = true;
            fastDreamWarpPrevKeyRaw = GetFastDreamWarpKeyBindingRaw();
            fastDreamWarpPrevButton = GetFastDreamWarpControllerBinding();
            UpdateKeybindValue(fastDreamWarpKeyValue, "Settings/FastReload/SetKey".Localize());
        }

        private void CancelFastDreamWarpRebind()
        {
            if (!waitingForFastDreamWarpRebind)
            {
                return;
            }

            waitingForFastDreamWarpRebind = false;
            UpdateKeybindValue(fastDreamWarpKeyValue, GetFastDreamWarpBindingLabel());
        }

        private void HandleFastDreamWarpRebind()
        {
            if (!waitingForFastDreamWarpRebind)
            {
                return;
            }

            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (!Input.GetKeyDown(key) || key == KeyCode.None)
                {
                    continue;
                }

                if (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6)
                {
                    continue;
                }

                ApplyFastDreamWarpKeyRebind(key);
                return;
            }

            if (TryGetPressedControllerButton(out InputControlType button))
            {
                ApplyFastDreamWarpControllerRebind(button);
            }
        }

        private void ApplyFastDreamWarpKeyRebind(KeyCode key)
        {
            if (key == KeyCode.Escape)
            {
                waitingForFastDreamWarpRebind = false;
                UpdateKeybindValue(fastDreamWarpKeyValue, GetFastDreamWarpBindingLabel());
                return;
            }

            if (!TryMapKeyCodeToInControlKey(key, out Key mapped))
            {
                return;
            }

            string newLabel = mapped.ToString();
            bool clear = !string.IsNullOrEmpty(fastDreamWarpPrevKeyRaw)
                && string.Equals(fastDreamWarpPrevKeyRaw, newLabel, StringComparison.Ordinal);

            if (!clear && TryGetHotkeyConflictOwnersExceptSelf(key, "Settings/FastDreamWarp/Hotkey".Localize(), out string conflictOwners))
            {
                ShowStatusMessage(string.Format("QuickMenu/HotkeyInUse".Localize(), FormatKeyLabel(key), conflictOwners));
                UpdateKeybindValue(fastDreamWarpKeyValue, "Settings/FastReload/SetKey".Localize());
                return;
            }

            ApplyFastDreamWarpKeyBinding(clear ? (Key?)null : mapped);
            waitingForFastDreamWarpRebind = false;
            UpdateKeybindValue(fastDreamWarpKeyValue, GetFastDreamWarpBindingLabel());
        }

        private void ApplyFastDreamWarpControllerRebind(InputControlType button)
        {
            bool clear = !EqualityComparer<InputControlType>.Default.Equals(fastDreamWarpPrevButton, default)
                && EqualityComparer<InputControlType>.Default.Equals(fastDreamWarpPrevButton, button);

            ApplyFastDreamWarpControllerBinding(clear ? (InputControlType?)null : button);
            waitingForFastDreamWarpRebind = false;
            UpdateKeybindValue(fastDreamWarpKeyValue, GetFastDreamWarpBindingLabel());
        }

        private void HandleFastDreamWarpHotkey()
        {
            if (IsAnyUiVisible())
            {
                return;
            }

            PlayerAction action = FastDreamWarpSettings.Keybinds.Toggle;
            if (!action.WasPressed)
            {
                return;
            }

            bool newValue = !GetFastDreamWarpEnabled();
            SetFastDreamWarpEnabled(newValue);
            UpdateToggleValue(bossAnimFastDreamWarpValue, newValue);
            string state = newValue ? "ON" : "OFF";
            string label = "Modules/FastDreamWarp".Localize();
            ShowStatusMessage($"{label}: {state}");
        }

        private static void ApplyFastDreamWarpKeyBinding(Key? key)
        {
            PlayerAction action = FastDreamWarpSettings.Keybinds.Toggle;
            InputControlType controllerBinding = KeybindUtil.GetControllerButtonBinding(action);

            action.ClearBindings();

            if (!EqualityComparer<InputControlType>.Default.Equals(controllerBinding, default))
            {
                KeybindUtil.AddInputControlType(action, controllerBinding);
            }

            if (key.HasValue && !EqualityComparer<Key>.Default.Equals(key.Value, default))
            {
                KeybindUtil.AddKeyOrMouseBinding(action, new InputHandler.KeyOrMouseBinding(key.Value));
            }
        }

        private static void ApplyFastDreamWarpControllerBinding(InputControlType? button)
        {
            PlayerAction action = FastDreamWarpSettings.Keybinds.Toggle;
            Key? key = GetFastDreamWarpKeyBinding();

            action.ClearBindings();

            if (button.HasValue && !EqualityComparer<InputControlType>.Default.Equals(button.Value, default))
            {
                KeybindUtil.AddInputControlType(action, button.Value);
            }

            if (key.HasValue && !EqualityComparer<Key>.Default.Equals(key.Value, default))
            {
                KeybindUtil.AddKeyOrMouseBinding(action, new InputHandler.KeyOrMouseBinding(key.Value));
            }
        }

        private string GetFastDreamWarpBindingLabel()
        {
            string keyLabel = GetFastDreamWarpKeyBindingRaw();
            string controllerLabel = GetFastDreamWarpControllerBindingLabel();

            if (string.IsNullOrEmpty(keyLabel) && string.IsNullOrEmpty(controllerLabel))
            {
                return "Settings/FastReload/NotSet".Localize();
            }

            if (string.IsNullOrEmpty(keyLabel))
            {
                return controllerLabel;
            }

            if (string.IsNullOrEmpty(controllerLabel))
            {
                return keyLabel;
            }

            return $"{keyLabel} / {controllerLabel}";
        }

        private static string GetFastDreamWarpKeyBindingRaw()
        {
            InputHandler.KeyOrMouseBinding binding = KeybindUtil.GetKeyOrMouseBinding(FastDreamWarpSettings.Keybinds.Toggle);
            return FormatKeyOrMouseBinding(binding);
        }

        private static InputControlType GetFastDreamWarpControllerBinding()
        {
            return KeybindUtil.GetControllerButtonBinding(FastDreamWarpSettings.Keybinds.Toggle);
        }

        private static string GetFastDreamWarpControllerBindingLabel()
        {
            InputControlType binding = GetFastDreamWarpControllerBinding();
            return EqualityComparer<InputControlType>.Default.Equals(binding, default)
                ? string.Empty
                : binding.ToString();
        }

        private static bool TryGetPressedControllerButton(out InputControlType button)
        {
            button = default;

            foreach (InputDevice device in InputManager.Devices)
            {
                if (device == null || !device.AnyButtonWasPressed)
                {
                    continue;
                }

                foreach (InputControl control in device.Controls)
                {
                    if (control != null && control.IsButton && control.WasPressed)
                    {
                        button = control.Target;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
