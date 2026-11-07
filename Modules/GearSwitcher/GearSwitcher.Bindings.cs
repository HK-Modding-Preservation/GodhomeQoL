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
    private const int SoulHudIndicatorResyncMaxFrames = 3600;

    private static void UpdatePantheonShellBindingState()
    {
        if (!IsGloballyEnabled)
        {
            return;
        }

        bool bossRush = PlayerData.instance != null && PlayerData.instance.bossRushMode;
        bool shellBound = bossRush && GetPantheonShellBindingSelected();
        if (bossRush == pantheonActive && shellBound == pantheonShellBound)
        {
            return;
        }

        pantheonActive = bossRush;
        pantheonShellBound = shellBound;
        ApplyPantheonShellBindingOverride();
    }

    private static bool GetPantheonShellBindingSelected()
    {
        if (IsGloballyEnabled)
        {
            return false;
        }

        try
        {
            Type type = typeof(BossSequenceController);
            foreach (string fieldName in new[] { "boundShell", "boundHeart" })
            {
                FieldInfo? field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null && field.FieldType == typeof(bool))
                {
                    return (bool)(field.GetValue(null) ?? false);
                }
            }
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "GearSwitcher.Bindings.cs");
        }

        return false;
    }

    private static void ApplyPantheonShellBindingOverride()
    {
        if (!IsGloballyEnabled)
        {
            return;
        }

        if (!TryGetPreset(GetLastPresetName(), out GearPreset preset))
        {
            return;
        }

        bool shouldEnableShell = GetPresetShellBindingState(preset);
        if (IsPantheonSequenceActive() && pantheonShellBound)
        {
            SetBinding<ShellBinding>(false);
            return;
        }

        SetBinding<ShellBinding>(shouldEnableShell);
    }

    private static bool GetBossBindingFlag(string propertyName, params string[] fieldNames)
    {
        try
        {
            Type type = typeof(BossSequenceController);
            PropertyInfo? prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (prop != null && prop.PropertyType == typeof(bool))
            {
                return (bool)(prop.GetValue(null, null) ?? false);
            }

            foreach (string fieldName in fieldNames)
            {
                FieldInfo? field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null && field.FieldType == typeof(bool))
                {
                    return (bool)(field.GetValue(null) ?? false);
                }
            }
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "GearSwitcher.Bindings.cs");
        }

        return false;
    }

    private static void SetBossBindingFlag(bool value, string propertyName, params string[] fieldNames)
    {
        try
        {
            Type type = typeof(BossSequenceController);
            PropertyInfo? prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (prop != null && prop.PropertyType == typeof(bool) && prop.CanWrite)
            {
                prop.SetValue(null, value, null);
                return;
            }

            foreach (string fieldName in fieldNames)
            {
                FieldInfo? field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null && field.FieldType == typeof(bool))
                {
                    field.SetValue(null, value);
                    return;
                }
            }
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "GearSwitcher.Bindings.cs");
        }
    }

    internal static void ApplyBindingsImmediate(GearPreset preset)
    {
        if (!IsGloballyEnabled)
        {
            return;
        }

        if (!IsSafeToApply())
        {
            pendingBindingsPreset = preset;
            pendingBindingsApply = true;
            EnsurePendingCoroutine();
            return;
        }

        ApplyBindings(preset);
    }

    internal static void RestoreAllBindingsImmediate()
        => RestoreAllBindingsImmediate(force: false);

    internal static void RestoreAllBindingsImmediate(bool force)
    {
        if (!force && !IsGloballyEnabled)
        {
            return;
        }

        try
        {
            BindingManager.RestoreBinding<NailBinding>();
            ForceRestoreShellBindingHard();
            BindingManager.RestoreBinding<CharmsBinding>();
            BindingManager.RestoreBinding<SoulBinding>();
            TryRefreshNailDamage();
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "GearSwitcher.Bindings.cs");
        }
    }

    private static void RunStartupShellCleanup(GearPreset? startupPreset)
    {
        if (!IsGloballyEnabled)
        {
            return;
        }

        if (IsPantheonSequenceActive() && pantheonShellBound)
        {
            return;
        }

        bool shouldEnableShell = IsGloballyEnabled
            && startupPreset != null
            && GetPresetShellBindingState(startupPreset);
        bool shellApplied = IsShellBindingApplied();
        bool staleBossShellFlag = GetBossBindingFlag("BoundShell", "boundShell", "boundHeart");

        if (shouldEnableShell)
        {
            if (!shellApplied)
            {
                SetBinding<ShellBinding>(true);
            }

            return;
        }

        if (shellApplied || staleBossShellFlag)
        {
            ForceRestoreShellBindingHard();
        }
    }

    private static void ForceRestoreShellBinding()
    {
        ForceRestoreShellBindingHard();
    }

    private static bool IsShellBindingApplied()
    {
        try
        {
            if (BindingManager.TryGetBinding<ShellBinding>(out ShellBinding? binding) && binding != null)
            {
                return binding.IsApplied;
            }
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "GearSwitcher.Bindings.cs");
        }

        return false;
    }

    private static void ApplyBindings(GearPreset preset, bool forceResync = false)
    {
        try
        {
            bool applyCharms = preset.HasAllBindings || GetPresetBool(preset.Bindings, "CharmsBinding");
            bool applyNail = preset.HasAllBindings || GetPresetBool(preset.Bindings, "NailBinding");
            bool applyShell = GetPresetShellBindingState(preset);
            bool applySoul = preset.HasAllBindings || GetPresetBool(preset.Bindings, "SoulBinding");

            if (IsPantheonSequenceActive() && pantheonShellBound)
            {
                applyShell = false;
            }

            bool charmsChanged = IsBindingApplied<CharmsBinding>() != applyCharms;
            bool soulChanged = forceResync || IsBindingApplied<SoulBinding>() != applySoul;
            bool changed = soulChanged
                || charmsChanged
                || IsBindingApplied<NailBinding>() != applyNail
                || IsShellBindingApplied() != applyShell;

            if (applyCharms)
            {
                SetCharmsBindingExemptCharms();
            }

            SetBinding<CharmsBinding>(applyCharms);
            SetBinding<NailBinding>(applyNail);
            SetShellBinding(applyShell);
            SetBinding<SoulBinding>(applySoul);

            if (applyCharms && (charmsChanged || forceResync))
            {
                RemoveCharmsExceptVoidHeart();
            }

            if (changed)
            {
                TryRefreshNailDamage();
                ResyncBindingHudIndicators(soulChanged);
                ScheduleBindingHudResync(soulChanged);
            }
        }
        catch (Exception ex)
        {
            LogError($"[GearSwitcher] ApplyBindings failed: {ex}");
        }
    }

    private static void SetShellBinding(bool value)
    {
        if (value)
        {
            SetBinding<ShellBinding>(true);
            return;
        }

        ForceRestoreShellBindingHard();
    }

    private static void ForceRestoreShellBindingHard()
    {
        bool refreshHealthHud = IsShellBindingApplied();
        SetBinding<ShellBinding>(false);

        if (IsGloballyEnabled)
        {
            pantheonShellBound = false;
        }

        bool preservePantheonSelection = IsPantheonSequenceActive() && pantheonShellBound;
        if (!preservePantheonSelection)
        {
            refreshHealthHud |= GetBossBindingFlag("BoundShell", "boundShell", "boundHeart");
            SetBossBindingFlag(false, "BoundShell", "boundShell", "boundHeart");
        }

        if (refreshHealthHud)
        {
            ScheduleHealthHudRefresh();
        }

        int generation = coroutineGeneration;
        _ = GlobalCoroutineExecutor.Start(EnsureShellBindingFullyRestored(generation));
    }

    private static IEnumerator EnsureShellBindingFullyRestored(int generation)
    {
        for (int i = 0; i < 3; i++)
        {
            if (generation != coroutineGeneration)
            {
                yield break;
            }

            yield return null;

            bool preservePantheonSelection = IsPantheonSequenceActive() && pantheonShellBound;
            if (!preservePantheonSelection)
            {
                SetBossBindingFlag(false, "BoundShell", "boundShell", "boundHeart");
            }

            if (!IsShellBindingApplied())
            {
                yield break;
            }

            SetBinding<ShellBinding>(false);
            ScheduleHealthHudRefresh();
        }
    }

    private static bool IsPantheonSequenceActive()
    {
        try
        {
            return BossSequenceController.IsInSequence;
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "GearSwitcher.Bindings.cs");
            return false;
        }
    }

    private static void SetBinding<T>(bool value) where T : Binding, new()
    {
        try
        {
            if (value)
            {
                BindingManager.ApplyBinding<T>();
            }
            else
            {
                BindingManager.RestoreBinding<T>();
            }
        }
        catch (Exception ex)
        {
            string action = value ? "apply" : "restore";
            LogError($"[GearSwitcher] Failed to {action} binding {typeof(T).Name}: {ex.Message}");
        }
    }

    private static void TryRestartHudChild(string childPath)
    {
        try
        {
            Transform? child = Ref.GC?.hudCanvas?.transform.Find(childPath);
            if (child == null || !child.gameObject.activeInHierarchy)
            {
                return;
            }

            child.gameObject.SetActive(false);
            child.gameObject.SetActive(true);
            ShowHudIfSafe();
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "GearSwitcher.Bindings.cs");
        }
    }

    private static void ShowHudIfSafe()
    {
        if (!IsSafeToApply() || GameManager.instance.IsGamePaused())
        {
            return;
        }

        Ref.GC?.hudCanvas?.LocateMyFSM("Slide Out")?.SendEvent("IN");
    }

    private static void TryRefreshVesselsHud() => TryRestartHudChild("Soul Orb/Vessels");

    private static void RefreshChangedVessels(int previousReserveMax, int reserveMax)
    {
        int previousCount = previousReserveMax / 33;
        int count = reserveMax / 33;
        for (int vessel = Math.Min(previousCount, count) + 1; vessel <= Math.Max(previousCount, count) && vessel <= 4; vessel++)
        {
            TryRestartHudChild($"Soul Orb/Vessels/Vessel {vessel}");
        }

        if (IsBindingApplied<SoulBinding>() && IsHudEventRegistered(BindVesselOrbEvent))
        {
            EventRegister.SendEvent(BindVesselOrbEvent);
        }
    }

    private static void TryRefreshHealthHud() => TryRestartHudChild("Health");

    private static void ScheduleHealthHudRefresh()
    {
        int generation = coroutineGeneration;
        int token = ++healthHudRefreshToken;
        _ = GlobalCoroutineExecutor.Start(DelayedHealthHudRefresh(generation, token));
    }

    private static IEnumerator DelayedHealthHudRefresh(int generation, int token)
    {
        for (int i = 0; i < 3; i++)
        {
            yield return null;
            if (generation != coroutineGeneration || token != healthHudRefreshToken)
            {
                yield break;
            }
        }

        TryRefreshHealthHud();
    }

    private static void ResyncBindingHudIndicators(bool includeSoul)
    {
        try
        {
            bool nailApplied = IsBindingApplied<NailBinding>();
            bool charmsApplied = IsBindingApplied<CharmsBinding>();
            bool soulApplied = IsBindingApplied<SoulBinding>();

            EventRegister.SendEvent(nailApplied ? ShowBoundNailEvent : HideBoundNailEvent);
            EventRegister.SendEvent(charmsApplied ? ShowBoundCharmsEvent : HideBoundCharmsEvent);
            if (includeSoul)
            {
                TryResyncSoulBindingIndicator(soulApplied);
            }

            PlayMakerFSM.BroadcastEvent(CharmIndicatorCheckEvent);
            PlayMakerFSM.BroadcastEvent(UpdateBlueHealthEvent);
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "GearSwitcher.Bindings.cs");
        }
    }

    private static void TryResyncSoulBindingIndicator(bool soulApplied)
    {
        try
        {
            GameManager? gm = GameManager.instance;
            gm?.soulOrb_fsm?.SendEvent(MPLoseEvent);
            SoulBinding.SyncBindingCap();
            if (soulApplied)
            {
                gm?.soulVessel_fsm?.SendEvent(MPReserveDownEvent);
                if (IsHudEventRegistered(BindVesselOrbEvent))
                {
                    EventRegister.SendEvent(BindVesselOrbEvent);
                    return;
                }

                ScheduleDelayedSoulBindingIndicatorResync(soulApplied: true);
            }
            else
            {
                gm?.soulVessel_fsm?.SendEvent(MPReserveUpEvent);
                if (IsHudEventRegistered(UnbindVesselOrbEvent))
                {
                    EventRegister.SendEvent(UnbindVesselOrbEvent);
                    return;
                }

                ScheduleDelayedSoulBindingIndicatorResync(soulApplied: false);
            }
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "GearSwitcher.Bindings.cs");
        }
    }

    private static IEnumerator WaitForSoulBindingHudEventAndResync(int generation, int token, bool soulApplied)
    {
        string eventName = soulApplied ? BindVesselOrbEvent : UnbindVesselOrbEvent;
        for (int i = 0; i < SoulHudIndicatorResyncMaxFrames; i++)
        {
            if (generation != coroutineGeneration || token != soulBindingHudResyncToken)
            {
                yield break;
            }

            if (IsHudEventRegistered(eventName))
            {
                break;
            }

            yield return null;
        }

        if (generation != coroutineGeneration || token != soulBindingHudResyncToken)
        {
            yield break;
        }

        if (!IsHudEventRegistered(eventName))
        {
            yield break;
        }

        try
        {
            GameManager? gm = GameManager.instance;
            gm?.soulOrb_fsm?.SendEvent(MPLoseEvent);
            if (soulApplied)
            {
                gm?.soulVessel_fsm?.SendEvent(MPReserveDownEvent);
                EventRegister.SendEvent(BindVesselOrbEvent);
            }
            else
            {
                gm?.soulVessel_fsm?.SendEvent(MPReserveUpEvent);
                EventRegister.SendEvent(UnbindVesselOrbEvent);
            }
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "GearSwitcher.Bindings.cs");
        }
    }

    private static bool IsHudEventRegistered(string eventName)
    {
        try
        {
            return !string.IsNullOrEmpty(eventName)
                && EventRegister.eventRegister != null
                && EventRegister.eventRegister.ContainsKey(eventName);
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "GearSwitcher.Bindings.cs");
            return false;
        }
    }

    private static bool IsBindingApplied<T>() where T : Binding
    {
        try
        {
            return BindingManager.TryGetBinding<T>(out T? binding) && binding != null && binding.IsApplied;
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "GearSwitcher.Bindings.cs");
            return false;
        }
    }
}
