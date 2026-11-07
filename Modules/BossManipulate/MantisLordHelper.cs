using Modding;
using Satchel;
using Satchel.Futils;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed class MantisLordHelper : Module
{
    private const string MantisLordScene = "GG_Mantis_Lords";
    private const string Phase1Name = "Mantis Lord";
    private const string Phase2S1Name = "Mantis Lord S1";
    private const string Phase2S2Name = "Mantis Lord S2";
    private const int DefaultPhase1Hp = 500;
    private const int DefaultPhase2S1Hp = 600;
    private const int DefaultPhase2S2Hp = 600;
    private const int MinHp = 1;
    private const int MaxHp = 999999;

    [LocalSetting]
    internal static int mantisLordPhase1Hp = DefaultPhase1Hp;

    [LocalSetting]
    internal static int mantisLordPhase2S1Hp = DefaultPhase2S1Hp;

    [LocalSetting]
    internal static int mantisLordPhase2S2Hp = DefaultPhase2S2Hp;

    private enum MantisLordTarget
    {
        Unknown = 0,
        Phase1 = 1,
        Phase2S1 = 2,
        Phase2S2 = 3,
    }

    private static readonly Dictionary<int, int> vanillaHpByInstance = new();
    private static bool moduleActive;
    private static bool hoGEntryAllowed;

    public override ToggleableLevel ToggleableLevel => ToggleableLevel.ChangeScene;

    private protected override void Load()
    {
        moduleActive = true;
        BossManipulateEntryGuard.EnsureHooks();
        NormalizeState();
        vanillaHpByInstance.Clear();
        On.HealthManager.OnEnable += OnHealthManagerOnEnable;
        On.HealthManager.Awake += OnHealthManagerAwake;
        On.HealthManager.Start += OnHealthManagerStart;
        USceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        ModHooks.BeforeSceneLoadHook += OnBeforeSceneLoad;
    }

    private protected override void Unload()
    {
        RestoreVanillaHealthIfPresent();
        moduleActive = false;
        On.HealthManager.OnEnable -= OnHealthManagerOnEnable;
        On.HealthManager.Awake -= OnHealthManagerAwake;
        On.HealthManager.Start -= OnHealthManagerStart;
        USceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
        ModHooks.BeforeSceneLoadHook -= OnBeforeSceneLoad;
        vanillaHpByInstance.Clear();
        hoGEntryAllowed = false;
    }

    internal static void ReapplyLiveSettings()
    {
        if (!moduleActive)
        {
            return;
        }

        ApplyMantisLordHealthIfPresent();
    }

    private static void NormalizeState()
    {
        mantisLordPhase1Hp = ClampHp(mantisLordPhase1Hp);
        mantisLordPhase2S1Hp = ClampHp(mantisLordPhase2S1Hp);
        mantisLordPhase2S2Hp = ClampHp(mantisLordPhase2S2Hp);
    }

    internal static void ApplyMantisLordHealthIfPresent()
    {
        if (!moduleActive)
        {
            return;
        }

        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsMantisLord(hm) || !ShouldApplySettings(hm.gameObject))
            {
                continue;
            }

            ApplyMantisLordHealth(hm.gameObject, hm);
        }
    }

    internal static void RestoreVanillaHealthIfPresent()
    {
        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsMantisLord(hm))
            {
                continue;
            }

            RestoreVanillaHealth(hm.gameObject, hm);
        }
    }

    private static void OnHealthManagerOnEnable(On.HealthManager.orig_OnEnable orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsMantisLord(self))
        {
            return;
        }

        RememberVanillaHp(self);
        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }

        ApplyMantisLordHealth(self.gameObject, self);
    }

    private static void OnHealthManagerAwake(On.HealthManager.orig_Awake orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsMantisLord(self))
        {
            return;
        }

        RememberVanillaHp(self);
        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }

        ApplyMantisLordHealth(self.gameObject, self);
    }

    private static void OnHealthManagerStart(On.HealthManager.orig_Start orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsMantisLord(self))
        {
            return;
        }

        RememberVanillaHp(self);
        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }

        ApplyMantisLordHealth(self.gameObject, self);
    }

    private static void SceneManager_activeSceneChanged(Scene from, Scene to)
    {
        UpdateHoGEntryAllowed(from.name, to.name);
        if (!string.Equals(to.name, MantisLordScene, StringComparison.Ordinal))
        {
            vanillaHpByInstance.Clear();
            return;
        }

        if (!moduleActive)
        {
            return;
        }

        ApplyMantisLordHealthIfPresent();
    }

    private static string OnBeforeSceneLoad(string newSceneName)
    {
        UpdateHoGEntryAllowed(USceneManager.GetActiveScene().name, newSceneName);
        return newSceneName;
    }

    private static bool IsMantisLord(HealthManager hm)
    {
        if (hm == null || hm.gameObject == null)
        {
            return false;
        }

        return GetMantisLordTarget(hm.gameObject) != MantisLordTarget.Unknown;
    }

    private static bool ShouldApplySettings(GameObject? gameObject)
    {
        if (gameObject == null || GetMantisLordTarget(gameObject) == MantisLordTarget.Unknown)
        {
            return false;
        }

        return hoGEntryAllowed;
    }

    private static void UpdateHoGEntryAllowed(string currentScene, string nextScene)
    {
        if (string.Equals(nextScene, MantisLordScene, StringComparison.Ordinal))
        {
            if (BossManipulateEntryGuard.IsAllowedBossEntry(currentScene, nextScene))
            {
                hoGEntryAllowed = true;
            }
            else if (string.Equals(currentScene, MantisLordScene, StringComparison.Ordinal) && hoGEntryAllowed)
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

    private static MantisLordTarget GetMantisLordTarget(GameObject gameObject)
    {
        if (gameObject == null || !string.Equals(gameObject.scene.name, MantisLordScene, StringComparison.Ordinal))
        {
            return MantisLordTarget.Unknown;
        }

        string name = gameObject.name;
        if (name.StartsWith(Phase2S1Name, StringComparison.Ordinal))
        {
            return MantisLordTarget.Phase2S1;
        }

        if (name.StartsWith(Phase2S2Name, StringComparison.Ordinal))
        {
            return MantisLordTarget.Phase2S2;
        }

        if (name.StartsWith(Phase1Name, StringComparison.Ordinal))
        {
            return MantisLordTarget.Phase1;
        }

        return MantisLordTarget.Unknown;
    }

    private static int GetConfiguredHp(MantisLordTarget target)
    {
        return target switch
        {
            MantisLordTarget.Phase1 => mantisLordPhase1Hp,
            MantisLordTarget.Phase2S1 => mantisLordPhase2S1Hp,
            MantisLordTarget.Phase2S2 => mantisLordPhase2S2Hp,
            _ => 0,
        };
    }

    private static void ApplyMantisLordHealth(GameObject boss, HealthManager? hm = null)
    {
        if (!ShouldApplySettings(boss))
        {
            return;
        }

        MantisLordTarget target = GetMantisLordTarget(boss);
        if (target == MantisLordTarget.Unknown)
        {
            return;
        }

        hm ??= boss.GetComponent<HealthManager>();
        if (hm == null)
        {
            return;
        }

        int configuredHp = GetConfiguredHp(target);
        if (configuredHp <= 0)
        {
            return;
        }

        int targetHp = ClampHp(configuredHp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static void RestoreVanillaHealth(GameObject boss, HealthManager? hm = null)
    {
        if (!hoGEntryAllowed)
        {
            return;
        }

        if (boss == null || GetMantisLordTarget(boss) == MantisLordTarget.Unknown)
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

        if (hp > 0)
        {
            vanillaHpByInstance[instanceId] = hp;
        }
    }

    private static bool TryGetVanillaHp(HealthManager hm, out int hp)
    {
        if (vanillaHpByInstance.TryGetValue(hm.GetInstanceID(), out hp) && hp > 0)
        {
            return true;
        }

        hp = hm.hp;

        return hp > 0;
    }

    private static int ClampHp(int value)
    {
        return Mathf.Clamp(value, MinHp, MaxHp);
    }
}
