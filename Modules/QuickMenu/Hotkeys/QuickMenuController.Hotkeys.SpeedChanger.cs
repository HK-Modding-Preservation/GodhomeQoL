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
        private bool IsSpeedChangerRebinding()
        {
            return waitingForSpeedToggleRebind
                || waitingForSpeedInputRebind;
        }

        private void StartSpeedChangerToggleRebind()
        {
            waitingForSpeedToggleRebind = true;
            speedTogglePrevKey = SpeedChanger.toggleKeybind;
            UpdateKeybindValue(speedChangerToggleKeyValue, "Settings/FastReload/SetKey".Localize());
        }

        private void StartSpeedChangerInputRebind()
        {
            waitingForSpeedInputRebind = true;
            speedInputPrevKey = SpeedChanger.inputSpeedKeybind;
            UpdateKeybindValue(speedChangerInputKeyValue, "Settings/FastReload/SetKey".Localize());
        }

        private void CancelSpeedChangerRebind()
        {
            if (waitingForSpeedToggleRebind)
            {
                waitingForSpeedToggleRebind = false;
                UpdateKeybindValue(speedChangerToggleKeyValue, FormatSpeedChangerKeyLabel(SpeedChanger.toggleKeybind));
            }

            if (waitingForSpeedInputRebind)
            {
                waitingForSpeedInputRebind = false;
                UpdateKeybindValue(speedChangerInputKeyValue, FormatSpeedChangerKeyLabel(SpeedChanger.inputSpeedKeybind));
            }
        }

        private void HandleSpeedChangerRebind()
        {
            if (!IsSpeedChangerRebinding())
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

                if (waitingForSpeedToggleRebind)
                {
                    ApplySpeedChangerToggleRebind(key);
                    return;
                }

                if (waitingForSpeedInputRebind)
                {
                    ApplySpeedChangerInputRebind(key);
                    return;
                }

            }
        }

        private void ApplySpeedChangerToggleRebind(KeyCode key)
        {
            if (key == KeyCode.Escape)
            {
                waitingForSpeedToggleRebind = false;
                UpdateKeybindValue(speedChangerToggleKeyValue, FormatSpeedChangerKeyLabel(SpeedChanger.toggleKeybind));
                return;
            }

            bool clear = string.Equals(speedTogglePrevKey, key.ToString(), StringComparison.OrdinalIgnoreCase);
            if (!clear && TryGetHotkeyConflictOwnersExceptSelf(key, "SpeedChanger/ToggleKey".Localize(), out string conflictOwners))
            {
                ShowStatusMessage(string.Format("QuickMenu/HotkeyInUse".Localize(), FormatKeyLabel(key), conflictOwners));
                UpdateKeybindValue(speedChangerToggleKeyValue, "Settings/FastReload/SetKey".Localize());
                return;
            }

            SetSpeedChangerKeybind(key, speedTogglePrevKey, "toggle", value => SpeedChanger.toggleKeybind = value);
            SpeedChanger.SuppressToggleUntilRelease(key);
            waitingForSpeedToggleRebind = false;
            UpdateKeybindValue(speedChangerToggleKeyValue, FormatSpeedChangerKeyLabel(SpeedChanger.toggleKeybind));
        }

        private void ApplySpeedChangerInputRebind(KeyCode key)
        {
            if (key == KeyCode.Escape)
            {
                waitingForSpeedInputRebind = false;
                UpdateKeybindValue(speedChangerInputKeyValue, FormatSpeedChangerKeyLabel(SpeedChanger.inputSpeedKeybind));
                return;
            }

            bool clear = string.Equals(speedInputPrevKey, key.ToString(), StringComparison.OrdinalIgnoreCase);
            if (!clear && TryGetHotkeyConflictOwnersExceptSelf(key, "SpeedChanger/InputKey".Localize(), out string conflictOwners))
            {
                ShowStatusMessage(string.Format("QuickMenu/HotkeyInUse".Localize(), FormatKeyLabel(key), conflictOwners));
                UpdateKeybindValue(speedChangerInputKeyValue, "Settings/FastReload/SetKey".Localize());
                return;
            }

            SetSpeedChangerKeybind(key, speedInputPrevKey, "input", value => SpeedChanger.inputSpeedKeybind = value);
            SpeedChanger.SuppressInputUntilRelease(key);
            waitingForSpeedInputRebind = false;
            UpdateKeybindValue(speedChangerInputKeyValue, FormatSpeedChangerKeyLabel(SpeedChanger.inputSpeedKeybind));
        }

        private void SetSpeedChangerKeybind(KeyCode key, string previous, string slot, Action<string> setter)
        {
            string keyName = key.ToString();
            bool clear = string.Equals(previous, keyName, StringComparison.OrdinalIgnoreCase)
                || IsSpeedChangerKeyInUse(keyName, slot);

            setter(clear ? string.Empty : keyName);
            RefreshSpeedChangerKeybinds();
            GodhomeQoL.SaveGlobalSettingsSafe();
        }

        private static bool IsSpeedChangerKeyInUse(string keyName, string except)
        {
            if (string.IsNullOrWhiteSpace(keyName))
            {
                return false;
            }

            bool inToggle = !string.Equals(except, "toggle", StringComparison.OrdinalIgnoreCase)
                && string.Equals(SpeedChanger.toggleKeybind, keyName, StringComparison.OrdinalIgnoreCase);
            bool inInput = !string.Equals(except, "input", StringComparison.OrdinalIgnoreCase)
                && string.Equals(SpeedChanger.inputSpeedKeybind, keyName, StringComparison.OrdinalIgnoreCase);

            return inToggle || inInput;
        }

        private void RefreshSpeedChangerKeybinds()
        {
            SpeedChanger? module = GetSpeedChangerModule();
            if (module == null)
            {
                return;
            }

            MethodInfo? method = typeof(SpeedChanger).GetMethod("RefreshKeybinds", BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(module, null);
        }

        private static string FormatSpeedChangerKeyLabel(string storedKey)
        {
            if (string.IsNullOrWhiteSpace(storedKey) || storedKey.Equals("Not Set", StringComparison.OrdinalIgnoreCase))
            {
                return "SpeedChanger/NotSet".Localize();
            }

            return storedKey;
        }

        private static string GetSpeedChangerDisplayValue()
        {
            int index = ClampOptionIndex(SpeedChanger.displayStyle, SpeedChangerDisplayOptions.Length);
            return SpeedChangerDisplayOptions[index];
        }
    }
}
