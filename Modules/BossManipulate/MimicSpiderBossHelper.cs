using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using Satchel;
using Satchel.Futils;

namespace GodhomeQoL.Modules.BossChallenge;

public abstract class MimicSpiderBossHelper : Module
{
    private protected const string BossName = "Mimic Spider";
    private protected const string BossPhaseCheckStateName = "Roof Jump?";
    private protected const int DefaultBossVanillaHp = 680;
    private protected const int DefaultBossPhase2Hp = 560;
    private protected const int MinBossHp = 1;
    private protected const int MaxBossHp = 999999;
    private protected const int MinBossPhase2Hp = 1;

    private readonly Dictionary<int, int> vanillaHpByInstance = new();
    private bool moduleActive;
    private bool hoGEntryAllowed;

    private protected abstract string SceneName { get; }
    private protected abstract bool UseMaxHp { get; set; }
    private protected abstract int MaxHp { get; set; }
    private protected abstract bool UseCustomPhase { get; set; }
    private protected abstract int Phase2Hp { get; set; }

    public override ToggleableLevel ToggleableLevel => ToggleableLevel.ChangeScene;

    private protected override void Load()
    {
        moduleActive = true;
        BossManipulateEntryGuard.EnsureHooks();
        NormalizePhaseThresholdState();
        vanillaHpByInstance.Clear();
        On.HealthManager.Awake += OnHealthManagerAwake;
        On.HealthManager.Start += OnHealthManagerStart;
        On.PlayMakerFSM.OnEnable += OnPlayMakerFsmOnEnable_Boss;
        On.PlayMakerFSM.Start += OnPlayMakerFsmStart_Boss;
        USceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        ModHooks.BeforeSceneLoadHook += OnBeforeSceneLoad;
    }

    private protected override void Unload()
    {
        RestoreVanillaHealthIfPresentCore();
        RestoreVanillaPhaseThresholdsIfPresentCore();
        moduleActive = false;
        On.HealthManager.Awake -= OnHealthManagerAwake;
        On.HealthManager.Start -= OnHealthManagerStart;
        On.PlayMakerFSM.OnEnable -= OnPlayMakerFsmOnEnable_Boss;
        On.PlayMakerFSM.Start -= OnPlayMakerFsmStart_Boss;
        USceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
        ModHooks.BeforeSceneLoadHook -= OnBeforeSceneLoad;
        vanillaHpByInstance.Clear();
        hoGEntryAllowed = false;
    }

    internal void ReapplyLiveSettingsCore()
    {
        if (!moduleActive)
        {
            return;
        }

        if (ShouldUseCustomHp())
        {
            ApplyHealthIfPresentCore();
        }
        else
        {
            RestoreVanillaHealthIfPresentCore();
        }

        ApplyPhaseThresholdSettingsIfPresentCore();
    }

    private void NormalizePhaseThresholdState()
    {
        Phase2Hp = ClampBossPhase2Hp(Phase2Hp, ResolvePhase2MaxHp());
    }

    internal void ApplyHealthIfPresentCore()
    {
        if (!moduleActive || !ShouldUseCustomHp())
        {
            return;
        }

        if (!TryFindBossHealthManager(out HealthManager? hm))
        {
            return;
        }

        if (hm != null && hm.gameObject != null)
        {
            ApplyBossHealth(hm.gameObject, hm);
        }
    }

    internal void RestoreVanillaHealthIfPresentCore()
    {
        if (!TryFindBossHealthManager(out HealthManager? hm))
        {
            return;
        }

        if (hm != null && hm.gameObject != null)
        {
            RestoreVanillaHealth(hm.gameObject, hm);
        }
    }

    private void OnHealthManagerAwake(On.HealthManager.orig_Awake orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsBoss(self))
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
            ApplyBossHealth(self.gameObject, self);
        }
        else
        {
            RestoreVanillaHealth(self.gameObject, self);
        }
    }

    private void OnHealthManagerStart(On.HealthManager.orig_Start orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsBoss(self))
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
            ApplyBossHealth(self.gameObject, self);
            _ = self.StartCoroutine(DeferredApply(self));
        }
        else
        {
            RestoreVanillaHealth(self.gameObject, self);
        }
    }

    private void OnPlayMakerFsmOnEnable_Boss(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
    {
        orig(self);

        if (!moduleActive || self == null || self.gameObject == null || !IsBossPhaseControlFsm(self))
        {
            return;
        }

        ApplyPhaseThresholdSettings(self);
    }

    private void OnPlayMakerFsmStart_Boss(On.PlayMakerFSM.orig_Start orig, PlayMakerFSM self)
    {
        orig(self);

        if (!moduleActive || self == null || self.gameObject == null || !IsBossPhaseControlFsm(self))
        {
            return;
        }

        ApplyPhaseThresholdSettings(self);
    }

    private IEnumerator DeferredApply(HealthManager hm)
    {
        yield return null;

        if (!moduleActive || hm == null || hm.gameObject == null || !IsBoss(hm))
        {
            yield break;
        }

        if (ShouldUseCustomHp() && ShouldApplySettings(hm.gameObject))
        {
            ApplyBossHealth(hm.gameObject, hm);
            yield return new WaitForSeconds(0.01f);
            if (moduleActive && hm != null && hm.gameObject != null && IsBoss(hm) && ShouldUseCustomHp() && ShouldApplySettings(hm.gameObject))
            {
                ApplyBossHealth(hm.gameObject, hm);
            }
        }

        ApplyPhaseThresholdSettingsIfPresentCore();
    }

    private void SceneManager_activeSceneChanged(Scene from, Scene to)
    {
        UpdateHoGEntryAllowed(from.name, to.name);
        if (!string.Equals(to.name, SceneName, StringComparison.Ordinal))
        {
            vanillaHpByInstance.Clear();
            return;
        }

        if (!moduleActive)
        {
            return;
        }

        if (ShouldUseCustomHp())
        {
            ApplyHealthIfPresentCore();
        }
        else
        {
            RestoreVanillaHealthIfPresentCore();
        }

        ApplyPhaseThresholdSettingsIfPresentCore();
    }

    private string OnBeforeSceneLoad(string newSceneName)
    {
        UpdateHoGEntryAllowed(USceneManager.GetActiveScene().name, newSceneName);
        return newSceneName;
    }

    private bool IsBoss(HealthManager hm)
    {
        if (hm == null || hm.gameObject == null)
        {
            return false;
        }

        return IsBossObject(hm.gameObject);
    }

    private bool IsBossObject(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return false;
        }

        return string.Equals(gameObject.scene.name, SceneName, StringComparison.Ordinal)
            && gameObject.name.StartsWith(BossName, StringComparison.Ordinal);
    }

    private bool ShouldApplySettings(GameObject? gameObject)
    {
        if (gameObject == null || !IsBossObject(gameObject))
        {
            return false;
        }

        return hoGEntryAllowed;
    }

    private bool ShouldUseCustomHp() => UseMaxHp;
    private bool ShouldUseCustomPhaseThreshold() => UseCustomPhase;

    private void UpdateHoGEntryAllowed(string currentScene, string nextScene)
    {
        if (string.Equals(nextScene, SceneName, StringComparison.Ordinal))
        {
            if (BossManipulateEntryGuard.IsAllowedBossEntry(currentScene, nextScene))
            {
                hoGEntryAllowed = true;
            }
            else if (string.Equals(currentScene, SceneName, StringComparison.Ordinal) && hoGEntryAllowed)
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

    internal void ApplyPhaseThresholdSettingsIfPresentCore()
    {
        if (!moduleActive)
        {
            return;
        }

        foreach (PlayMakerFSM fsm in UObject.FindObjectsOfType<PlayMakerFSM>())
        {
            if (fsm == null || fsm.gameObject == null || !IsBossPhaseControlFsm(fsm))
            {
                continue;
            }

            ApplyPhaseThresholdSettings(fsm);
        }
    }

    internal void RestoreVanillaPhaseThresholdsIfPresentCore()
    {
        foreach (PlayMakerFSM fsm in UObject.FindObjectsOfType<PlayMakerFSM>())
        {
            if (fsm == null || fsm.gameObject == null || !IsBossPhaseControlFsm(fsm))
            {
                continue;
            }

            if (!ShouldApplyPhaseSettings(fsm.gameObject))
            {
                continue;
            }

            SetPhase2ThresholdOnFsm(fsm, GetVanillaPhase2Hp());
        }
    }

    private bool IsBossPhaseControlFsm(PlayMakerFSM fsm)
    {
        if (fsm == null || fsm.gameObject == null || !IsBossObject(fsm.gameObject))
        {
            return false;
        }

        return fsm.Fsm?.GetState(BossPhaseCheckStateName) != null;
    }

    private bool ShouldApplyPhaseSettings(GameObject? gameObject)
    {
        return ShouldApplySettings(gameObject);
    }

    private void ApplyPhaseThresholdSettings(PlayMakerFSM fsm)
    {
        if (fsm == null || fsm.gameObject == null || !IsBossPhaseControlFsm(fsm))
        {
            return;
        }

        if (!ShouldApplyPhaseSettings(fsm.gameObject))
        {
            return;
        }

        int threshold = ShouldUseCustomPhaseThreshold()
            ? ClampBossPhase2Hp(Phase2Hp, ResolvePhase2MaxHp())
            : GetVanillaPhase2Hp();

        SetPhase2ThresholdOnFsm(fsm, threshold);
    }

    private void SetPhase2ThresholdOnFsm(PlayMakerFSM fsm, int value)
    {
        if (fsm == null)
        {
            return;
        }

        int threshold = Math.Max(MinBossPhase2Hp, value);
        FsmState? checkState = fsm.Fsm?.GetState(BossPhaseCheckStateName);
        if (checkState?.Actions == null)
        {
            return;
        }

        foreach (FsmStateAction action in checkState.Actions)
        {
            if (action is not IntCompare compare || !IsHpCompare(compare))
            {
                continue;
            }

            FsmInt? thresholdOperand = ResolveThresholdOperand(compare);
            if (thresholdOperand == null)
            {
                continue;
            }

            thresholdOperand.UseVariable = false;
            thresholdOperand.Name = string.Empty;
            thresholdOperand.Value = threshold;
        }
    }

    private int GetVanillaPhase2Hp()
    {
        return DefaultBossPhase2Hp;
    }

    private bool IsHpCompare(IntCompare compare)
    {
        return (compare.integer1 != null
            && string.Equals(compare.integer1.Name, "HP", StringComparison.Ordinal))
            || (compare.integer2 != null
                && string.Equals(compare.integer2.Name, "HP", StringComparison.Ordinal));
    }

    private FsmInt? ResolveThresholdOperand(IntCompare compare)
    {
        if (compare == null)
        {
            return null;
        }

        FsmInt? integer1 = compare.integer1;
        FsmInt? integer2 = compare.integer2;
        if (integer1 == null && integer2 == null)
        {
            return null;
        }

        if (integer1 != null && string.Equals(integer1.Name, "HP", StringComparison.Ordinal))
        {
            return integer2 ?? integer1;
        }

        if (integer2 != null && string.Equals(integer2.Name, "HP", StringComparison.Ordinal))
        {
            return integer1 ?? integer2;
        }

        return integer2 ?? integer1;
    }

    private void ApplyBossHealth(GameObject boss, HealthManager? hm = null)
    {
        if (!ShouldApplySettings(boss) || !ShouldUseCustomHp())
        {
            return;
        }

        hm ??= boss.GetComponent<HealthManager>();
        if (hm == null)
        {
            return;
        }

        RememberVanillaHp(hm);
        int targetHp = ClampBossHp(MaxHp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private void RestoreVanillaHealth(GameObject boss, HealthManager? hm = null)
    {
        if (!hoGEntryAllowed)
        {
            return;
        }

        if (boss == null || !IsBossObject(boss))
        {
            return;
        }

        hm ??= boss.GetComponent<HealthManager>();
        if (hm == null)
        {
            return;
        }

        if (!TryGetVanillaHp(hm, out int vanillaHp))
        {
            return;
        }

        int targetHp = ClampBossHp(vanillaHp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private bool TryFindBossHealthManager(out HealthManager? hm)
    {
        hm = null;
        foreach (HealthManager candidate in UObject.FindObjectsOfType<HealthManager>())
        {
            if (candidate != null && IsBoss(candidate))
            {
                hm = candidate;
                return true;
            }
        }

        return false;
    }

    private void RememberVanillaHp(HealthManager hm)
    {
        int instanceId = hm.GetInstanceID();
        if (vanillaHpByInstance.ContainsKey(instanceId))
        {
            return;
        }

        int hp = hm.hp;

        hp = Math.Max(hp, DefaultBossVanillaHp);
        if (hp > 0)
        {
            vanillaHpByInstance[instanceId] = hp;
        }
    }

    private bool TryGetVanillaHp(HealthManager hm, out int hp)
    {
        if (vanillaHpByInstance.TryGetValue(hm.GetInstanceID(), out hp) && hp > 0)
        {
            hp = Math.Max(hp, DefaultBossVanillaHp);
            return true;
        }

        hp = hm.hp;

        hp = Math.Max(hp, DefaultBossVanillaHp);
        return hp > 0;
    }

    private int ClampBossHp(int value)
    {
        if (value < MinBossHp)
        {
            return MinBossHp;
        }

        return value > MaxBossHp ? MaxBossHp : value;
    }

    internal int GetPhase2MaxHpForUiCore()
    {
        return ResolvePhase2MaxHp();
    }

    private int ClampBossPhase2Hp(int value, int maxHp)
    {
        int clampedMaxHp = ClampBossHp(maxHp);
        if (value < MinBossPhase2Hp)
        {
            return MinBossPhase2Hp;
        }

        return value > clampedMaxHp ? clampedMaxHp : value;
    }

    private int ResolvePhase2MaxHp()
    {
        if (ShouldUseCustomHp())
        {
            return ClampBossHp(MaxHp);
        }

        if (TryFindBossHealthManager(out HealthManager? hm) && hm != null && TryGetVanillaHp(hm, out int vanillaHp))
        {
            return ClampBossHp(vanillaHp);
        }

        return DefaultBossVanillaHp;
    }
}
