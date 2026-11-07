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
        private string GetMaskDamageToggleUiKeyLabel()
        {
            return FormatMaskDamageKeyLabel(MaskDamage.GetToggleUiKeybind());
        }

        private string GetFreezeHitboxesKeyLabel()
        {
            return FormatMaskDamageKeyLabel(FreezeHitboxes.GetUnfreezeKeybind());
        }

        private static string FormatMaskDamageKeyLabel(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "Settings/FastReload/NotSet".Localize();
            }

            if (Enum.TryParse(raw, true, out KeyCode key))
            {
                return FormatKeyLabel(key);
            }

            return raw;
        }

        private bool IsMaskDamageRebinding()
        {
            return waitingForMaskDamageUiRebind;
        }

        private void StartMaskDamageUiRebind()
        {
            waitingForMaskDamageUiRebind = true;
            maskDamageUiPrevKey = MaskDamage.GetToggleUiKeybind();
            UpdateKeybindValue(maskDamageToggleUiKeyValue, "Settings/FastReload/SetKey".Localize());
        }

        private void CancelMaskDamageRebind()
        {
            if (!waitingForMaskDamageUiRebind)
            {
                return;
            }

            waitingForMaskDamageUiRebind = false;
            UpdateKeybindValue(maskDamageToggleUiKeyValue, GetMaskDamageToggleUiKeyLabel());
        }

        private void HandleMaskDamageUiRebind()
        {
            if (!waitingForMaskDamageUiRebind)
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

                ApplyMaskDamageUiRebind(key);
                return;
            }
        }

        private void ApplyMaskDamageUiRebind(KeyCode key)
        {
            if (key == KeyCode.Escape)
            {
                waitingForMaskDamageUiRebind = false;
                UpdateKeybindValue(maskDamageToggleUiKeyValue, GetMaskDamageToggleUiKeyLabel());
                return;
            }

            string keyName = key.ToString();
            bool clear = !string.IsNullOrEmpty(maskDamageUiPrevKey)
                && string.Equals(maskDamageUiPrevKey, keyName, StringComparison.OrdinalIgnoreCase);

            if (!clear && TryGetMaskDamageHotkeyConflictOwners(key, out string conflictOwners))
            {
                ShowStatusMessage(string.Format("QuickMenu/HotkeyInUse".Localize(), FormatKeyLabel(key), conflictOwners));
                UpdateKeybindValue(maskDamageToggleUiKeyValue, "Settings/FastReload/SetKey".Localize());
                return;
            }

            MaskDamage.SetToggleUiKeybind(clear ? string.Empty : keyName);
            waitingForMaskDamageUiRebind = false;
            UpdateKeybindValue(maskDamageToggleUiKeyValue, GetMaskDamageToggleUiKeyLabel());
        }

        private bool TryGetMaskDamageHotkeyConflictOwners(KeyCode key, out string ownersText)
        {
            return TryGetHotkeyConflictOwnersExceptSelf(key, "Settings/MaskDamage/ToggleUI".Localize(), out ownersText);
        }

        private bool IsFreezeHitboxesRebinding()
        {
            return waitingForFreezeHitboxesRebind;
        }

        private void StartFreezeHitboxesRebind()
        {
            waitingForFreezeHitboxesRebind = true;
            freezeHitboxesPrevKey = FreezeHitboxes.GetUnfreezeKeybind();
            UpdateKeybindValue(freezeHitboxesUnfreezeKeyValue, "Settings/FastReload/SetKey".Localize());
        }

        private void CancelFreezeHitboxesRebind()
        {
            if (!waitingForFreezeHitboxesRebind)
            {
                return;
            }

            waitingForFreezeHitboxesRebind = false;
            UpdateKeybindValue(freezeHitboxesUnfreezeKeyValue, GetFreezeHitboxesKeyLabel());
        }

        private void HandleFreezeHitboxesRebind()
        {
            if (!waitingForFreezeHitboxesRebind)
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

                ApplyFreezeHitboxesRebind(key);
                return;
            }
        }

        private void ApplyFreezeHitboxesRebind(KeyCode key)
        {
            if (key == KeyCode.Escape)
            {
                waitingForFreezeHitboxesRebind = false;
                UpdateKeybindValue(freezeHitboxesUnfreezeKeyValue, GetFreezeHitboxesKeyLabel());
                return;
            }

            string keyName = key.ToString();
            bool clear = !string.IsNullOrEmpty(freezeHitboxesPrevKey)
                && string.Equals(freezeHitboxesPrevKey, keyName, StringComparison.OrdinalIgnoreCase);

            if (!clear && TryGetHotkeyConflictOwnersExceptSelf(key, "Freeze Hitboxes Unfreeze Hotkey", out string conflictOwners))
            {
                ShowStatusMessage(string.Format("QuickMenu/HotkeyInUse".Localize(), FormatKeyLabel(key), conflictOwners));
                UpdateKeybindValue(freezeHitboxesUnfreezeKeyValue, "Settings/FastReload/SetKey".Localize());
                return;
            }

            FreezeHitboxes.SetUnfreezeKeybind(clear ? string.Empty : keyName);
            waitingForFreezeHitboxesRebind = false;
            UpdateKeybindValue(freezeHitboxesUnfreezeKeyValue, GetFreezeHitboxesKeyLabel());
        }
    }
}
