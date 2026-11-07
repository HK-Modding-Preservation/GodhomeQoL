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
        private bool IsFastReloadRebinding()
        {
            return waitingForReloadRebind;
        }

        private void StartReloadRebind()
        {
            waitingForReloadRebind = true;
            reloadPrevKey = GetReloadKey();
            UpdateKeybindValue(reloadKeyValue, "Settings/FastReload/SetKey".Localize());
        }

        private void CancelFastReloadRebind()
        {
            if (waitingForReloadRebind)
            {
                waitingForReloadRebind = false;
                UpdateKeybindValue(reloadKeyValue, FormatKeyLabel(GetReloadKey()));
            }
        }

        private void HandleFastReloadRebind()
        {
            if (!IsFastReloadRebinding())
            {
                return;
            }

            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (!Input.GetKeyDown(key) || key == KeyCode.None)
                {
                    continue;
                }

                if (key == KeyCode.Mouse0)
                {
                    continue;
                }

                if (waitingForReloadRebind)
                {
                    ApplyReloadRebind(key);
                    return;
                }
            }
        }

        private void ApplyReloadRebind(KeyCode key)
        {
            if (key == KeyCode.Escape)
            {
                waitingForReloadRebind = false;
                UpdateKeybindValue(reloadKeyValue, FormatKeyLabel(GetReloadKey()));
                return;
            }

            if (key == reloadPrevKey)
            {
                Modules.FastReload.reloadKeyCode = (int)KeyCode.None;
                waitingForReloadRebind = false;
                UpdateKeybindValue(reloadKeyValue, FormatKeyLabel(GetReloadKey()));
                return;
            }

            if (TryGetFastReloadHotkeyConflictOwners(key, out string conflictOwners))
            {
                ShowStatusMessage(string.Format("QuickMenu/HotkeyInUse".Localize(), FormatKeyLabel(key), conflictOwners));
                UpdateKeybindValue(reloadKeyValue, "Settings/FastReload/SetKey".Localize());
                return;
            }

            Modules.FastReload.reloadKeyCode = (int)key;

            waitingForReloadRebind = false;
            UpdateKeybindValue(reloadKeyValue, FormatKeyLabel(GetReloadKey()));
        }

        private bool TryGetFastReloadHotkeyConflictOwners(KeyCode key, out string ownersText)
        {
            List<string> owners = new();
            HashSet<string> unique = new(StringComparer.OrdinalIgnoreCase);

            void AddOwner(string owner)
            {
                if (string.IsNullOrWhiteSpace(owner))
                {
                    return;
                }

                if (unique.Add(owner))
                {
                    owners.Add(owner);
                }
            }

            if (GetQuickMenuToggleKey() == key)
            {
                AddOwner("Settings/QuickMenu/Hotkey".Localize());
            }

            foreach (QuickMenuItemDefinition def in GetOrderedQuickMenuDefinitions())
            {
                if (!IsOverlayHotkeySupported(def.Id))
                {
                    continue;
                }

                if (GetOverlayHotkeyKey(def.Id) == key)
                {
                    AddOwner($"{def.Label} Overlay Hotkey");
                }
            }

            if (KeyCodeMatchesRawKeybind(MaskDamage.GetToggleUiKeybind(), key))
            {
                AddOwner("Settings/MaskDamage/ToggleUI".Localize());
            }

            if (KeyCodeMatchesRawKeybind(NailDamageCheck.GetKeybind(), key))
            {
                AddOwner("Settings/QoL/NailDamageCheck".Localize());
            }

            if (KeyCodeMatchesRawKeybind(FreezeHitboxes.GetUnfreezeKeybind(), key))
            {
                AddOwner("Freeze Hitboxes Unfreeze Hotkey");
            }

            if (KeyCodeMatchesRawKeybind(Modules.Cheats.Cheats.GetKillAllHotkeyRaw(), key))
            {
                AddOwner("Settings/Cheats/KillAllHotkey".Localize());
            }

            if (KeyCodeMatchesRawKeybind(SpeedChanger.toggleKeybind, key))
            {
                AddOwner("SpeedChanger/ToggleKey".Localize());
            }

            if (KeyCodeMatchesRawKeybind(SpeedChanger.inputSpeedKeybind, key))
            {
                AddOwner("SpeedChanger/InputKey".Localize());
            }

            if (Modules.QoL.TeleportKit.MenuHotkey == key)
            {
                AddOwner("TeleportKit/MenuHotkey".Localize());
            }

            if (GetTeleportKitSaveKeyDisplay() == key)
            {
                AddOwner("TeleportKit/SaveHotkey".Localize());
            }

            if (GetTeleportKitTeleportKeyDisplay() == key)
            {
                AddOwner("TeleportKit/TeleportHotkey".Localize());
            }

            if (TryMapKeyCodeToInControlKey(key, out Key mapped))
            {
                string mappedRaw = mapped.ToString();
                if (string.Equals(GetShowHpBindingRaw(), mappedRaw, StringComparison.OrdinalIgnoreCase))
                {
                    AddOwner("ShowHPOnDeath/HudToggleKey".Localize());
                }

                if (string.Equals(GetShowHpBindingRaw(ShowHpSettings.Keybinds.History), mappedRaw, StringComparison.OrdinalIgnoreCase))
                {
                    AddOwner("ShowHPOnDeath/HistoryKey".Localize());
                }

                if (string.Equals(GetFastDreamWarpKeyBindingRaw(), mappedRaw, StringComparison.OrdinalIgnoreCase))
                {
                    AddOwner("Settings/FastDreamWarp/Hotkey".Localize());
                }
            }

            ownersText = string.Join(", ", owners);
            return owners.Count > 0;
        }

        private static KeyCode GetReloadKey() => (KeyCode)Modules.FastReload.reloadKeyCode;
    }
}
