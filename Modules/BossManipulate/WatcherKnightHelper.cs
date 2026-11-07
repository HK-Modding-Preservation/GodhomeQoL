using Modding;
using Satchel;
using Satchel.Futils;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed class WatcherKnightHelper : Module
{
    private const string WatcherKnightScene = "GG_Watcher_Knights";
    private const string WatcherKnightNamePrefix = "Black Knight ";
    private const int WatcherKnightCount = 6;
    private const int DefaultWatcherKnightHp = 600;
    private const int P5WatcherKnightHp = 350;
    private const int MinWatcherKnightHp = 1;
    private const int MaxWatcherKnightHp = 999999;

    [LocalSetting]
    [BoolOption]
    internal static bool watcherKnightP5Hp = false;

    [LocalSetting]
    internal static int watcherKnight1Hp = DefaultWatcherKnightHp;

    [LocalSetting]
    internal static int watcherKnight2Hp = DefaultWatcherKnightHp;

    [LocalSetting]
    internal static int watcherKnight3Hp = DefaultWatcherKnightHp;

    [LocalSetting]
    internal static int watcherKnight4Hp = DefaultWatcherKnightHp;

    [LocalSetting]
    internal static int watcherKnight5Hp = DefaultWatcherKnightHp;

    [LocalSetting]
    internal static int watcherKnight6Hp = DefaultWatcherKnightHp;

    [LocalSetting]
    internal static int watcherKnight1HpBeforeP5 = DefaultWatcherKnightHp;

    [LocalSetting]
    internal static int watcherKnight2HpBeforeP5 = DefaultWatcherKnightHp;

    [LocalSetting]
    internal static int watcherKnight3HpBeforeP5 = DefaultWatcherKnightHp;

    [LocalSetting]
    internal static int watcherKnight4HpBeforeP5 = DefaultWatcherKnightHp;

    [LocalSetting]
    internal static int watcherKnight5HpBeforeP5 = DefaultWatcherKnightHp;

    [LocalSetting]
    internal static int watcherKnight6HpBeforeP5 = DefaultWatcherKnightHp;

    [LocalSetting]
    internal static bool watcherKnightHasStoredStateBeforeP5 = false;

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
        On.HealthManager.Awake += OnHealthManagerAwake_WatcherKnight;
        On.HealthManager.Start += OnHealthManagerStart_WatcherKnight;
        USceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        ModHooks.BeforeSceneLoadHook += OnBeforeSceneLoad;
    }

    private protected override void Unload()
    {
        RestoreVanillaHealthIfPresent();
        moduleActive = false;
        On.HealthManager.Awake -= OnHealthManagerAwake_WatcherKnight;
        On.HealthManager.Start -= OnHealthManagerStart_WatcherKnight;
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

        ApplyWatcherKnightHealthIfPresent();
    }

    internal static void SetP5HpEnabled(bool value)
    {
        if (value)
        {
            if (!watcherKnightP5Hp)
            {
                watcherKnight1HpBeforeP5 = ClampWatcherKnightHp(watcherKnight1Hp);
                watcherKnight2HpBeforeP5 = ClampWatcherKnightHp(watcherKnight2Hp);
                watcherKnight3HpBeforeP5 = ClampWatcherKnightHp(watcherKnight3Hp);
                watcherKnight4HpBeforeP5 = ClampWatcherKnightHp(watcherKnight4Hp);
                watcherKnight5HpBeforeP5 = ClampWatcherKnightHp(watcherKnight5Hp);
                watcherKnight6HpBeforeP5 = ClampWatcherKnightHp(watcherKnight6Hp);
                watcherKnightHasStoredStateBeforeP5 = true;
            }

            watcherKnightP5Hp = true;
            watcherKnight1Hp = P5WatcherKnightHp;
            watcherKnight2Hp = P5WatcherKnightHp;
            watcherKnight3Hp = P5WatcherKnightHp;
            watcherKnight4Hp = P5WatcherKnightHp;
            watcherKnight5Hp = P5WatcherKnightHp;
            watcherKnight6Hp = P5WatcherKnightHp;
        }
        else
        {
            if (watcherKnightP5Hp && watcherKnightHasStoredStateBeforeP5)
            {
                watcherKnight1Hp = ClampWatcherKnightHp(watcherKnight1HpBeforeP5);
                watcherKnight2Hp = ClampWatcherKnightHp(watcherKnight2HpBeforeP5);
                watcherKnight3Hp = ClampWatcherKnightHp(watcherKnight3HpBeforeP5);
                watcherKnight4Hp = ClampWatcherKnightHp(watcherKnight4HpBeforeP5);
                watcherKnight5Hp = ClampWatcherKnightHp(watcherKnight5HpBeforeP5);
                watcherKnight6Hp = ClampWatcherKnightHp(watcherKnight6HpBeforeP5);
            }

            watcherKnightP5Hp = false;
            watcherKnightHasStoredStateBeforeP5 = false;
        }

        ReapplyLiveSettings();
    }

    private static void NormalizeP5State()
    {
        if (!watcherKnightP5Hp)
        {
            watcherKnight1Hp = ClampWatcherKnightHp(watcherKnight1Hp);
            watcherKnight2Hp = ClampWatcherKnightHp(watcherKnight2Hp);
            watcherKnight3Hp = ClampWatcherKnightHp(watcherKnight3Hp);
            watcherKnight4Hp = ClampWatcherKnightHp(watcherKnight4Hp);
            watcherKnight5Hp = ClampWatcherKnightHp(watcherKnight5Hp);
            watcherKnight6Hp = ClampWatcherKnightHp(watcherKnight6Hp);
            return;
        }

        if (!watcherKnightHasStoredStateBeforeP5)
        {
            watcherKnight1HpBeforeP5 = ClampWatcherKnightHp(watcherKnight1Hp);
            watcherKnight2HpBeforeP5 = ClampWatcherKnightHp(watcherKnight2Hp);
            watcherKnight3HpBeforeP5 = ClampWatcherKnightHp(watcherKnight3Hp);
            watcherKnight4HpBeforeP5 = ClampWatcherKnightHp(watcherKnight4Hp);
            watcherKnight5HpBeforeP5 = ClampWatcherKnightHp(watcherKnight5Hp);
            watcherKnight6HpBeforeP5 = ClampWatcherKnightHp(watcherKnight6Hp);
            watcherKnightHasStoredStateBeforeP5 = true;
        }

        watcherKnight1Hp = P5WatcherKnightHp;
        watcherKnight2Hp = P5WatcherKnightHp;
        watcherKnight3Hp = P5WatcherKnightHp;
        watcherKnight4Hp = P5WatcherKnightHp;
        watcherKnight5Hp = P5WatcherKnightHp;
        watcherKnight6Hp = P5WatcherKnightHp;
    }

    internal static void ApplyWatcherKnightHealthIfPresent()
    {
        if (!moduleActive)
        {
            return;
        }

        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsWatcherKnight(hm) || !ShouldApplySettings(hm.gameObject))
            {
                continue;
            }

            ApplyWatcherKnightHealth(hm.gameObject, hm);
        }
    }

    internal static void RestoreVanillaHealthIfPresent()
    {
        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsWatcherKnight(hm))
            {
                continue;
            }

            RestoreVanillaHealth(hm.gameObject, hm);
        }
    }

    private static void OnHealthManagerAwake_WatcherKnight(On.HealthManager.orig_Awake orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsWatcherKnight(self))
        {
            return;
        }

        RememberVanillaHp(self);
        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }

        ApplyWatcherKnightHealth(self.gameObject, self);
    }

    private static void OnHealthManagerStart_WatcherKnight(On.HealthManager.orig_Start orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsWatcherKnight(self))
        {
            return;
        }

        RememberVanillaHp(self);
        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }

        ApplyWatcherKnightHealth(self.gameObject, self);
    }

    private static void SceneManager_activeSceneChanged(Scene from, Scene to)
    {
        UpdateHoGEntryAllowed(from.name, to.name);
        if (!string.Equals(to.name, WatcherKnightScene, StringComparison.Ordinal))
        {
            vanillaHpByInstance.Clear();
            return;
        }

        if (!moduleActive)
        {
            return;
        }

        ApplyWatcherKnightHealthIfPresent();
    }

    private static string OnBeforeSceneLoad(string newSceneName)
    {
        UpdateHoGEntryAllowed(USceneManager.GetActiveScene().name, newSceneName);
        return newSceneName;
    }

    private static bool IsWatcherKnight(HealthManager hm)
    {
        if (hm == null || hm.gameObject == null)
        {
            return false;
        }

        return IsWatcherKnightObject(hm.gameObject);
    }

    private static bool IsWatcherKnightObject(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return false;
        }

        return string.Equals(gameObject.scene.name, WatcherKnightScene, StringComparison.Ordinal)
            && TryGetKnightIndex(gameObject, out _);
    }

    private static bool TryGetKnightIndex(GameObject gameObject, out int index)
    {
        index = 0;
        if (gameObject == null)
        {
            return false;
        }

        string name = gameObject.name;
        if (string.IsNullOrEmpty(name) || !name.StartsWith(WatcherKnightNamePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        int cursor = WatcherKnightNamePrefix.Length;
        int value = 0;
        bool hasDigit = false;
        while (cursor < name.Length)
        {
            char c = name[cursor];
            if (!char.IsDigit(c))
            {
                break;
            }

            hasDigit = true;
            value = (value * 10) + (c - '0');
            cursor++;
        }

        if (!hasDigit || value < 1 || value > WatcherKnightCount)
        {
            return false;
        }

        index = value;
        return true;
    }

    private static bool ShouldApplySettings(GameObject? gameObject)
    {
        if (gameObject == null || !IsWatcherKnightObject(gameObject))
        {
            return false;
        }

        return hoGEntryAllowed;
    }

    private static void UpdateHoGEntryAllowed(string currentScene, string nextScene)
    {
        if (string.Equals(nextScene, WatcherKnightScene, StringComparison.Ordinal))
        {
            if (BossManipulateEntryGuard.IsAllowedBossEntry(currentScene, nextScene))
            {
                hoGEntryAllowed = true;
            }
            else if (string.Equals(currentScene, WatcherKnightScene, StringComparison.Ordinal) && hoGEntryAllowed)
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

    private static void ApplyWatcherKnightHealth(GameObject boss, HealthManager? hm = null)
    {
        if (!ShouldApplySettings(boss))
        {
            return;
        }

        if (!TryGetKnightIndex(boss, out int index))
        {
            return;
        }

        hm ??= boss.GetComponent<HealthManager>();
        if (hm == null)
        {
            return;
        }

        int targetHp = GetTargetHpByIndex(index);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static int GetTargetHpByIndex(int index)
    {
        return index switch
        {
            1 => ClampWatcherKnightHp(watcherKnight1Hp),
            2 => ClampWatcherKnightHp(watcherKnight2Hp),
            3 => ClampWatcherKnightHp(watcherKnight3Hp),
            4 => ClampWatcherKnightHp(watcherKnight4Hp),
            5 => ClampWatcherKnightHp(watcherKnight5Hp),
            6 => ClampWatcherKnightHp(watcherKnight6Hp),
            _ => DefaultWatcherKnightHp,
        };
    }

    private static void RestoreVanillaHealth(GameObject boss, HealthManager? hm = null)
    {
        if (!hoGEntryAllowed)
        {
            return;
        }

        if (boss == null || !IsWatcherKnightObject(boss))
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

        int targetHp = ClampWatcherKnightHp(vanillaHp);
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

    private static int ClampWatcherKnightHp(int value)
    {
        if (value < MinWatcherKnightHp)
        {
            return MinWatcherKnightHp;
        }

        return value > MaxWatcherKnightHp ? MaxWatcherKnightHp : value;
    }
}
