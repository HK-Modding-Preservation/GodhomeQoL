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
        private string GetNailDamageCheckKeyLabel()
        {
            return FormatMaskDamageKeyLabel(NailDamageCheck.GetKeybind());
        }

        private bool IsNailDamageCheckRebinding()
        {
            return waitingForNailDamageCheckRebind;
        }

        private void StartNailDamageCheckRebind()
        {
            waitingForNailDamageCheckRebind = true;
            nailDamageCheckPrevKey = NailDamageCheck.GetKeybind();
            UpdateKeybindValue(nailDamageCheckKeyValue, "Settings/FastReload/SetKey".Localize());
        }

        private void CancelNailDamageCheckRebind()
        {
            if (!waitingForNailDamageCheckRebind)
            {
                return;
            }

            waitingForNailDamageCheckRebind = false;
            UpdateKeybindValue(nailDamageCheckKeyValue, GetNailDamageCheckKeyLabel());
        }

        private void HandleNailDamageCheckRebind()
        {
            if (!waitingForNailDamageCheckRebind)
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

                ApplyNailDamageCheckRebind(key);
                return;
            }
        }

        private void ApplyNailDamageCheckRebind(KeyCode key)
        {
            if (key == KeyCode.Escape)
            {
                waitingForNailDamageCheckRebind = false;
                UpdateKeybindValue(nailDamageCheckKeyValue, GetNailDamageCheckKeyLabel());
                return;
            }

            string keyName = key.ToString();
            bool clear = !string.IsNullOrEmpty(nailDamageCheckPrevKey)
                && string.Equals(nailDamageCheckPrevKey, keyName, StringComparison.OrdinalIgnoreCase);

            if (!clear && TryGetHotkeyConflictOwnersExceptSelf(key, "Settings/QoL/NailDamageCheck".Localize(), out string conflictOwners))
            {
                ShowStatusMessage(string.Format("QuickMenu/HotkeyInUse".Localize(), FormatKeyLabel(key), conflictOwners));
                UpdateKeybindValue(nailDamageCheckKeyValue, "Settings/FastReload/SetKey".Localize());
                return;
            }

            NailDamageCheck.SetKeybind(clear ? string.Empty : keyName);
            waitingForNailDamageCheckRebind = false;
            UpdateKeybindValue(nailDamageCheckKeyValue, GetNailDamageCheckKeyLabel());
        }
    }
}
