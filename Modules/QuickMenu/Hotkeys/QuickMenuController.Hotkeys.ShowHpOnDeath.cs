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
        private bool showHpRebindHistory;
        private Text? showHpHistoryKeyValue;

        private Text? ShowHpRebindValue => showHpRebindHistory ? showHpHistoryKeyValue : showHpHudToggleKeyValue;

        private static PlayerAction GetShowHpRebindAction(bool history) => history ? ShowHpSettings.Keybinds.History : ShowHpSettings.Keybinds.Hide;

        private static string GetShowHpRebindOwner(bool history) => (history ? "ShowHPOnDeath/HistoryKey" : "ShowHPOnDeath/HudToggleKey").Localize();

        private bool IsShowHpOnDeathRebinding()
        {
            return waitingForShowHpRebind;
        }

        private void StartShowHpOnDeathRebind()
        {
            StartShowHpRebind(false);
        }

        private void StartShowHpHistoryRebind()
        {
            StartShowHpRebind(true);
        }

        private void StartShowHpRebind(bool history)
        {
            if (waitingForShowHpRebind)
            {
                CancelShowHpOnDeathRebind();
            }

            showHpRebindHistory = history;
            waitingForShowHpRebind = true;
            showHpPrevBindingRaw = GetShowHpBindingRaw(GetShowHpRebindAction(history));
            UpdateKeybindValue(ShowHpRebindValue, "Settings/FastReload/SetKey".Localize());
        }

        private void CancelShowHpOnDeathRebind()
        {
            if (!waitingForShowHpRebind)
            {
                return;
            }

            waitingForShowHpRebind = false;
            UpdateKeybindValue(ShowHpRebindValue, GetShowHpBindingLabel(GetShowHpRebindAction(showHpRebindHistory)));
        }

        private void HandleShowHpOnDeathRebind()
        {
            if (!waitingForShowHpRebind)
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

                ApplyShowHpOnDeathRebind(key);
                return;
            }
        }

        private void ApplyShowHpOnDeathRebind(KeyCode key)
        {
            PlayerAction action = GetShowHpRebindAction(showHpRebindHistory);
            if (key == KeyCode.Escape)
            {
                waitingForShowHpRebind = false;
                UpdateKeybindValue(ShowHpRebindValue, GetShowHpBindingLabel(action));
                return;
            }

            if (!TryMapKeyCodeToInControlKey(key, out Key mapped))
            {
                return;
            }

            string newLabel = mapped.ToString();
            bool clear = !string.IsNullOrEmpty(showHpPrevBindingRaw)
                && string.Equals(showHpPrevBindingRaw, newLabel, StringComparison.Ordinal);

            if (!clear && TryGetHotkeyConflictOwnersExceptSelf(key, GetShowHpRebindOwner(showHpRebindHistory), out string conflictOwners))
            {
                ShowStatusMessage(string.Format("QuickMenu/HotkeyInUse".Localize(), FormatKeyLabel(key), conflictOwners));
                UpdateKeybindValue(ShowHpRebindValue, "Settings/FastReload/SetKey".Localize());
                return;
            }

            ApplyShowHpBinding(action, clear ? (Key?)null : mapped);
            waitingForShowHpRebind = false;
            overlayHotkeySuppressFrames = 2;
            UpdateKeybindValue(ShowHpRebindValue, GetShowHpBindingLabel(action));
        }

        private static void ApplyShowHpBinding(PlayerAction action, Key? key)
        {
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

        private string GetShowHpBindingLabel() => GetShowHpBindingLabel(ShowHpSettings.Keybinds.Hide);

        private string GetShowHpBindingLabel(PlayerAction action)
        {
            string raw = GetShowHpBindingRaw(action);
            return string.IsNullOrEmpty(raw) ? "Settings/FastReload/NotSet".Localize() : raw;
        }

        private static string GetShowHpBindingRaw() => GetShowHpBindingRaw(ShowHpSettings.Keybinds.Hide);

        private static string GetShowHpBindingRaw(PlayerAction action)
        {
            InputHandler.KeyOrMouseBinding binding = KeybindUtil.GetKeyOrMouseBinding(action);
            return FormatKeyOrMouseBinding(binding);
        }

        private void HandleBossHistoryHotkey()
        {
            PlayerAction? action = ShowHpSettings?.Keybinds?.History;
            if (action == null || !action.WasPressed)
            {
                return;
            }

            if (bossHistoryVisible)
            {
                CloseAllOverlaysForHotkey();
                return;
            }

            returnToQuickOnClose = false;
            returnToBossManipulateOnClose = false;
            returnToQolOnClose = false;
            SetQuickVisible(false);
            SetQuickSettingsVisible(false);
            CloseAllOverlaysForHotkey();
            bossHistorySelected = null;
            SetBossHistoryVisible(true);
        }
    }
}
