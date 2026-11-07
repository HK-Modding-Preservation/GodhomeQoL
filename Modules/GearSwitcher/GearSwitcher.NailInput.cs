using Mono.Cecil.Cil;
using MonoMod.Cil;
using IL;
using InControl;
using GodhomeQoL.Modules.BossChallenge;
using GodhomeQoL.Modules.QoL;
using ToggleableBindings;
using ToggleableBindings.VanillaBindings;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class GearSwitcher : Module
{
    internal static void ApplyNailInputImmediate(GearPreset preset)
    {
        if (!IsGloballyEnabled)
        {
            return;
        }

        if (!IsSafeToApply())
        {
            pendingNailInputPreset = preset;
            pendingNailInputApply = true;
            EnsurePendingCoroutine();
            return;
        }

        ApplyNailInput(preset);
    }

    private static void RestoreNailInputState()
    {
        try
        {
            PlayerAction? action = GetNailAttackAction();
            if (action == null)
            {
                return;
            }

            RestoreNailAttack(action);
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "GearSwitcher.NailInput.cs");
        }
    }

    private static void EnsureNailInputState()
    {
        if (!IsGloballyEnabled)
        {
            return;
        }

        string lastPreset = NormalizeBuiltinPresetName(GetLastPresetName());
        if (string.IsNullOrEmpty(lastPreset))
        {
            return;
        }

        if (!TryGetPreset(lastPreset, out GearPreset preset))
        {
            return;
        }

        ApplyNailInputImmediate(preset);
    }

    private static void ApplyNailInput(GearPreset preset)
    {
        PlayerAction? action = GetNailAttackAction();
        if (action == null)
        {
            pendingNailInputPreset = preset;
            pendingNailInputApply = true;
            EnsurePendingCoroutine();
            return;
        }

        if (preset.Nailless)
        {
            DisableNailAttack(action, preserveStored: false);
        }
        else
        {
            RestoreNailAttack(action);
        }
    }

    private static PlayerAction? GetNailAttackAction()
    {
        GameManager? manager = GameManager.instance;
        if (manager == null || manager.inputHandler == null)
        {
            return null;
        }

        return manager.inputHandler.ActionButtonToPlayerAction(HeroActionButton.ATTACK);
    }

    private static void CacheNailAttackBindings(PlayerAction action)
    {
        if (action.Bindings.Count == 0)
        {
            return;
        }

        savedNailAttackBindings.Clear();
        foreach (BindingSource binding in action.Bindings)
        {
            savedNailAttackBindings.Add(binding);
        }

        hasSavedNailAttackBindings = true;
        SaveNailAttackBindings(action);
    }

    private static void SaveNailAttackBindings(PlayerAction action)
    {
        InputHandler.KeyOrMouseBinding keyBinding = KeybindUtil.GetKeyOrMouseBinding(action);
        InputControlType controllerBinding = KeybindUtil.GetControllerButtonBinding(action);

        string keyName = string.Empty;
        string mouseName = string.Empty;

        if (TryGetBindingKey(keyBinding, out Key key) && !EqualityComparer<Key>.Default.Equals(key, default))
        {
            keyName = key.ToString();
        }
        else if (TryGetBindingMouse(keyBinding, out Mouse mouse) && !EqualityComparer<Mouse>.Default.Equals(mouse, default))
        {
            mouseName = mouse.ToString();
        }

        Settings.NailAttackKeyBinding = keyName;
        Settings.NailAttackMouseBinding = mouseName;
        Settings.NailAttackControllerBinding = controllerBinding;
        Settings.NailAttackBindingsStored = !string.IsNullOrEmpty(keyName)
            || !string.IsNullOrEmpty(mouseName)
            || !EqualityComparer<InputControlType>.Default.Equals(controllerBinding, default);

        GodhomeQoL.SaveGlobalSettingsSafe();
    }

    private static void RestoreNailAttackFromSettings(PlayerAction action)
    {
        action.ClearBindings();

        InputControlType controllerBinding = Settings.NailAttackControllerBinding;
        if (!EqualityComparer<InputControlType>.Default.Equals(controllerBinding, default))
        {
            KeybindUtil.AddInputControlType(action, controllerBinding);
        }

        if (!string.IsNullOrEmpty(Settings.NailAttackKeyBinding)
            && Enum.TryParse(Settings.NailAttackKeyBinding, out Key key))
        {
            KeybindUtil.AddKeyOrMouseBinding(action, new InputHandler.KeyOrMouseBinding(key));
            return;
        }

        if (!string.IsNullOrEmpty(Settings.NailAttackMouseBinding)
            && Enum.TryParse(Settings.NailAttackMouseBinding, out Mouse mouse))
        {
            KeybindUtil.AddKeyOrMouseBinding(action, new InputHandler.KeyOrMouseBinding(mouse));
        }
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

    private static void DisableNailAttack(PlayerAction action, bool preserveStored)
    {
        if (!preserveStored || (!hasSavedNailAttackBindings && !Settings.NailAttackBindingsStored))
        {
            CacheNailAttackBindings(action);
        }

        action.ClearBindings();
    }

    private static void RestoreNailAttack(PlayerAction action)
    {
        if (action.Bindings.Count > 0)
        {
            CacheNailAttackBindings(action);
            return;
        }

        if (hasSavedNailAttackBindings)
        {
            action.ClearBindings();
            foreach (BindingSource binding in savedNailAttackBindings)
            {
                action.AddBinding(binding);
            }

            ReleaseAttackKeysFromOtherActions(action);
            return;
        }

        if (Settings.NailAttackBindingsStored)
        {
            RestoreNailAttackFromSettings(action);
            ReleaseAttackKeysFromOtherActions(action);
            CacheNailAttackBindings(action);
        }
        else
        {
            action.ClearBindings();
        }
    }

    private static void ReleaseAttackKeysFromOtherActions(PlayerAction action)
    {
        try
        {
            PlayerActionSet? owner = action.Owner;
            if (owner == null)
            {
                return;
            }

            bool released = false;
            foreach (BindingSource binding in action.Bindings.ToList())
            {
                foreach (PlayerAction other in owner.Actions)
                {
                    if (other == action || !other.HasBinding(binding))
                    {
                        continue;
                    }

                    other.RemoveBinding(binding);
                    released = true;
                }
            }

            if (released)
            {
                QuickMenu.ShowStatusMessageFromExternal("GearSwitcher/AttackKeyRestored".Localize());
            }
        }
        catch (Exception ex)
        {
            LogSuppressed(ex, "GearSwitcher.NailInput.cs/ReleaseAttackKeys");
        }
    }

    private static void EnforceNaillessInput()
    {
        if (!IsGloballyEnabled)
        {
            return;
        }

        string lastPreset = NormalizeBuiltinPresetName(GetLastPresetName());
        if (string.IsNullOrEmpty(lastPreset))
        {
            return;
        }

        if (!TryGetPreset(lastPreset, out GearPreset preset) || !preset.Nailless)
        {
            return;
        }

        PlayerAction? action = GetNailAttackAction();
        if (action == null || action.Bindings.Count == 0)
        {
            return;
        }

        DisableNailAttack(action, preserveStored: true);
    }
}
