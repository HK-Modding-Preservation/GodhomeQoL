using Modding;
using Satchel;
using Satchel.Futils;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed class SisterOfBattleHelper : Module
{
    private const string SisterOfBattleScene = "GG_Mantis_Lords_V";
    private const string Phase1Name = "Mantis Lord";
    private const string Phase2S1Name = "Mantis Lord S1";
    private const string Phase2S2Name = "Mantis Lord S2";
    private const string Phase2S3Name = "Mantis Lord S3";
    private const int DefaultPhase1Hp = 600;
    private const int DefaultPhase2S1Hp = 950;
    private const int DefaultPhase2S2Hp = 950;
    private const int DefaultPhase2S3Hp = 950;
    private const int P5Phase1Hp = 500;
    private const int P5Phase2S1Hp = 750;
    private const int P5Phase2S2Hp = 750;
    private const int P5Phase2S3Hp = 750;
    private const int MinHp = 1;
    private const int MaxHp = 999999;

    [LocalSetting]
    [BoolOption]
    internal static bool sisterOfBattleP5Hp = false;

    [LocalSetting]
    internal static int sisterOfBattlePhase1Hp = DefaultPhase1Hp;

    [LocalSetting]
    internal static int sisterOfBattlePhase2S1Hp = DefaultPhase2S1Hp;

    [LocalSetting]
    internal static int sisterOfBattlePhase2S2Hp = DefaultPhase2S2Hp;

    [LocalSetting]
    internal static int sisterOfBattlePhase2S3Hp = DefaultPhase2S3Hp;

    [LocalSetting]
    internal static int sisterOfBattlePhase1HpBeforeP5 = DefaultPhase1Hp;

    [LocalSetting]
    internal static int sisterOfBattlePhase2S1HpBeforeP5 = DefaultPhase2S1Hp;

    [LocalSetting]
    internal static int sisterOfBattlePhase2S2HpBeforeP5 = DefaultPhase2S2Hp;

    [LocalSetting]
    internal static int sisterOfBattlePhase2S3HpBeforeP5 = DefaultPhase2S3Hp;

    [LocalSetting]
    internal static bool sisterOfBattleHasStoredStateBeforeP5 = false;

    private enum SisterTarget
    {
        Unknown = 0,
        Phase1 = 1,
        Phase2S1 = 2,
        Phase2S2 = 3,
        Phase2S3 = 4,
    }

    private static readonly Dictionary<int, int> vanillaHpByInstance = new();
    private static bool moduleActive;
    private static bool hoGEntryAllowed;

    public override ToggleableLevel ToggleableLevel => ToggleableLevel.ChangeScene;

    private protected override void Load()
    {
        moduleActive = true;
        BossManipulateEntryGuard.EnsureHooks();
        NormalizeP5State();
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

        ApplySisterOfBattleHealthIfPresent();
    }

    internal static void SetP5HpEnabled(bool value)
    {
        if (value)
        {
            if (!sisterOfBattleP5Hp)
            {
                sisterOfBattlePhase1HpBeforeP5 = ClampHp(sisterOfBattlePhase1Hp);
                sisterOfBattlePhase2S1HpBeforeP5 = ClampHp(sisterOfBattlePhase2S1Hp);
                sisterOfBattlePhase2S2HpBeforeP5 = ClampHp(sisterOfBattlePhase2S2Hp);
                sisterOfBattlePhase2S3HpBeforeP5 = ClampHp(sisterOfBattlePhase2S3Hp);
                sisterOfBattleHasStoredStateBeforeP5 = true;
            }

            sisterOfBattleP5Hp = true;
            sisterOfBattlePhase1Hp = P5Phase1Hp;
            sisterOfBattlePhase2S1Hp = P5Phase2S1Hp;
            sisterOfBattlePhase2S2Hp = P5Phase2S2Hp;
            sisterOfBattlePhase2S3Hp = P5Phase2S3Hp;
        }
        else
        {
            if (sisterOfBattleP5Hp && sisterOfBattleHasStoredStateBeforeP5)
            {
                sisterOfBattlePhase1Hp = ClampHp(sisterOfBattlePhase1HpBeforeP5);
                sisterOfBattlePhase2S1Hp = ClampHp(sisterOfBattlePhase2S1HpBeforeP5);
                sisterOfBattlePhase2S2Hp = ClampHp(sisterOfBattlePhase2S2HpBeforeP5);
                sisterOfBattlePhase2S3Hp = ClampHp(sisterOfBattlePhase2S3HpBeforeP5);
            }

            sisterOfBattleP5Hp = false;
            sisterOfBattleHasStoredStateBeforeP5 = false;
        }

        ReapplyLiveSettings();
    }

    private static void NormalizeP5State()
    {
        sisterOfBattlePhase1Hp = ClampHp(sisterOfBattlePhase1Hp);
        sisterOfBattlePhase2S1Hp = ClampHp(sisterOfBattlePhase2S1Hp);
        sisterOfBattlePhase2S2Hp = ClampHp(sisterOfBattlePhase2S2Hp);
        sisterOfBattlePhase2S3Hp = ClampHp(sisterOfBattlePhase2S3Hp);

        if (!sisterOfBattleP5Hp)
        {
            return;
        }

        if (!sisterOfBattleHasStoredStateBeforeP5)
        {
            sisterOfBattlePhase1HpBeforeP5 = sisterOfBattlePhase1Hp;
            sisterOfBattlePhase2S1HpBeforeP5 = sisterOfBattlePhase2S1Hp;
            sisterOfBattlePhase2S2HpBeforeP5 = sisterOfBattlePhase2S2Hp;
            sisterOfBattlePhase2S3HpBeforeP5 = sisterOfBattlePhase2S3Hp;
            sisterOfBattleHasStoredStateBeforeP5 = true;
        }

        sisterOfBattlePhase1Hp = P5Phase1Hp;
        sisterOfBattlePhase2S1Hp = P5Phase2S1Hp;
        sisterOfBattlePhase2S2Hp = P5Phase2S2Hp;
        sisterOfBattlePhase2S3Hp = P5Phase2S3Hp;
    }

    internal static void ApplySisterOfBattleHealthIfPresent()
    {
        if (!moduleActive)
        {
            return;
        }

        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsSisterOfBattle(hm) || !ShouldApplySettings(hm.gameObject))
            {
                continue;
            }

            ApplySisterOfBattleHealth(hm.gameObject, hm);
        }
    }

    internal static void RestoreVanillaHealthIfPresent()
    {
        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsSisterOfBattle(hm))
            {
                continue;
            }

            RestoreVanillaHealth(hm.gameObject, hm);
        }
    }

    private static void OnHealthManagerOnEnable(On.HealthManager.orig_OnEnable orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsSisterOfBattle(self))
        {
            return;
        }

        RememberVanillaHp(self);
        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }

        ApplySisterOfBattleHealth(self.gameObject, self);
    }

    private static void OnHealthManagerAwake(On.HealthManager.orig_Awake orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsSisterOfBattle(self))
        {
            return;
        }

        RememberVanillaHp(self);
        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }

        ApplySisterOfBattleHealth(self.gameObject, self);
    }

    private static void OnHealthManagerStart(On.HealthManager.orig_Start orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsSisterOfBattle(self))
        {
            return;
        }

        RememberVanillaHp(self);
        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }

        ApplySisterOfBattleHealth(self.gameObject, self);
    }

    private static void SceneManager_activeSceneChanged(Scene from, Scene to)
    {
        UpdateHoGEntryAllowed(from.name, to.name);
        if (!string.Equals(to.name, SisterOfBattleScene, StringComparison.Ordinal))
        {
            vanillaHpByInstance.Clear();
            return;
        }

        if (!moduleActive)
        {
            return;
        }

        ApplySisterOfBattleHealthIfPresent();
    }

    private static string OnBeforeSceneLoad(string newSceneName)
    {
        UpdateHoGEntryAllowed(USceneManager.GetActiveScene().name, newSceneName);
        return newSceneName;
    }

    private static bool IsSisterOfBattle(HealthManager hm)
    {
        if (hm == null || hm.gameObject == null)
        {
            return false;
        }

        return GetSisterTarget(hm.gameObject) != SisterTarget.Unknown;
    }

    private static bool ShouldApplySettings(GameObject? gameObject)
    {
        if (gameObject == null || GetSisterTarget(gameObject) == SisterTarget.Unknown)
        {
            return false;
        }

        return hoGEntryAllowed;
    }

    private static void UpdateHoGEntryAllowed(string currentScene, string nextScene)
    {
        if (string.Equals(nextScene, SisterOfBattleScene, StringComparison.Ordinal))
        {
            if (BossManipulateEntryGuard.IsAllowedBossEntry(currentScene, nextScene))
            {
                hoGEntryAllowed = true;
            }
            else if (string.Equals(currentScene, SisterOfBattleScene, StringComparison.Ordinal) && hoGEntryAllowed)
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

    private static SisterTarget GetSisterTarget(GameObject gameObject)
    {
        if (gameObject == null || !string.Equals(gameObject.scene.name, SisterOfBattleScene, StringComparison.Ordinal))
        {
            return SisterTarget.Unknown;
        }

        string name = gameObject.name;
        if (name.StartsWith(Phase2S1Name, StringComparison.Ordinal))
        {
            return SisterTarget.Phase2S1;
        }

        if (name.StartsWith(Phase2S2Name, StringComparison.Ordinal))
        {
            return SisterTarget.Phase2S2;
        }

        if (name.StartsWith(Phase2S3Name, StringComparison.Ordinal))
        {
            return SisterTarget.Phase2S3;
        }

        if (name.StartsWith(Phase1Name, StringComparison.Ordinal))
        {
            return SisterTarget.Phase1;
        }

        return SisterTarget.Unknown;
    }

    private static int GetConfiguredHp(SisterTarget target)
    {
        return target switch
        {
            SisterTarget.Phase1 => sisterOfBattlePhase1Hp,
            SisterTarget.Phase2S1 => sisterOfBattlePhase2S1Hp,
            SisterTarget.Phase2S2 => sisterOfBattlePhase2S2Hp,
            SisterTarget.Phase2S3 => sisterOfBattlePhase2S3Hp,
            _ => 0,
        };
    }

    private static void ApplySisterOfBattleHealth(GameObject boss, HealthManager? hm = null)
    {
        if (!ShouldApplySettings(boss))
        {
            return;
        }

        SisterTarget target = GetSisterTarget(boss);
        if (target == SisterTarget.Unknown)
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

        if (boss == null || GetSisterTarget(boss) == SisterTarget.Unknown)
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
