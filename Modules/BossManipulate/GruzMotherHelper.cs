using Modding;
using Satchel;
using Satchel.Futils;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed class GruzMotherHelper : Module
{
    private const string GruzScene = "GG_Gruz_Mother_V";
    private const string GruzName = "Giant Fly";
    private const int DefaultGruzMaxHp = 945;
    private const int DefaultGruzVanillaHp = 945;
    private const int MinGruzHp = 1;
    private const int MaxGruzHp = 999999;

    [LocalSetting]
    [BoolOption]
    internal static bool gruzUseMaxHp = false;

    [LocalSetting]
    [BoolOption]
    internal static bool gruzP5Hp = false;

    [LocalSetting]
    internal static int gruzMaxHp = DefaultGruzMaxHp;

    [LocalSetting]
    internal static int gruzMaxHpBeforeP5 = DefaultGruzMaxHp;

    [LocalSetting]
    internal static bool gruzHasStoredMaxHpBeforeP5 = false;

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
        On.HealthManager.Awake += OnHealthManagerAwake;
        On.HealthManager.Start += OnHealthManagerStart;
        USceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        ModHooks.BeforeSceneLoadHook += OnBeforeSceneLoad;
    }

    private protected override void Unload()
    {
        RestoreVanillaHealthIfPresent();
        moduleActive = false;
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

        if (ShouldUseCustomHp())
        {
            ApplyGruzHealthIfPresent();
        }
        else
        {
            RestoreVanillaHealthIfPresent();
        }
    }

    internal static void SetP5HpEnabled(bool value)
    {
        if (value)
        {
            if (!gruzP5Hp)
            {
                gruzMaxHpBeforeP5 = ClampGruzHp(gruzMaxHp);
                gruzHasStoredMaxHpBeforeP5 = true;
            }

            gruzP5Hp = true;
            gruzUseMaxHp = true;
            gruzMaxHp = 650;
        }
        else
        {
            if (gruzP5Hp && gruzHasStoredMaxHpBeforeP5)
            {
                gruzMaxHp = ClampGruzHp(gruzMaxHpBeforeP5);
            }

            gruzP5Hp = false;
            gruzHasStoredMaxHpBeforeP5 = false;
        }

        ReapplyLiveSettings();
    }

    private static void NormalizeP5State()
    {
        if (!gruzP5Hp)
        {
            return;
        }

        if (!gruzHasStoredMaxHpBeforeP5)
        {
            gruzMaxHpBeforeP5 = ClampGruzHp(gruzMaxHp);
            gruzHasStoredMaxHpBeforeP5 = true;
        }

        gruzUseMaxHp = true;
        gruzMaxHp = 650;
    }

    internal static void ApplyGruzHealthIfPresent()
    {
        if (!moduleActive || !ShouldUseCustomHp())
        {
            return;
        }

        if (!TryFindGruzHealthManager(out HealthManager? hm))
        {
            return;
        }

        if (hm != null && hm.gameObject != null)
        {
            ApplyGruzHealth(hm.gameObject, hm);
        }
    }

    internal static void RestoreVanillaHealthIfPresent()
    {
        if (!TryFindGruzHealthManager(out HealthManager? hm))
        {
            return;
        }

        if (hm != null && hm.gameObject != null)
        {
            RestoreVanillaHealth(hm.gameObject, hm);
        }
    }

    private static void OnHealthManagerAwake(On.HealthManager.orig_Awake orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsGruz(self))
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
            ApplyGruzHealth(self.gameObject, self);
        }
        else
        {
            RestoreVanillaHealth(self.gameObject, self);
        }
    }

    private static void OnHealthManagerStart(On.HealthManager.orig_Start orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsGruz(self))
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
            ApplyGruzHealth(self.gameObject, self);
            _ = self.StartCoroutine(DeferredApply(self));
        }
        else
        {
            RestoreVanillaHealth(self.gameObject, self);
        }
    }

    private static IEnumerator DeferredApply(HealthManager hm)
    {
        yield return null;

        if (!moduleActive || hm == null || hm.gameObject == null || !IsGruz(hm))
        {
            yield break;
        }

        if (ShouldUseCustomHp() && ShouldApplySettings(hm.gameObject))
        {
            ApplyGruzHealth(hm.gameObject, hm);
            yield return new WaitForSeconds(0.01f);
            if (moduleActive && hm != null && hm.gameObject != null && IsGruz(hm) && ShouldUseCustomHp() && ShouldApplySettings(hm.gameObject))
            {
                ApplyGruzHealth(hm.gameObject, hm);
            }
        }
    }

    private static void SceneManager_activeSceneChanged(Scene from, Scene to)
    {
        UpdateHoGEntryAllowed(from.name, to.name);
        if (!string.Equals(to.name, GruzScene, StringComparison.Ordinal))
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
            ApplyGruzHealthIfPresent();
        }
        else
        {
            RestoreVanillaHealthIfPresent();
        }
    }

    private static string OnBeforeSceneLoad(string newSceneName)
    {
        UpdateHoGEntryAllowed(USceneManager.GetActiveScene().name, newSceneName);
        return newSceneName;
    }

    private static bool IsGruz(HealthManager hm)
    {
        if (hm == null || hm.gameObject == null)
        {
            return false;
        }

        return IsGruzObject(hm.gameObject);
    }

    private static bool IsGruzObject(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return false;
        }

        return string.Equals(gameObject.scene.name, GruzScene, StringComparison.Ordinal)
            && gameObject.name.StartsWith(GruzName, StringComparison.Ordinal);
    }

    private static bool ShouldApplySettings(GameObject? gameObject)
    {
        if (gameObject == null || !IsGruzObject(gameObject))
        {
            return false;
        }

        return hoGEntryAllowed;
    }

    private static bool ShouldUseCustomHp() => gruzUseMaxHp;

    private static void UpdateHoGEntryAllowed(string currentScene, string nextScene)
    {
        if (string.Equals(nextScene, GruzScene, StringComparison.Ordinal))
        {
            if (BossManipulateEntryGuard.IsAllowedBossEntry(currentScene, nextScene))
            {
                hoGEntryAllowed = true;
            }
            else if (string.Equals(currentScene, GruzScene, StringComparison.Ordinal) && hoGEntryAllowed)
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

    private static void ApplyGruzHealth(GameObject gruz, HealthManager? hm = null)
    {
        if (!ShouldApplySettings(gruz) || !ShouldUseCustomHp())
        {
            return;
        }

        hm ??= gruz.GetComponent<HealthManager>();
        if (hm == null)
        {
            return;
        }

        RememberVanillaHp(hm);
        int targetHp = ClampGruzHp(gruzMaxHp);
        gruz.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static void RestoreVanillaHealth(GameObject gruz, HealthManager? hm = null)
    {
        if (!hoGEntryAllowed)
        {
            return;
        }

        if (gruz == null || !IsGruzObject(gruz))
        {
            return;
        }

        hm ??= gruz.GetComponent<HealthManager>();
        if (hm == null)
        {
            return;
        }

        if (!TryGetVanillaHp(hm, out int vanillaHp))
        {
            return;
        }

        int targetHp = ClampGruzHp(vanillaHp);
        gruz.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static bool TryFindGruzHealthManager(out HealthManager? hm)
    {
        hm = null;
        foreach (HealthManager candidate in UObject.FindObjectsOfType<HealthManager>())
        {
            if (candidate != null && IsGruz(candidate))
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

        hp = Math.Max(hp, DefaultGruzVanillaHp);
        if (hp > 0)
        {
            vanillaHpByInstance[instanceId] = hp;
        }
    }

    private static bool TryGetVanillaHp(HealthManager hm, out int hp)
    {
        if (vanillaHpByInstance.TryGetValue(hm.GetInstanceID(), out hp) && hp > 0)
        {
            hp = Math.Max(hp, DefaultGruzVanillaHp);
            return true;
        }

        hp = hm.hp;

        hp = Math.Max(hp, DefaultGruzVanillaHp);
        return hp > 0;
    }

    private static int ClampGruzHp(int value)
    {
        if (value < MinGruzHp)
        {
            return MinGruzHp;
        }

        return value > MaxGruzHp ? MaxGruzHp : value;
    }
}
