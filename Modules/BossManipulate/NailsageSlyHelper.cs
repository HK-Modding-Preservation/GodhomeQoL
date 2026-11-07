using Modding;
using Satchel;
using Satchel.Futils;
using HutongGames.PlayMaker;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed class NailsageSlyHelper : Module
{
    private const string NailsageSlyScene = "GG_Sly";
    private const string NailsageSlyName = "Sly Boss";
    private const string AscendedHpVariableName = "Ascended HP";
    private const string LegacyAscendedHpVariableName = "Acended HP";
    private const int DefaultNailsageSlyPhase1Hp = 1200;
    private const int DefaultNailsageSlyPhase2Hp = 600;
    private const int P5NailsageSlyPhase1Hp = 800;
    private const int P5NailsageSlyPhase2Hp = 250;
    private const int MinNailsageSlyHp = 1;
    private const int MaxNailsageSlyHp = 999999;

    [LocalSetting]
    [BoolOption]
    internal static bool nailsageSlyP5Hp = false;

    [LocalSetting]
    internal static int nailsageSlyPhase1Hp = DefaultNailsageSlyPhase1Hp;

    [LocalSetting]
    internal static int nailsageSlyPhase2Hp = DefaultNailsageSlyPhase2Hp;

    [LocalSetting]
    internal static int nailsageSlyPhase1HpBeforeP5 = DefaultNailsageSlyPhase1Hp;

    [LocalSetting]
    internal static int nailsageSlyPhase2HpBeforeP5 = DefaultNailsageSlyPhase2Hp;

    [LocalSetting]
    internal static bool nailsageSlyHasStoredStateBeforeP5 = false;

    private static readonly Dictionary<int, int> vanillaHpByInstance = new();
    private static readonly Dictionary<int, int> vanillaPhase2HpByFsm = new();
    private static readonly Dictionary<int, bool> phase2ActiveByInstance = new();
    private static readonly Dictionary<int, bool> pendingPhase2ApplyByInstance = new();
    private static bool moduleActive;
    private static bool hoGEntryAllowed;

    public override ToggleableLevel ToggleableLevel => ToggleableLevel.ChangeScene;

    private protected override void Load()
    {
        moduleActive = true;
        BossManipulateEntryGuard.EnsureHooks();
        NormalizeP5State();
        vanillaHpByInstance.Clear();
        vanillaPhase2HpByFsm.Clear();
        phase2ActiveByInstance.Clear();
        pendingPhase2ApplyByInstance.Clear();
        On.HealthManager.Awake += OnHealthManagerAwake_NailsageSly;
        On.HealthManager.Start += OnHealthManagerStart_NailsageSly;
        On.HealthManager.Update += OnHealthManagerUpdate_NailsageSly;
        On.PlayMakerFSM.OnEnable += OnPlayMakerFsmOnEnable_NailsageSly;
        On.PlayMakerFSM.Start += OnPlayMakerFsmStart_NailsageSly;
        USceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        ModHooks.BeforeSceneLoadHook += OnBeforeSceneLoad;
    }

    private protected override void Unload()
    {
        RestoreVanillaHealthIfPresent();
        RestoreVanillaPhase2HpIfPresent();
        moduleActive = false;
        On.HealthManager.Awake -= OnHealthManagerAwake_NailsageSly;
        On.HealthManager.Start -= OnHealthManagerStart_NailsageSly;
        On.HealthManager.Update -= OnHealthManagerUpdate_NailsageSly;
        On.PlayMakerFSM.OnEnable -= OnPlayMakerFsmOnEnable_NailsageSly;
        On.PlayMakerFSM.Start -= OnPlayMakerFsmStart_NailsageSly;
        USceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
        ModHooks.BeforeSceneLoadHook -= OnBeforeSceneLoad;
        vanillaHpByInstance.Clear();
        vanillaPhase2HpByFsm.Clear();
        phase2ActiveByInstance.Clear();
        pendingPhase2ApplyByInstance.Clear();
        hoGEntryAllowed = false;
    }

    internal static void ReapplyLiveSettings()
    {
        if (!moduleActive)
        {
            return;
        }

        ApplyNailsageSlyHealthIfPresent();
        ApplyPhase2HpSettingsIfPresent();
    }

    internal static void SetP5HpEnabled(bool value)
    {
        if (value)
        {
            if (!nailsageSlyP5Hp)
            {
                nailsageSlyPhase1HpBeforeP5 = ClampNailsageSlyHp(nailsageSlyPhase1Hp);
                nailsageSlyPhase2HpBeforeP5 = ClampNailsageSlyHp(nailsageSlyPhase2Hp);
                nailsageSlyHasStoredStateBeforeP5 = true;
            }

            nailsageSlyP5Hp = true;
            nailsageSlyPhase1Hp = P5NailsageSlyPhase1Hp;
            nailsageSlyPhase2Hp = P5NailsageSlyPhase2Hp;
        }
        else
        {
            if (nailsageSlyP5Hp && nailsageSlyHasStoredStateBeforeP5)
            {
                nailsageSlyPhase1Hp = ClampNailsageSlyHp(nailsageSlyPhase1HpBeforeP5);
                nailsageSlyPhase2Hp = ClampNailsageSlyHp(nailsageSlyPhase2HpBeforeP5);
            }

            nailsageSlyP5Hp = false;
            nailsageSlyHasStoredStateBeforeP5 = false;
        }

        ReapplyLiveSettings();
    }

    private static void NormalizeP5State()
    {
        if (!nailsageSlyP5Hp)
        {
            nailsageSlyPhase1Hp = ClampNailsageSlyHp(nailsageSlyPhase1Hp);
            nailsageSlyPhase2Hp = ClampNailsageSlyHp(nailsageSlyPhase2Hp);
            return;
        }

        if (!nailsageSlyHasStoredStateBeforeP5)
        {
            nailsageSlyPhase1HpBeforeP5 = ClampNailsageSlyHp(nailsageSlyPhase1Hp);
            nailsageSlyPhase2HpBeforeP5 = ClampNailsageSlyHp(nailsageSlyPhase2Hp);
            nailsageSlyHasStoredStateBeforeP5 = true;
        }

        nailsageSlyPhase1Hp = P5NailsageSlyPhase1Hp;
        nailsageSlyPhase2Hp = P5NailsageSlyPhase2Hp;
    }

    internal static void ApplyNailsageSlyHealthIfPresent()
    {
        if (!moduleActive)
        {
            return;
        }

        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || !IsNailsageSly(hm) || hm.gameObject == null || !ShouldApplySettings(hm.gameObject))
            {
                continue;
            }

            int id = hm.GetInstanceID();
            bool phase2Active = phase2ActiveByInstance.TryGetValue(id, out bool active) && active;
            if (phase2Active)
            {
                ApplyPhase2Health(hm.gameObject, hm);
            }
            else
            {
                ApplyPhase1Health(hm.gameObject, hm);
            }
        }
    }

    internal static void RestoreVanillaHealthIfPresent()
    {
        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || !IsNailsageSly(hm) || hm.gameObject == null)
            {
                continue;
            }

            RestoreVanillaHealth(hm.gameObject, hm);
        }
    }

    internal static void ApplyPhase2HpSettingsIfPresent()
    {
        if (!moduleActive)
        {
            return;
        }

        foreach (PlayMakerFSM fsm in UObject.FindObjectsOfType<PlayMakerFSM>())
        {
            if (fsm == null || fsm.gameObject == null || !IsNailsageSlyPhaseControlFsm(fsm))
            {
                continue;
            }

            ApplyPhase2HpSetting(fsm);
        }
    }

    internal static void RestoreVanillaPhase2HpIfPresent()
    {
        foreach (PlayMakerFSM fsm in UObject.FindObjectsOfType<PlayMakerFSM>())
        {
            if (fsm == null || fsm.gameObject == null || !IsNailsageSlyPhaseControlFsm(fsm))
            {
                continue;
            }

            if (!ShouldApplySettings(fsm.gameObject))
            {
                continue;
            }

            RememberVanillaPhase2Hp(fsm);
            SetPhase2HpOnFsm(fsm, GetVanillaPhase2Hp(fsm));
        }
    }

    private static void OnHealthManagerAwake_NailsageSly(On.HealthManager.orig_Awake orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsNailsageSly(self))
        {
            return;
        }

        RememberVanillaHp(self);

        int id = self.GetInstanceID();
        bool looksLikePhase2Spawn = self.hp > 0 && self.hp <= DefaultNailsageSlyPhase2Hp;
        phase2ActiveByInstance[id] = looksLikePhase2Spawn;
        pendingPhase2ApplyByInstance[id] = false;

        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }

        if (looksLikePhase2Spawn)
        {
            ApplyPhase2Health(self.gameObject, self);
        }
        else
        {
            ApplyPhase1Health(self.gameObject, self);
        }
    }

    private static void OnHealthManagerStart_NailsageSly(On.HealthManager.orig_Start orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsNailsageSly(self))
        {
            return;
        }

        RememberVanillaHp(self);
        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }

        int id = self.GetInstanceID();
        bool phase2Active = phase2ActiveByInstance.TryGetValue(id, out bool active) && active;
        if (phase2Active)
        {
            ApplyPhase2Health(self.gameObject, self);
            _ = self.StartCoroutine(DeferredApply(self, applyPhase2: true));
        }
        else
        {
            ApplyPhase1Health(self.gameObject, self);
            _ = self.StartCoroutine(DeferredApply(self, applyPhase2: false));
        }
    }

    private static void OnHealthManagerUpdate_NailsageSly(On.HealthManager.orig_Update orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || self == null || self.gameObject == null || !IsNailsageSly(self) || !ShouldApplySettings(self.gameObject))
        {
            return;
        }

        int id = self.GetInstanceID();
        bool phase2Active = phase2ActiveByInstance.TryGetValue(id, out bool active) && active;
        if (phase2Active)
        {
            return;
        }

        if (self.hp <= 0)
        {
            pendingPhase2ApplyByInstance[id] = true;
            return;
        }

        if (!pendingPhase2ApplyByInstance.TryGetValue(id, out bool pending) || !pending)
        {
            return;
        }

        phase2ActiveByInstance[id] = true;
        pendingPhase2ApplyByInstance[id] = false;
        ApplyPhase2Health(self.gameObject, self);
    }

    private static void OnPlayMakerFsmOnEnable_NailsageSly(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
    {
        orig(self);

        if (!moduleActive || self == null || self.gameObject == null || !IsNailsageSlyPhaseControlFsm(self))
        {
            return;
        }

        ApplyPhase2HpSetting(self);
    }

    private static void OnPlayMakerFsmStart_NailsageSly(On.PlayMakerFSM.orig_Start orig, PlayMakerFSM self)
    {
        orig(self);

        if (!moduleActive || self == null || self.gameObject == null || !IsNailsageSlyPhaseControlFsm(self))
        {
            return;
        }

        ApplyPhase2HpSetting(self);
    }

    private static IEnumerator DeferredApply(HealthManager hm, bool applyPhase2)
    {
        yield return null;

        if (!moduleActive || hm == null || hm.gameObject == null || !IsNailsageSly(hm) || !ShouldApplySettings(hm.gameObject))
        {
            yield break;
        }

        if (applyPhase2)
        {
            ApplyPhase2Health(hm.gameObject, hm);
        }
        else
        {
            ApplyPhase1Health(hm.gameObject, hm);
        }

        yield return new WaitForSeconds(0.01f);
        if (!moduleActive || hm == null || hm.gameObject == null || !IsNailsageSly(hm) || !ShouldApplySettings(hm.gameObject))
        {
            yield break;
        }

        if (applyPhase2)
        {
            ApplyPhase2Health(hm.gameObject, hm);
        }
        else
        {
            int id = hm.GetInstanceID();
            bool phase2Active = phase2ActiveByInstance.TryGetValue(id, out bool active) && active;
            if (!phase2Active)
            {
                ApplyPhase1Health(hm.gameObject, hm);
            }
        }
    }

    private static void SceneManager_activeSceneChanged(Scene from, Scene to)
    {
        UpdateHoGEntryAllowed(from.name, to.name);
        if (!string.Equals(to.name, NailsageSlyScene, StringComparison.Ordinal))
        {
            vanillaHpByInstance.Clear();
            vanillaPhase2HpByFsm.Clear();
            phase2ActiveByInstance.Clear();
            pendingPhase2ApplyByInstance.Clear();
            return;
        }

        if (!moduleActive)
        {
            return;
        }

        ApplyNailsageSlyHealthIfPresent();
        ApplyPhase2HpSettingsIfPresent();
    }

    private static string OnBeforeSceneLoad(string newSceneName)
    {
        UpdateHoGEntryAllowed(USceneManager.GetActiveScene().name, newSceneName);
        return newSceneName;
    }

    private static bool IsNailsageSly(HealthManager hm)
    {
        if (hm == null || hm.gameObject == null)
        {
            return false;
        }

        return IsNailsageSlyObject(hm.gameObject);
    }

    private static bool IsNailsageSlyObject(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return false;
        }

        return string.Equals(gameObject.scene.name, NailsageSlyScene, StringComparison.Ordinal)
            && gameObject.name.StartsWith(NailsageSlyName, StringComparison.Ordinal);
    }

    private static bool ShouldApplySettings(GameObject? gameObject)
    {
        if (gameObject == null || !IsNailsageSlyObject(gameObject))
        {
            return false;
        }

        return hoGEntryAllowed;
    }

    private static bool IsNailsageSlyPhaseControlFsm(PlayMakerFSM fsm)
    {
        if (fsm == null || fsm.gameObject == null || !IsNailsageSlyObject(fsm.gameObject))
        {
            return false;
        }

        if (fsm.FsmVariables.GetFsmInt(AscendedHpVariableName) != null
            || fsm.FsmVariables.GetFsmInt(LegacyAscendedHpVariableName) != null)
        {
            return true;
        }

        return string.Equals(fsm.FsmName, "sly_control", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyPhase2HpSetting(PlayMakerFSM fsm)
    {
        if (fsm == null || fsm.gameObject == null || !ShouldApplySettings(fsm.gameObject))
        {
            return;
        }

        RememberVanillaPhase2Hp(fsm);
        SetPhase2HpOnFsm(fsm, ClampNailsageSlyHp(nailsageSlyPhase2Hp));
    }

    private static void RememberVanillaPhase2Hp(PlayMakerFSM fsm)
    {
        int id = fsm.GetInstanceID();
        if (vanillaPhase2HpByFsm.ContainsKey(id))
        {
            return;
        }

        int phase2Hp = DefaultNailsageSlyPhase2Hp;
        FsmInt? phase2Var = fsm.FsmVariables.GetFsmInt(AscendedHpVariableName);
        if (phase2Var != null)
        {
            phase2Hp = phase2Var.Value;
        }
        else
        {
            FsmInt? legacyPhase2Var = fsm.FsmVariables.GetFsmInt(LegacyAscendedHpVariableName);
            if (legacyPhase2Var != null)
            {
                phase2Hp = legacyPhase2Var.Value;
            }
        }

        vanillaPhase2HpByFsm[id] = ClampNailsageSlyHp(phase2Hp);
    }

    private static int GetVanillaPhase2Hp(PlayMakerFSM fsm)
    {
        int id = fsm.GetInstanceID();
        if (vanillaPhase2HpByFsm.TryGetValue(id, out int value))
        {
            return ClampNailsageSlyHp(value);
        }

        RememberVanillaPhase2Hp(fsm);
        if (vanillaPhase2HpByFsm.TryGetValue(id, out value))
        {
            return ClampNailsageSlyHp(value);
        }

        return DefaultNailsageSlyPhase2Hp;
    }

    private static void SetPhase2HpOnFsm(PlayMakerFSM fsm, int value)
    {
        int target = ClampNailsageSlyHp(value);
        FsmInt? phase2Var = fsm.FsmVariables.GetFsmInt(AscendedHpVariableName);
        if (phase2Var != null)
        {
            phase2Var.Value = target;
        }

        FsmInt? legacyPhase2Var = fsm.FsmVariables.GetFsmInt(LegacyAscendedHpVariableName);
        if (legacyPhase2Var != null)
        {
            legacyPhase2Var.Value = target;
        }
    }

    private static void UpdateHoGEntryAllowed(string currentScene, string nextScene)
    {
        if (string.Equals(nextScene, NailsageSlyScene, StringComparison.Ordinal))
        {
            if (BossManipulateEntryGuard.IsAllowedBossEntry(currentScene, nextScene))
            {
                hoGEntryAllowed = true;
            }
            else if (string.Equals(currentScene, NailsageSlyScene, StringComparison.Ordinal) && hoGEntryAllowed)
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

    private static void ApplyPhase1Health(GameObject boss, HealthManager? hm = null)
    {
        if (!ShouldApplySettings(boss))
        {
            return;
        }

        hm ??= boss.GetComponent<HealthManager>();
        if (hm == null)
        {
            return;
        }

        int targetHp = ClampNailsageSlyHp(nailsageSlyPhase1Hp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static void ApplyPhase2Health(GameObject boss, HealthManager? hm = null)
    {
        if (!ShouldApplySettings(boss))
        {
            return;
        }

        hm ??= boss.GetComponent<HealthManager>();
        if (hm == null)
        {
            return;
        }

        int targetHp = ClampNailsageSlyHp(nailsageSlyPhase2Hp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static void RestoreVanillaHealth(GameObject boss, HealthManager? hm = null)
    {
        if (!hoGEntryAllowed)
        {
            return;
        }

        if (boss == null || !IsNailsageSlyObject(boss))
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

        int targetHp = ClampNailsageSlyHp(vanillaHp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static void RememberVanillaHp(HealthManager hm)
    {
        int instanceId = hm.GetInstanceID();
        if (vanillaHpByInstance.ContainsKey(instanceId))
        {
            return;
        }

        int hp = hm.hp;

        hp = Math.Max(hp, DefaultNailsageSlyPhase1Hp);
        if (hp > 0)
        {
            vanillaHpByInstance[instanceId] = hp;
        }
    }

    private static bool TryGetVanillaHp(HealthManager hm, out int hp)
    {
        if (vanillaHpByInstance.TryGetValue(hm.GetInstanceID(), out hp) && hp > 0)
        {
            hp = Math.Max(hp, DefaultNailsageSlyPhase1Hp);
            return true;
        }

        hp = hm.hp;

        hp = Math.Max(hp, DefaultNailsageSlyPhase1Hp);
        return hp > 0;
    }

    private static int ClampNailsageSlyHp(int value)
    {
        if (value < MinNailsageSlyHp)
        {
            return MinNailsageSlyHp;
        }

        return value > MaxNailsageSlyHp ? MaxNailsageSlyHp : value;
    }
}
