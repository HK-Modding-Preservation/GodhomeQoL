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
        private bool IsCheatsKillAllRebinding()
        {
            return waitingForCheatsKillAllRebind;
        }

        private string GetCheatsKillAllHotkeyLabel()
        {
            return FormatMaskDamageKeyLabel(Modules.Cheats.Cheats.GetKillAllHotkeyRaw());
        }

        private void StartCheatsKillAllRebind()
        {
            waitingForCheatsKillAllRebind = true;
            cheatsKillAllPrevKey = Modules.Cheats.Cheats.GetKillAllHotkeyRaw();
            UpdateKeybindValue(cheatsKillAllHotkeyValue, "Settings/FastReload/SetKey".Localize());
        }

        private void CancelCheatsKillAllRebind()
        {
            if (!waitingForCheatsKillAllRebind)
            {
                return;
            }

            waitingForCheatsKillAllRebind = false;
            UpdateKeybindValue(cheatsKillAllHotkeyValue, GetCheatsKillAllHotkeyLabel());
        }

        private void HandleCheatsKillAllRebind()
        {
            if (!waitingForCheatsKillAllRebind)
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

                ApplyCheatsKillAllRebind(key);
                return;
            }
        }

        private void ApplyCheatsKillAllRebind(KeyCode key)
        {
            if (key == KeyCode.Escape)
            {
                waitingForCheatsKillAllRebind = false;
                UpdateKeybindValue(cheatsKillAllHotkeyValue, GetCheatsKillAllHotkeyLabel());
                return;
            }

            string keyName = key.ToString();
            bool clear = !string.IsNullOrEmpty(cheatsKillAllPrevKey)
                && string.Equals(cheatsKillAllPrevKey, keyName, StringComparison.OrdinalIgnoreCase);

            if (!clear && TryGetHotkeyConflictOwnersExceptSelf(key, "Settings/Cheats/KillAllHotkey".Localize(), out string conflictOwners))
            {
                ShowStatusMessage(string.Format("QuickMenu/HotkeyInUse".Localize(), FormatKeyLabel(key), conflictOwners));
                UpdateKeybindValue(cheatsKillAllHotkeyValue, "Settings/FastReload/SetKey".Localize());
                return;
            }

            Modules.Cheats.Cheats.SetKillAllHotkeyRaw(clear ? string.Empty : keyName);
            waitingForCheatsKillAllRebind = false;
            UpdateKeybindValue(cheatsKillAllHotkeyValue, GetCheatsKillAllHotkeyLabel());
        }

        private void HandleCheatsKillAllHotkey()
        {
            if (!GetCheatsMasterEnabled() || !GetCheatsEnabled())
            {
                return;
            }

            KeyCode key = Modules.Cheats.Cheats.GetKillAllHotkey();
            if (key == KeyCode.None || !Input.GetKeyDown(key))
            {
                return;
            }

            _ = Modules.Cheats.Cheats.KillAll();
        }
    }
}
