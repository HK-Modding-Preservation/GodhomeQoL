using Modding;
using Satchel;
using Satchel.Futils;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed class GodTamerHelper : Module
{
    private const string GodTamerScene = "GG_God_Tamer";
    private const string LobsterName = "Lobster";
    private const string LancerName = "Lancer";
    private const int DefaultLobsterHp = 1000;
    private const int DefaultLancerHp = 1000;
    private const int P5LobsterHp = 750;
    private const int P5LancerHp = 750;
    private const int MinGodTamerHp = 1;
    private const int MaxGodTamerHp = 999999;

    [LocalSetting]
    [BoolOption]
    internal static bool godTamerP5Hp = false;

    [LocalSetting]
    internal static int godTamerLobsterHp = DefaultLobsterHp;

    [LocalSetting]
    internal static int godTamerLancerHp = DefaultLancerHp;

    [LocalSetting]
    internal static int godTamerLobsterHpBeforeP5 = DefaultLobsterHp;

    [LocalSetting]
    internal static int godTamerLancerHpBeforeP5 = DefaultLancerHp;

    [LocalSetting]
    internal static bool godTamerHasStoredStateBeforeP5 = false;

    private enum GodTamerTarget
    {
        Unknown = 0,
        Lobster = 1,
        Lancer = 2,
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
        On.HealthManager.Awake += OnHealthManagerAwake_GodTamer;
        On.HealthManager.Start += OnHealthManagerStart_GodTamer;
        USceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        ModHooks.BeforeSceneLoadHook += OnBeforeSceneLoad;
    }

    private protected override void Unload()
    {
        RestoreVanillaHealthIfPresent();
        moduleActive = false;
        On.HealthManager.Awake -= OnHealthManagerAwake_GodTamer;
        On.HealthManager.Start -= OnHealthManagerStart_GodTamer;
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

        ApplyGodTamerHealthIfPresent();
    }

    internal static void SetP5HpEnabled(bool value)
    {
        if (value)
        {
            if (!godTamerP5Hp)
            {
                godTamerLobsterHpBeforeP5 = ClampGodTamerHp(godTamerLobsterHp);
                godTamerLancerHpBeforeP5 = ClampGodTamerHp(godTamerLancerHp);
                godTamerHasStoredStateBeforeP5 = true;
            }

            godTamerP5Hp = true;
            godTamerLobsterHp = P5LobsterHp;
            godTamerLancerHp = P5LancerHp;
        }
        else
        {
            if (godTamerP5Hp && godTamerHasStoredStateBeforeP5)
            {
                godTamerLobsterHp = ClampGodTamerHp(godTamerLobsterHpBeforeP5);
                godTamerLancerHp = ClampGodTamerHp(godTamerLancerHpBeforeP5);
            }

            godTamerP5Hp = false;
            godTamerHasStoredStateBeforeP5 = false;
        }

        ReapplyLiveSettings();
    }

    private static void NormalizeP5State()
    {
        if (!godTamerP5Hp)
        {
            godTamerLobsterHp = ClampGodTamerHp(godTamerLobsterHp);
            godTamerLancerHp = ClampGodTamerHp(godTamerLancerHp);
            return;
        }

        if (!godTamerHasStoredStateBeforeP5)
        {
            godTamerLobsterHpBeforeP5 = ClampGodTamerHp(godTamerLobsterHp);
            godTamerLancerHpBeforeP5 = ClampGodTamerHp(godTamerLancerHp);
            godTamerHasStoredStateBeforeP5 = true;
        }

        godTamerLobsterHp = P5LobsterHp;
        godTamerLancerHp = P5LancerHp;
    }

    internal static void ApplyGodTamerHealthIfPresent()
    {
        if (!moduleActive)
        {
            return;
        }

        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsGodTamer(hm) || !ShouldApplySettings(hm.gameObject))
            {
                continue;
            }

            ApplyGodTamerHealth(hm.gameObject, hm);
        }
    }

    internal static void RestoreVanillaHealthIfPresent()
    {
        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsGodTamer(hm))
            {
                continue;
            }

            RestoreVanillaHealth(hm.gameObject, hm);
        }
    }

    private static void OnHealthManagerAwake_GodTamer(On.HealthManager.orig_Awake orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsGodTamer(self))
        {
            return;
        }

        RememberVanillaHp(self);
        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }

        ApplyGodTamerHealth(self.gameObject, self);
    }

    private static void OnHealthManagerStart_GodTamer(On.HealthManager.orig_Start orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsGodTamer(self))
        {
            return;
        }

        RememberVanillaHp(self);
        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }

        ApplyGodTamerHealth(self.gameObject, self);
    }

    private static void SceneManager_activeSceneChanged(Scene from, Scene to)
    {
        UpdateHoGEntryAllowed(from.name, to.name);
        if (!string.Equals(to.name, GodTamerScene, StringComparison.Ordinal))
        {
            vanillaHpByInstance.Clear();
            return;
        }

        if (!moduleActive)
        {
            return;
        }

        ApplyGodTamerHealthIfPresent();
    }

    private static string OnBeforeSceneLoad(string newSceneName)
    {
        UpdateHoGEntryAllowed(USceneManager.GetActiveScene().name, newSceneName);
        return newSceneName;
    }

    private static bool IsGodTamer(HealthManager hm)
    {
        if (hm == null || hm.gameObject == null)
        {
            return false;
        }

        return IsGodTamerObject(hm.gameObject);
    }

    private static bool IsGodTamerObject(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return false;
        }

        if (!string.Equals(gameObject.scene.name, GodTamerScene, StringComparison.Ordinal))
        {
            return false;
        }

        return GetGodTamerTarget(gameObject) != GodTamerTarget.Unknown;
    }

    private static GodTamerTarget GetGodTamerTarget(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return GodTamerTarget.Unknown;
        }

        string name = gameObject.name;
        if (name.StartsWith(LobsterName, StringComparison.Ordinal))
        {
            return GodTamerTarget.Lobster;
        }

        if (name.StartsWith(LancerName, StringComparison.Ordinal))
        {
            return GodTamerTarget.Lancer;
        }

        return GodTamerTarget.Unknown;
    }

    private static bool ShouldApplySettings(GameObject? gameObject)
    {
        if (gameObject == null || !IsGodTamerObject(gameObject))
        {
            return false;
        }

        return hoGEntryAllowed;
    }

    private static void UpdateHoGEntryAllowed(string currentScene, string nextScene)
    {
        if (string.Equals(nextScene, GodTamerScene, StringComparison.Ordinal))
        {
            if (BossManipulateEntryGuard.IsAllowedBossEntry(currentScene, nextScene))
            {
                hoGEntryAllowed = true;
            }
            else if (string.Equals(currentScene, GodTamerScene, StringComparison.Ordinal) && hoGEntryAllowed)
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

    private static void ApplyGodTamerHealth(GameObject target, HealthManager? hm = null)
    {
        if (!ShouldApplySettings(target))
        {
            return;
        }

        GodTamerTarget bossTarget = GetGodTamerTarget(target);
        if (bossTarget == GodTamerTarget.Unknown)
        {
            return;
        }

        hm ??= target.GetComponent<HealthManager>();
        if (hm == null)
        {
            return;
        }

        int configuredHp = GetConfiguredHp(bossTarget);
        if (configuredHp <= 0)
        {
            return;
        }

        int targetHp = ClampGodTamerHp(configuredHp);
        target.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static int GetConfiguredHp(GodTamerTarget target)
    {
        return target switch
        {
            GodTamerTarget.Lobster => godTamerLobsterHp,
            GodTamerTarget.Lancer => godTamerLancerHp,
            _ => 0,
        };
    }

    private static void RestoreVanillaHealth(GameObject target, HealthManager? hm = null)
    {
        if (!hoGEntryAllowed)
        {
            return;
        }

        if (target == null || !IsGodTamerObject(target))
        {
            return;
        }

        hm ??= target.GetComponent<HealthManager>();
        if (hm == null)
        {
            return;
        }

        if (!TryGetVanillaHp(hm, out int vanillaHp))
        {
            return;
        }

        int targetHp = ClampGodTamerHp(vanillaHp);
        target.manageHealth(targetHp);
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

    private static int ClampGodTamerHp(int value)
    {
        if (value < MinGodTamerHp)
        {
            return MinGodTamerHp;
        }

        return value > MaxGodTamerHp ? MaxGodTamerHp : value;
    }
}
