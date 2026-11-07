using Modding;
using Satchel;
using Satchel.Futils;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed class HornetSentinelHelper : Module
{
    private const string HornetSentinelScene = "GG_Hornet_2";
    private const string HornetSentinelName = "Hornet Boss 2";
    private const int DefaultHornetSentinelMaxHp = 1200;
    private const int DefaultHornetSentinelVanillaHp = 1200;
    private const int DefaultHornetSentinelPhase2Hp = 480;
    private const int P5HornetSentinelHp = 850;
    private const int MinHornetSentinelHp = 1;
    private const int MaxHornetSentinelHp = 999999;
    private const int MinHornetSentinelPhase2Hp = 1;

    [LocalSetting]
    [BoolOption]
    internal static bool hornetSentinelUseMaxHp = false;

    [LocalSetting]
    [BoolOption]
    internal static bool hornetSentinelP5Hp = false;

    [LocalSetting]
    internal static int hornetSentinelMaxHp = DefaultHornetSentinelMaxHp;

    [LocalSetting]
    internal static bool hornetSentinelUseCustomPhase = false;

    [LocalSetting]
    internal static int hornetSentinelPhase2Hp = DefaultHornetSentinelPhase2Hp;

    [LocalSetting]
    internal static int hornetSentinelMaxHpBeforeP5 = DefaultHornetSentinelMaxHp;

    [LocalSetting]
    internal static bool hornetSentinelUseCustomPhaseBeforeP5 = false;

    [LocalSetting]
    internal static int hornetSentinelPhase2HpBeforeP5 = DefaultHornetSentinelPhase2Hp;

    [LocalSetting]
    internal static bool hornetSentinelUseMaxHpBeforeP5 = false;

    [LocalSetting]
    internal static bool hornetSentinelHasStoredStateBeforeP5 = false;

    private static readonly Dictionary<int, int> vanillaHpByInstance = new();
    private static readonly Dictionary<int, int> vanillaPhase2HpByFsm = new();
    private static bool moduleActive;
    private static bool hoGEntryAllowed;

    public override ToggleableLevel ToggleableLevel => ToggleableLevel.ChangeScene;

    private protected override void Load()
    {
        moduleActive = true;
        BossManipulateEntryGuard.EnsureHooks();
        NormalizeP5State();
        NormalizePhaseThresholdState();
        vanillaHpByInstance.Clear();
        vanillaPhase2HpByFsm.Clear();
        On.HealthManager.Awake += OnHealthManagerAwake_HornetSentinel;
        On.HealthManager.Start += OnHealthManagerStart_HornetSentinel;
        On.PlayMakerFSM.OnEnable += OnPlayMakerFsmOnEnable_HornetSentinel;
        On.PlayMakerFSM.Start += OnPlayMakerFsmStart_HornetSentinel;
        USceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        ModHooks.BeforeSceneLoadHook += OnBeforeSceneLoad;
    }

    private protected override void Unload()
    {
        RestoreVanillaHealthIfPresent();
        RestoreVanillaPhaseThresholdsIfPresent();
        moduleActive = false;
        On.HealthManager.Awake -= OnHealthManagerAwake_HornetSentinel;
        On.HealthManager.Start -= OnHealthManagerStart_HornetSentinel;
        On.PlayMakerFSM.OnEnable -= OnPlayMakerFsmOnEnable_HornetSentinel;
        On.PlayMakerFSM.Start -= OnPlayMakerFsmStart_HornetSentinel;
        USceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
        ModHooks.BeforeSceneLoadHook -= OnBeforeSceneLoad;
        vanillaHpByInstance.Clear();
        vanillaPhase2HpByFsm.Clear();
        hoGEntryAllowed = false;
    }

    internal static void ReapplyLiveSettings()
    {
        if (!moduleActive)
        {
            return;
        }

        if (ShouldUseCustomHp())
        {
            ApplyHornetSentinelHealthIfPresent();
        }
        else
        {
            RestoreVanillaHealthIfPresent();
        }

        ApplyPhaseThresholdSettingsIfPresent();
    }

    internal static void SetP5HpEnabled(bool value)
    {
        if (value)
        {
            if (!hornetSentinelP5Hp)
            {
                hornetSentinelMaxHpBeforeP5 = ClampHornetSentinelHp(hornetSentinelMaxHp);
                hornetSentinelUseMaxHpBeforeP5 = hornetSentinelUseMaxHp;
                hornetSentinelUseCustomPhaseBeforeP5 = hornetSentinelUseCustomPhase;
                hornetSentinelPhase2HpBeforeP5 = ClampHornetSentinelPhase2Hp(hornetSentinelPhase2Hp);
                hornetSentinelHasStoredStateBeforeP5 = true;
            }

            hornetSentinelP5Hp = true;
            hornetSentinelUseMaxHp = true;
            hornetSentinelUseCustomPhase = false;
            hornetSentinelMaxHp = P5HornetSentinelHp;
        }
        else
        {
            if (hornetSentinelP5Hp && hornetSentinelHasStoredStateBeforeP5)
            {
                hornetSentinelMaxHp = ClampHornetSentinelHp(hornetSentinelMaxHpBeforeP5);
                hornetSentinelUseMaxHp = hornetSentinelUseMaxHpBeforeP5;
                hornetSentinelUseCustomPhase = hornetSentinelUseCustomPhaseBeforeP5;
                hornetSentinelPhase2Hp = ClampHornetSentinelPhase2Hp(hornetSentinelPhase2HpBeforeP5);
            }

            hornetSentinelP5Hp = false;
            hornetSentinelHasStoredStateBeforeP5 = false;
        }

        ReapplyLiveSettings();
    }

    private static void NormalizePhaseThresholdState()
    {
        hornetSentinelPhase2Hp = ClampHornetSentinelPhase2Hp(hornetSentinelPhase2Hp);
    }

    private static void NormalizeP5State()
    {
        if (!hornetSentinelP5Hp)
        {
            return;
        }

        if (!hornetSentinelHasStoredStateBeforeP5)
        {
            hornetSentinelMaxHpBeforeP5 = ClampHornetSentinelHp(hornetSentinelMaxHp);
            hornetSentinelUseMaxHpBeforeP5 = hornetSentinelUseMaxHp;
            hornetSentinelUseCustomPhaseBeforeP5 = hornetSentinelUseCustomPhase;
            hornetSentinelPhase2HpBeforeP5 = ClampHornetSentinelPhase2Hp(hornetSentinelPhase2Hp);
            hornetSentinelHasStoredStateBeforeP5 = true;
        }

        hornetSentinelUseMaxHp = true;
        hornetSentinelUseCustomPhase = false;
        hornetSentinelMaxHp = P5HornetSentinelHp;
    }

    internal static void ApplyHornetSentinelHealthIfPresent()
    {
        if (!moduleActive || !ShouldUseCustomHp())
        {
            return;
        }

        if (!TryFindHornetSentinelHealthManager(out HealthManager? hm))
        {
            return;
        }

        if (hm != null && hm.gameObject != null)
        {
            ApplyHornetSentinelHealth(hm.gameObject, hm);
        }
    }

    internal static void RestoreVanillaHealthIfPresent()
    {
        if (!TryFindHornetSentinelHealthManager(out HealthManager? hm))
        {
            return;
        }

        if (hm != null && hm.gameObject != null)
        {
            RestoreVanillaHealth(hm.gameObject, hm);
        }
    }

    internal static void ApplyPhaseThresholdSettingsIfPresent()
    {
        if (!moduleActive)
        {
            return;
        }

        foreach (PlayMakerFSM fsm in UObject.FindObjectsOfType<PlayMakerFSM>())
        {
            if (fsm == null || fsm.gameObject == null || !IsHornetSentinelPhaseControlFsm(fsm))
            {
                continue;
            }

            ApplyPhaseThresholdSettings(fsm);
        }
    }

    private static void OnHealthManagerAwake_HornetSentinel(On.HealthManager.orig_Awake orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsHornetSentinel(self))
        {
            return;
        }

        RememberVanillaHp(self);
        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }

        if (ShouldUseCustomHp())
        {
            ApplyHornetSentinelHealth(self.gameObject, self);
        }
        else
        {
            RestoreVanillaHealth(self.gameObject, self);
        }
    }

    private static void OnHealthManagerStart_HornetSentinel(On.HealthManager.orig_Start orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsHornetSentinel(self))
        {
            return;
        }

        RememberVanillaHp(self);
        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }

        if (ShouldUseCustomHp())
        {
            ApplyHornetSentinelHealth(self.gameObject, self);
            _ = self.StartCoroutine(DeferredApply(self));
        }
        else
        {
            RestoreVanillaHealth(self.gameObject, self);
        }
    }

    private static void OnPlayMakerFsmOnEnable_HornetSentinel(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
    {
        orig(self);

        if (!moduleActive || self == null || self.gameObject == null || !IsHornetSentinelPhaseControlFsm(self))
        {
            return;
        }

        ApplyPhaseThresholdSettings(self);
    }

    private static void OnPlayMakerFsmStart_HornetSentinel(On.PlayMakerFSM.orig_Start orig, PlayMakerFSM self)
    {
        orig(self);

        if (!moduleActive || self == null || self.gameObject == null || !IsHornetSentinelPhaseControlFsm(self))
        {
            return;
        }

        ApplyPhaseThresholdSettings(self);
    }

    private static IEnumerator DeferredApply(HealthManager hm)
    {
        yield return null;

        if (!moduleActive || hm == null || hm.gameObject == null || !IsHornetSentinel(hm))
        {
            yield break;
        }

        if (ShouldUseCustomHp() && ShouldApplySettings(hm.gameObject))
        {
            ApplyHornetSentinelHealth(hm.gameObject, hm);
            yield return new WaitForSeconds(0.01f);
            if (moduleActive && hm != null && hm.gameObject != null && IsHornetSentinel(hm) && ShouldUseCustomHp() && ShouldApplySettings(hm.gameObject))
            {
                ApplyHornetSentinelHealth(hm.gameObject, hm);
            }
        }
    }

    private static void SceneManager_activeSceneChanged(Scene from, Scene to)
    {
        UpdateHoGEntryAllowed(from.name, to.name);
        if (!string.Equals(to.name, HornetSentinelScene, StringComparison.Ordinal))
        {
            vanillaHpByInstance.Clear();
            vanillaPhase2HpByFsm.Clear();
            return;
        }

        if (!moduleActive)
        {
            return;
        }

        if (ShouldUseCustomHp())
        {
            ApplyHornetSentinelHealthIfPresent();
        }
        else
        {
            RestoreVanillaHealthIfPresent();
        }

        ApplyPhaseThresholdSettingsIfPresent();
    }

    private static string OnBeforeSceneLoad(string newSceneName)
    {
        UpdateHoGEntryAllowed(USceneManager.GetActiveScene().name, newSceneName);
        return newSceneName;
    }

    private static bool IsHornetSentinel(HealthManager hm)
    {
        if (hm == null || hm.gameObject == null)
        {
            return false;
        }

        return IsHornetSentinelObject(hm.gameObject);
    }

    private static bool IsHornetSentinelObject(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return false;
        }

        return string.Equals(gameObject.scene.name, HornetSentinelScene, StringComparison.Ordinal)
            && gameObject.name.StartsWith(HornetSentinelName, StringComparison.Ordinal);
    }

    private static bool ShouldApplySettings(GameObject? gameObject)
    {
        if (gameObject == null || !IsHornetSentinelObject(gameObject))
        {
            return false;
        }

        return hoGEntryAllowed;
    }

    private static bool ShouldUseCustomHp() => hornetSentinelUseMaxHp;

    private static bool ShouldUseCustomPhaseThreshold() => hornetSentinelUseCustomPhase && !hornetSentinelP5Hp;

    private static void UpdateHoGEntryAllowed(string currentScene, string nextScene)
    {
        if (string.Equals(nextScene, HornetSentinelScene, StringComparison.Ordinal))
        {
            if (BossManipulateEntryGuard.IsAllowedBossEntry(currentScene, nextScene))
            {
                hoGEntryAllowed = true;
            }
            else if (string.Equals(currentScene, HornetSentinelScene, StringComparison.Ordinal) && hoGEntryAllowed)
            {
                hoGEntryAllowed = true;
            }
            else
            {
                hoGEntryAllowed = false;
            }

            return;
        }

        hoGEntryAllowed = false;
    }

    private static bool IsHornetSentinelPhaseControlFsm(PlayMakerFSM fsm)
    {
        if (fsm == null || fsm.gameObject == null || !IsHornetSentinelObject(fsm.gameObject))
        {
            return false;
        }

        return string.Equals(fsm.FsmName, "Control", StringComparison.Ordinal)
            || fsm.Fsm?.GetState("Escalation") != null;
    }

    private static void ApplyPhaseThresholdSettings(PlayMakerFSM fsm)
    {
        if (fsm == null || fsm.gameObject == null || !IsHornetSentinelPhaseControlFsm(fsm))
        {
            return;
        }

        if (!ShouldApplySettings(fsm.gameObject))
        {
            return;
        }

        RememberVanillaPhase2Hp(fsm);
        int targetThreshold = ShouldUseCustomPhaseThreshold()
            ? ClampHornetSentinelPhase2Hp(hornetSentinelPhase2Hp)
            : GetVanillaPhase2Hp(fsm);

        SetPhase2ThresholdOnFsm(fsm, targetThreshold);
    }

    private static void RestoreVanillaPhaseThresholdsIfPresent()
    {
        foreach (PlayMakerFSM fsm in UObject.FindObjectsOfType<PlayMakerFSM>())
        {
            if (fsm == null || fsm.gameObject == null || !IsHornetSentinelPhaseControlFsm(fsm))
            {
                continue;
            }

            if (!ShouldApplySettings(fsm.gameObject))
            {
                continue;
            }

            RememberVanillaPhase2Hp(fsm);
            SetPhase2ThresholdOnFsm(fsm, GetVanillaPhase2Hp(fsm));
        }
    }

    private static void SetPhase2ThresholdOnFsm(PlayMakerFSM fsm, int value)
    {
        FsmState? escalation = fsm.Fsm?.GetState("Escalation");
        if (escalation?.Actions == null)
        {
            return;
        }

        int threshold = ClampHornetSentinelPhase2Hp(value);
        foreach (FsmStateAction action in escalation.Actions)
        {
            if (action is IntCompare compare && compare.integer2 != null)
            {
                compare.integer2.Value = threshold;
            }
        }
    }

    private static void RememberVanillaPhase2Hp(PlayMakerFSM fsm)
    {
        int id = fsm.GetInstanceID();
        if (vanillaPhase2HpByFsm.ContainsKey(id))
        {
            return;
        }

        int threshold = DefaultHornetSentinelPhase2Hp;
        FsmState? escalation = fsm.Fsm?.GetState("Escalation");
        if (escalation?.Actions != null)
        {
            foreach (FsmStateAction action in escalation.Actions)
            {
                if (action is IntCompare compare && compare.integer2 != null)
                {
                    threshold = compare.integer2.Value;
                    break;
                }
            }
        }

        vanillaPhase2HpByFsm[id] = ClampHornetSentinelPhase2Hp(threshold);
    }

    private static int GetVanillaPhase2Hp(PlayMakerFSM fsm)
    {
        int id = fsm.GetInstanceID();
        if (vanillaPhase2HpByFsm.TryGetValue(id, out int threshold))
        {
            return ClampHornetSentinelPhase2Hp(threshold);
        }

        RememberVanillaPhase2Hp(fsm);
        if (vanillaPhase2HpByFsm.TryGetValue(id, out threshold))
        {
            return ClampHornetSentinelPhase2Hp(threshold);
        }

        return DefaultHornetSentinelPhase2Hp;
    }

    private static void ApplyHornetSentinelHealth(GameObject hornetSentinel, HealthManager? hm = null)
    {
        if (!ShouldApplySettings(hornetSentinel) || !ShouldUseCustomHp())
        {
            return;
        }

        hm ??= hornetSentinel.GetComponent<HealthManager>();
        if (hm == null)
        {
            return;
        }

        RememberVanillaHp(hm);
        int targetHp = ClampHornetSentinelHp(hornetSentinelMaxHp);
        hornetSentinel.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static void RestoreVanillaHealth(GameObject hornetSentinel, HealthManager? hm = null)
    {
        if (!hoGEntryAllowed)
        {
            return;
        }

        if (hornetSentinel == null || !IsHornetSentinelObject(hornetSentinel))
        {
            return;
        }

        hm ??= hornetSentinel.GetComponent<HealthManager>();
        if (hm == null)
        {
            return;
        }

        if (!TryGetVanillaHp(hm, out int vanillaHp))
        {
            return;
        }

        int targetHp = ClampHornetSentinelHp(vanillaHp);
        hornetSentinel.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static bool TryFindHornetSentinelHealthManager(out HealthManager? hm)
    {
        hm = null;
        foreach (HealthManager candidate in UObject.FindObjectsOfType<HealthManager>())
        {
            if (candidate != null && IsHornetSentinel(candidate))
            {
                hm = candidate;
                return true;
            }
        }

        return false;
    }

    private static void RememberVanillaHp(HealthManager hm)
    {
        int instanceId = hm.GetInstanceID();
        if (vanillaHpByInstance.ContainsKey(instanceId))
        {
            return;
        }

        int hp = hm.hp;

        hp = Math.Max(hp, DefaultHornetSentinelVanillaHp);
        if (hp > 0)
        {
            vanillaHpByInstance[instanceId] = hp;
        }
    }

    private static bool TryGetVanillaHp(HealthManager hm, out int hp)
    {
        if (vanillaHpByInstance.TryGetValue(hm.GetInstanceID(), out hp) && hp > 0)
        {
            hp = Math.Max(hp, DefaultHornetSentinelVanillaHp);
            return true;
        }

        hp = hm.hp;

        hp = Math.Max(hp, DefaultHornetSentinelVanillaHp);
        return hp > 0;
    }

    private static int ClampHornetSentinelHp(int value)
    {
        if (value < MinHornetSentinelHp)
        {
            return MinHornetSentinelHp;
        }

        return value > MaxHornetSentinelHp ? MaxHornetSentinelHp : value;
    }

    private static int ClampHornetSentinelPhase2Hp(int value)
    {
        int maxPhase2Hp = ResolvePhase2MaxHp();

        if (value < MinHornetSentinelPhase2Hp)
        {
            return MinHornetSentinelPhase2Hp;
        }

        return value > maxPhase2Hp ? maxPhase2Hp : value;
    }

    private static int ResolvePhase2MaxHp()
    {
        return ShouldUseCustomHp()
            ? ClampHornetSentinelHp(hornetSentinelMaxHp)
            : DefaultHornetSentinelVanillaHp;
    }

    internal static int GetPhase2MaxHpForUi()
    {
        return ResolvePhase2MaxHp();
    }
}
