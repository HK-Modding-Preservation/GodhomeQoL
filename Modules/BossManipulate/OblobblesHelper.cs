using HutongGames.PlayMaker;
using Modding;
using Satchel;
using Satchel.Futils;
using System.Linq;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed class OblobblesHelper : Module
{
    private const string OblobblesScene = "GG_Oblobbles";
    private const string OblobbleName = "Mega Fat Bee";
    private const string SetRageFsmName = "Set Rage";
    private const string Phase2HpAddVariableName = "HP Add";
    private const string Phase2HpMaxVariableName = "HP Max";

    private const int DefaultLeftPhase1Hp = 750;
    private const int DefaultRightPhase1Hp = 750;
    private const int DefaultPhase2Hp = 750;
    private const int P5Phase1Hp = 450;
    private const int DefaultVanillaStartHp = 450;
    private const int DefaultVanillaPhase2HpAdd = 200;
    private const int DefaultVanillaPhase2HpMax = 750;
    private const int MinHp = 1;
    private const int MaxHp = 999999;

    [LocalSetting]
    [BoolOption]
    internal static bool oblobblesP5Hp = false;

    [LocalSetting]
    internal static int oblobblesLeftPhase1Hp = DefaultLeftPhase1Hp;

    [LocalSetting]
    internal static int oblobblesRightPhase1Hp = DefaultRightPhase1Hp;

    [LocalSetting]
    internal static bool oblobblesUsePhase2Hp = false;

    [LocalSetting]
    internal static int oblobblesPhase2Hp = DefaultPhase2Hp;

    [LocalSetting]
    internal static int oblobblesLeftPhase1HpBeforeP5 = DefaultLeftPhase1Hp;

    [LocalSetting]
    internal static int oblobblesRightPhase1HpBeforeP5 = DefaultRightPhase1Hp;

    [LocalSetting]
    internal static bool oblobblesUsePhase2HpBeforeP5 = false;

    [LocalSetting]
    internal static int oblobblesPhase2HpBeforeP5 = DefaultPhase2Hp;

    [LocalSetting]
    internal static bool oblobblesHasStoredStateBeforeP5 = false;

    private enum OblobbleSide
    {
        Unknown = 0,
        Left = 1,
        Right = 2,
    }

    private static readonly Dictionary<int, int> vanillaHpByInstance = new();
    private static readonly Dictionary<int, OblobbleSide> sideByInstance = new();
    private static readonly Dictionary<int, int> vanillaPhase2AddByFsm = new();
    private static readonly Dictionary<int, int> vanillaPhase2MaxByFsm = new();
    private static bool moduleActive;
    private static bool hoGEntryAllowed;

    public override ToggleableLevel ToggleableLevel => ToggleableLevel.ChangeScene;

    private protected override void Load()
    {
        moduleActive = true;
        BossManipulateEntryGuard.EnsureHooks();
        NormalizeP5State();
        NormalizePhase2State();
        vanillaHpByInstance.Clear();
        sideByInstance.Clear();
        vanillaPhase2AddByFsm.Clear();
        vanillaPhase2MaxByFsm.Clear();
        On.HealthManager.Awake += OnHealthManagerAwake;
        On.HealthManager.Start += OnHealthManagerStart;
        On.PlayMakerFSM.OnEnable += OnPlayMakerFsmOnEnable;
        On.PlayMakerFSM.Start += OnPlayMakerFsmStart;
        USceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        ModHooks.BeforeSceneLoadHook += OnBeforeSceneLoad;
    }

    private protected override void Unload()
    {
        RestoreVanillaHealthIfPresent();
        RestoreVanillaPhase2SettingsIfPresent();
        moduleActive = false;
        On.HealthManager.Awake -= OnHealthManagerAwake;
        On.HealthManager.Start -= OnHealthManagerStart;
        On.PlayMakerFSM.OnEnable -= OnPlayMakerFsmOnEnable;
        On.PlayMakerFSM.Start -= OnPlayMakerFsmStart;
        USceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
        ModHooks.BeforeSceneLoadHook -= OnBeforeSceneLoad;
        vanillaHpByInstance.Clear();
        sideByInstance.Clear();
        vanillaPhase2AddByFsm.Clear();
        vanillaPhase2MaxByFsm.Clear();
        hoGEntryAllowed = false;
    }

    internal static void ReapplyLiveSettings()
    {
        if (!moduleActive)
        {
            return;
        }

        ApplyOblobblesHealthIfPresent();
        ApplyPhase2SettingsIfPresent();
    }

    internal static void SetP5HpEnabled(bool value)
    {
        if (value)
        {
            if (!oblobblesP5Hp)
            {
                oblobblesLeftPhase1HpBeforeP5 = ClampHp(oblobblesLeftPhase1Hp);
                oblobblesRightPhase1HpBeforeP5 = ClampHp(oblobblesRightPhase1Hp);
                oblobblesUsePhase2HpBeforeP5 = oblobblesUsePhase2Hp;
                oblobblesPhase2HpBeforeP5 = ClampHp(oblobblesPhase2Hp);
                oblobblesHasStoredStateBeforeP5 = true;
            }

            oblobblesP5Hp = true;
            oblobblesLeftPhase1Hp = P5Phase1Hp;
            oblobblesRightPhase1Hp = P5Phase1Hp;
            oblobblesUsePhase2Hp = false;
        }
        else
        {
            if (oblobblesP5Hp && oblobblesHasStoredStateBeforeP5)
            {
                oblobblesLeftPhase1Hp = ClampHp(oblobblesLeftPhase1HpBeforeP5);
                oblobblesRightPhase1Hp = ClampHp(oblobblesRightPhase1HpBeforeP5);
                oblobblesUsePhase2Hp = oblobblesUsePhase2HpBeforeP5;
                oblobblesPhase2Hp = ClampHp(oblobblesPhase2HpBeforeP5);
            }

            oblobblesP5Hp = false;
            oblobblesHasStoredStateBeforeP5 = false;
        }

        NormalizePhase2State();
        ReapplyLiveSettings();
    }

    private static void NormalizeP5State()
    {
        oblobblesLeftPhase1Hp = ClampHp(oblobblesLeftPhase1Hp);
        oblobblesRightPhase1Hp = ClampHp(oblobblesRightPhase1Hp);
        oblobblesPhase2Hp = ClampHp(oblobblesPhase2Hp);

        if (!oblobblesP5Hp)
        {
            return;
        }

        if (!oblobblesHasStoredStateBeforeP5)
        {
            oblobblesLeftPhase1HpBeforeP5 = ClampHp(oblobblesLeftPhase1Hp);
            oblobblesRightPhase1HpBeforeP5 = ClampHp(oblobblesRightPhase1Hp);
            oblobblesUsePhase2HpBeforeP5 = oblobblesUsePhase2Hp;
            oblobblesPhase2HpBeforeP5 = ClampHp(oblobblesPhase2Hp);
            oblobblesHasStoredStateBeforeP5 = true;
        }

        oblobblesLeftPhase1Hp = P5Phase1Hp;
        oblobblesRightPhase1Hp = P5Phase1Hp;
        oblobblesUsePhase2Hp = false;
    }

    private static void NormalizePhase2State()
    {
        oblobblesPhase2Hp = ClampHp(oblobblesPhase2Hp);
    }

    internal static void ApplyOblobblesHealthIfPresent()
    {
        if (!moduleActive)
        {
            return;
        }

        RefreshSideAssignments();
        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsOblobble(hm) || !ShouldApplySettings(hm.gameObject))
            {
                continue;
            }

            ApplyOblobblePhase1Health(hm.gameObject, hm);
        }
    }

    internal static void RestoreVanillaHealthIfPresent()
    {
        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsOblobble(hm))
            {
                continue;
            }

            RestoreVanillaHealth(hm.gameObject, hm);
        }
    }

    internal static void ApplyPhase2SettingsIfPresent()
    {
        if (!moduleActive)
        {
            return;
        }

        foreach (PlayMakerFSM fsm in UObject.FindObjectsOfType<PlayMakerFSM>())
        {
            if (fsm == null || fsm.gameObject == null || !IsSetRageFsm(fsm))
            {
                continue;
            }

            ApplyPhase2Settings(fsm);
        }
    }

    internal static void RestoreVanillaPhase2SettingsIfPresent()
    {
        foreach (PlayMakerFSM fsm in UObject.FindObjectsOfType<PlayMakerFSM>())
        {
            if (fsm == null || fsm.gameObject == null || !IsSetRageFsm(fsm))
            {
                continue;
            }

            SetPhase2SettingsOnFsm(fsm, GetVanillaPhase2Add(fsm), GetVanillaPhase2Max(fsm));
        }
    }

    private static void OnHealthManagerAwake(On.HealthManager.orig_Awake orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsOblobble(self))
        {
            return;
        }

        RememberVanillaHp(self);
        RefreshSideAssignments();
        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }

        ApplyOblobblePhase1Health(self.gameObject, self);
    }

    private static void OnHealthManagerStart(On.HealthManager.orig_Start orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsOblobble(self))
        {
            return;
        }

        RememberVanillaHp(self);
        RefreshSideAssignments();
        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }

        ApplyOblobblePhase1Health(self.gameObject, self);
        _ = self.StartCoroutine(DeferredApply(self));
    }

    private static void OnPlayMakerFsmOnEnable(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
    {
        orig(self);

        if (!moduleActive || self == null || self.gameObject == null || !IsSetRageFsm(self))
        {
            return;
        }

        ApplyPhase2Settings(self);
    }

    private static void OnPlayMakerFsmStart(On.PlayMakerFSM.orig_Start orig, PlayMakerFSM self)
    {
        orig(self);

        if (!moduleActive || self == null || self.gameObject == null || !IsSetRageFsm(self))
        {
            return;
        }

        ApplyPhase2Settings(self);
    }

    private static IEnumerator DeferredApply(HealthManager hm)
    {
        yield return null;

        if (!moduleActive || hm == null || hm.gameObject == null || !IsOblobble(hm))
        {
            yield break;
        }

        if (ShouldApplySettings(hm.gameObject))
        {
            RefreshSideAssignments();
            ApplyOblobblePhase1Health(hm.gameObject, hm);
            yield return new WaitForSeconds(0.01f);
            if (moduleActive && hm != null && hm.gameObject != null && IsOblobble(hm) && ShouldApplySettings(hm.gameObject))
            {
                RefreshSideAssignments();
                ApplyOblobblePhase1Health(hm.gameObject, hm);
            }
        }

        ApplyPhase2SettingsIfPresent();
    }

    private static void SceneManager_activeSceneChanged(Scene from, Scene to)
    {
        UpdateHoGEntryAllowed(from.name, to.name);
        if (!string.Equals(to.name, OblobblesScene, StringComparison.Ordinal))
        {
            vanillaHpByInstance.Clear();
            sideByInstance.Clear();
            vanillaPhase2AddByFsm.Clear();
            vanillaPhase2MaxByFsm.Clear();
            return;
        }

        if (!moduleActive)
        {
            return;
        }

        ApplyOblobblesHealthIfPresent();
        ApplyPhase2SettingsIfPresent();
    }

    private static string OnBeforeSceneLoad(string newSceneName)
    {
        UpdateHoGEntryAllowed(USceneManager.GetActiveScene().name, newSceneName);
        return newSceneName;
    }

    private static bool IsOblobble(HealthManager hm)
    {
        if (hm == null || hm.gameObject == null)
        {
            return false;
        }

        return IsOblobbleObject(hm.gameObject);
    }

    private static bool IsOblobbleObject(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return false;
        }

        return string.Equals(gameObject.scene.name, OblobblesScene, StringComparison.Ordinal)
            && gameObject.name.StartsWith(OblobbleName, StringComparison.Ordinal);
    }

    private static bool ShouldApplySettings(GameObject? gameObject)
    {
        if (gameObject == null || !IsOblobbleObject(gameObject))
        {
            return false;
        }

        return hoGEntryAllowed;
    }

    private static bool ShouldUseCustomPhase2Hp() => oblobblesUsePhase2Hp && !oblobblesP5Hp;

    private static void UpdateHoGEntryAllowed(string currentScene, string nextScene)
    {
        if (string.Equals(nextScene, OblobblesScene, StringComparison.Ordinal))
        {
            if (BossManipulateEntryGuard.IsAllowedBossEntry(currentScene, nextScene))
            {
                hoGEntryAllowed = true;
            }
            else if (string.Equals(currentScene, OblobblesScene, StringComparison.Ordinal) && hoGEntryAllowed)
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

    private static void ApplyOblobblePhase1Health(GameObject boss, HealthManager? hm = null)
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

        RememberVanillaHp(hm);
        OblobbleSide side = ResolveSide(boss, hm);
        int configuredHp = side == OblobbleSide.Right
            ? oblobblesRightPhase1Hp
            : oblobblesLeftPhase1Hp;

        int targetHp = ClampHp(configuredHp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static void ApplyPhase2Settings(PlayMakerFSM fsm)
    {
        if (fsm == null || fsm.gameObject == null || !IsSetRageFsm(fsm))
        {
            return;
        }

        if (!ShouldApplySettings(fsm.gameObject))
        {
            return;
        }

        RememberVanillaPhase2Settings(fsm);
        if (ShouldUseCustomPhase2Hp())
        {
            int fixedHp = ClampHp(oblobblesPhase2Hp);
            SetPhase2SettingsOnFsm(fsm, fixedHp, fixedHp);
            return;
        }

        SetPhase2SettingsOnFsm(fsm, GetVanillaPhase2Add(fsm), GetVanillaPhase2Max(fsm));
    }

    private static void SetPhase2SettingsOnFsm(PlayMakerFSM fsm, int hpAdd, int hpMax)
    {
        if (fsm == null)
        {
            return;
        }

        FsmInt? hpAddVariable = fsm.FsmVariables?.FindFsmInt(Phase2HpAddVariableName);
        if (hpAddVariable != null)
        {
            hpAddVariable.Value = ClampHp(hpAdd);
        }

        FsmInt? hpMaxVariable = fsm.FsmVariables?.FindFsmInt(Phase2HpMaxVariableName);
        if (hpMaxVariable != null)
        {
            hpMaxVariable.Value = ClampHp(hpMax);
        }
    }

    private static bool IsSetRageFsm(PlayMakerFSM fsm)
    {
        if (fsm == null || fsm.gameObject == null || !IsOblobbleObject(fsm.gameObject))
        {
            return false;
        }

        if (string.Equals(fsm.FsmName, SetRageFsmName, StringComparison.Ordinal))
        {
            return true;
        }

        return fsm.FsmVariables?.FindFsmInt(Phase2HpAddVariableName) != null
            && fsm.FsmVariables?.FindFsmInt(Phase2HpMaxVariableName) != null;
    }

    private static void RememberVanillaPhase2Settings(PlayMakerFSM fsm)
    {
        int id = fsm.GetInstanceID();
        if (!vanillaPhase2AddByFsm.ContainsKey(id))
        {
            FsmInt? hpAddVariable = fsm.FsmVariables?.FindFsmInt(Phase2HpAddVariableName);
            int vanillaAdd = ClampHp(hpAddVariable?.Value ?? DefaultVanillaPhase2HpAdd);
            vanillaPhase2AddByFsm[id] = vanillaAdd;
        }

        if (!vanillaPhase2MaxByFsm.ContainsKey(id))
        {
            FsmInt? hpMaxVariable = fsm.FsmVariables?.FindFsmInt(Phase2HpMaxVariableName);
            int vanillaMax = ClampHp(hpMaxVariable?.Value ?? DefaultVanillaPhase2HpMax);
            vanillaPhase2MaxByFsm[id] = vanillaMax;
        }
    }

    private static int GetVanillaPhase2Add(PlayMakerFSM fsm)
    {
        int id = fsm.GetInstanceID();
        if (vanillaPhase2AddByFsm.TryGetValue(id, out int stored))
        {
            return ClampHp(stored);
        }

        FsmInt? hpAddVariable = fsm.FsmVariables?.FindFsmInt(Phase2HpAddVariableName);
        int vanillaAdd = ClampHp(hpAddVariable?.Value ?? DefaultVanillaPhase2HpAdd);
        vanillaPhase2AddByFsm[id] = vanillaAdd;
        return vanillaAdd;
    }

    private static int GetVanillaPhase2Max(PlayMakerFSM fsm)
    {
        int id = fsm.GetInstanceID();
        if (vanillaPhase2MaxByFsm.TryGetValue(id, out int stored))
        {
            return ClampHp(stored);
        }

        FsmInt? hpMaxVariable = fsm.FsmVariables?.FindFsmInt(Phase2HpMaxVariableName);
        int vanillaMax = ClampHp(hpMaxVariable?.Value ?? DefaultVanillaPhase2HpMax);
        vanillaPhase2MaxByFsm[id] = vanillaMax;
        return vanillaMax;
    }

    private static void RefreshSideAssignments()
    {
        List<HealthManager> oblobbles = UObject.FindObjectsOfType<HealthManager>()
            .Where(hm => hm != null && hm.gameObject != null && IsOblobble(hm))
            .OrderBy(hm => hm.transform.position.x)
            .ToList();

        if (oblobbles.Count == 0)
        {
            sideByInstance.Clear();
            return;
        }

        HashSet<int> activeIds = new(oblobbles.Select(hm => hm.GetInstanceID()));
        foreach (int id in sideByInstance.Keys.ToArray())
        {
            if (!activeIds.Contains(id))
            {
                sideByInstance.Remove(id);
            }
        }

        if (oblobbles.Count == 1)
        {
            HealthManager only = oblobbles[0];
            int id = only.GetInstanceID();
            if (!sideByInstance.ContainsKey(id))
            {
                sideByInstance[id] = only.transform.position.x <= 0f ? OblobbleSide.Left : OblobbleSide.Right;
            }

            return;
        }

        sideByInstance[oblobbles[0].GetInstanceID()] = OblobbleSide.Left;
        sideByInstance[oblobbles[^1].GetInstanceID()] = OblobbleSide.Right;
    }

    private static OblobbleSide ResolveSide(GameObject gameObject, HealthManager? hm = null)
    {
        hm ??= gameObject != null ? gameObject.GetComponent<HealthManager>() : null;
        if (hm == null)
        {
            return OblobbleSide.Unknown;
        }

        int id = hm.GetInstanceID();
        if (sideByInstance.TryGetValue(id, out OblobbleSide side) && side != OblobbleSide.Unknown)
        {
            return side;
        }

        RefreshSideAssignments();
        if (sideByInstance.TryGetValue(id, out side) && side != OblobbleSide.Unknown)
        {
            return side;
        }

        side = hm.transform.position.x <= 0f ? OblobbleSide.Left : OblobbleSide.Right;
        sideByInstance[id] = side;
        return side;
    }

    private static void RestoreVanillaHealth(GameObject boss, HealthManager? hm = null)
    {
        if (!hoGEntryAllowed)
        {
            return;
        }

        if (boss == null || !IsOblobbleObject(boss))
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

        int targetHp = ClampHp(vanillaHp);
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

        hp = Math.Max(hp, DefaultVanillaStartHp);
        if (hp > 0)
        {
            vanillaHpByInstance[instanceId] = hp;
        }
    }

    private static bool TryGetVanillaHp(HealthManager hm, out int hp)
    {
        if (vanillaHpByInstance.TryGetValue(hm.GetInstanceID(), out hp) && hp > 0)
        {
            hp = Math.Max(hp, DefaultVanillaStartHp);
            return true;
        }

        hp = hm.hp;

        hp = Math.Max(hp, DefaultVanillaStartHp);
        return hp > 0;
    }

    private static int ClampHp(int value)
    {
        if (value < MinHp)
        {
            return MinHp;
        }

        return value > MaxHp ? MaxHp : value;
    }
}
