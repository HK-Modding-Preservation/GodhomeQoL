using Modding;
using Satchel;
using Satchel.Futils;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed class OroMatoHelper : Module
{
    private const string OroMatoScene = "GG_Nailmasters";
    private const string OroName = "Oro";
    private const string MatoName = "Mato";
    private const int DefaultOroPhase1Hp = 800;
    private const int DefaultOroPhase2Hp = 1000;
    private const int DefaultMatoHp = 1000;
    private const int P5OroPhase1Hp = 500;
    private const int P5OroPhase2Hp = 600;
    private const int P5MatoHp = 1000;
    private const int MinOroMatoHp = 1;
    private const int MaxOroMatoHp = 999999;

    [LocalSetting]
    [BoolOption]
    internal static bool oroMatoP5Hp = false;

    [LocalSetting]
    internal static int oroMatoOroPhase1Hp = DefaultOroPhase1Hp;

    [LocalSetting]
    internal static int oroMatoOroPhase2Hp = DefaultOroPhase2Hp;

    [LocalSetting]
    internal static int oroMatoMatoHp = DefaultMatoHp;

    [LocalSetting]
    internal static int oroMatoOroPhase1HpBeforeP5 = DefaultOroPhase1Hp;

    [LocalSetting]
    internal static int oroMatoOroPhase2HpBeforeP5 = DefaultOroPhase2Hp;

    [LocalSetting]
    internal static int oroMatoMatoHpBeforeP5 = DefaultMatoHp;

    [LocalSetting]
    internal static bool oroMatoHasStoredStateBeforeP5 = false;

    private static readonly Dictionary<int, int> vanillaHpByInstance = new();
    private static readonly Dictionary<int, bool> oroPhase2ByInstance = new();
    private static bool moduleActive;
    private static bool hoGEntryAllowed;

    public override ToggleableLevel ToggleableLevel => ToggleableLevel.ChangeScene;

    private protected override void Load()
    {
        moduleActive = true;
        BossManipulateEntryGuard.EnsureHooks();
        NormalizeP5State();
        vanillaHpByInstance.Clear();
        oroPhase2ByInstance.Clear();
        On.HealthManager.Awake += OnHealthManagerAwake_OroMato;
        On.HealthManager.Start += OnHealthManagerStart_OroMato;
        On.HealthManager.Update += OnHealthManagerUpdate_OroMato;
        USceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        ModHooks.BeforeSceneLoadHook += OnBeforeSceneLoad;
    }

    private protected override void Unload()
    {
        RestoreVanillaHealthIfPresent();
        moduleActive = false;
        On.HealthManager.Awake -= OnHealthManagerAwake_OroMato;
        On.HealthManager.Start -= OnHealthManagerStart_OroMato;
        On.HealthManager.Update -= OnHealthManagerUpdate_OroMato;
        USceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
        ModHooks.BeforeSceneLoadHook -= OnBeforeSceneLoad;
        vanillaHpByInstance.Clear();
        oroPhase2ByInstance.Clear();
        hoGEntryAllowed = false;
    }

    internal static void ReapplyLiveSettings()
    {
        if (!moduleActive)
        {
            return;
        }

        ApplyOroMatoHealthIfPresent();
    }

    internal static void SetP5HpEnabled(bool value)
    {
        if (value)
        {
            if (!oroMatoP5Hp)
            {
                oroMatoOroPhase1HpBeforeP5 = ClampOroMatoHp(oroMatoOroPhase1Hp);
                oroMatoOroPhase2HpBeforeP5 = ClampOroMatoHp(oroMatoOroPhase2Hp);
                oroMatoMatoHpBeforeP5 = ClampOroMatoHp(oroMatoMatoHp);
                oroMatoHasStoredStateBeforeP5 = true;
            }

            oroMatoP5Hp = true;
            oroMatoOroPhase1Hp = P5OroPhase1Hp;
            oroMatoOroPhase2Hp = P5OroPhase2Hp;
            oroMatoMatoHp = P5MatoHp;
        }
        else
        {
            if (oroMatoP5Hp && oroMatoHasStoredStateBeforeP5)
            {
                oroMatoOroPhase1Hp = ClampOroMatoHp(oroMatoOroPhase1HpBeforeP5);
                oroMatoOroPhase2Hp = ClampOroMatoHp(oroMatoOroPhase2HpBeforeP5);
                oroMatoMatoHp = ClampOroMatoHp(oroMatoMatoHpBeforeP5);
            }

            oroMatoP5Hp = false;
            oroMatoHasStoredStateBeforeP5 = false;
        }

        ReapplyLiveSettings();
    }

    private static void NormalizeP5State()
    {
        if (!oroMatoP5Hp)
        {
            oroMatoOroPhase1Hp = ClampOroMatoHp(oroMatoOroPhase1Hp);
            oroMatoOroPhase2Hp = ClampOroMatoHp(oroMatoOroPhase2Hp);
            oroMatoMatoHp = ClampOroMatoHp(oroMatoMatoHp);
            return;
        }

        if (!oroMatoHasStoredStateBeforeP5)
        {
            oroMatoOroPhase1HpBeforeP5 = ClampOroMatoHp(oroMatoOroPhase1Hp);
            oroMatoOroPhase2HpBeforeP5 = ClampOroMatoHp(oroMatoOroPhase2Hp);
            oroMatoMatoHpBeforeP5 = ClampOroMatoHp(oroMatoMatoHp);
            oroMatoHasStoredStateBeforeP5 = true;
        }

        oroMatoOroPhase1Hp = P5OroPhase1Hp;
        oroMatoOroPhase2Hp = P5OroPhase2Hp;
        oroMatoMatoHp = P5MatoHp;
    }

    internal static void ApplyOroMatoHealthIfPresent()
    {
        if (!moduleActive)
        {
            return;
        }

        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsOroOrMato(hm) || !ShouldApplySettings(hm.gameObject))
            {
                continue;
            }

            if (IsMato(hm))
            {
                ApplyMatoHealth(hm.gameObject, hm);
                continue;
            }

            bool phase2 = GetOroPhase2State(hm);
            if (phase2)
            {
                ApplyOroPhase2Health(hm.gameObject, hm);
            }
            else
            {
                ApplyOroPhase1Health(hm.gameObject, hm);
            }
        }
    }

    internal static void RestoreVanillaHealthIfPresent()
    {
        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsOroOrMato(hm))
            {
                continue;
            }

            RestoreVanillaHealth(hm.gameObject, hm);
        }
    }

    private static void OnHealthManagerAwake_OroMato(On.HealthManager.orig_Awake orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsOroOrMato(self))
        {
            return;
        }

        RememberVanillaHp(self);
        UpdateOroPhaseState(self);
        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }

        ApplyHealthByTypeAndPhase(self.gameObject, self);
    }

    private static void OnHealthManagerStart_OroMato(On.HealthManager.orig_Start orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsOroOrMato(self))
        {
            return;
        }

        RememberVanillaHp(self);
        UpdateOroPhaseState(self);
        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }

        ApplyHealthByTypeAndPhase(self.gameObject, self);
    }

    private static void OnHealthManagerUpdate_OroMato(On.HealthManager.orig_Update orig, HealthManager self)
    {
        orig(self);

        if (!moduleActive || !IsOro(self) || self.gameObject == null || !ShouldApplySettings(self.gameObject))
        {
            return;
        }

        int instanceId = self.GetInstanceID();
        bool phase2 = oroPhase2ByInstance.TryGetValue(instanceId, out bool stored) && stored;
        if (!phase2)
        {
            int phase1Hp = ClampOroMatoHp(oroMatoOroPhase1Hp);
            if (self.hp > phase1Hp)
            {
                oroPhase2ByInstance[instanceId] = true;
                ApplyOroPhase2Health(self.gameObject, self);
                return;
            }

            return;
        }

        int targetHp = ClampOroMatoHp(oroMatoOroPhase2Hp);
        if (self.hp > targetHp)
        {
            ApplyOroPhase2Health(self.gameObject, self);
        }
    }

    private static void SceneManager_activeSceneChanged(Scene from, Scene to)
    {
        UpdateHoGEntryAllowed(from.name, to.name);
        if (!string.Equals(to.name, OroMatoScene, StringComparison.Ordinal))
        {
            vanillaHpByInstance.Clear();
            oroPhase2ByInstance.Clear();
            return;
        }

        if (!moduleActive)
        {
            return;
        }

        ApplyOroMatoHealthIfPresent();
    }

    private static string OnBeforeSceneLoad(string newSceneName)
    {
        UpdateHoGEntryAllowed(USceneManager.GetActiveScene().name, newSceneName);
        return newSceneName;
    }

    private static bool IsOroOrMato(HealthManager hm)
    {
        return IsOro(hm) || IsMato(hm);
    }

    private static bool IsOro(HealthManager hm)
    {
        return hm != null && hm.gameObject != null && IsOroObject(hm.gameObject);
    }

    private static bool IsMato(HealthManager hm)
    {
        return hm != null && hm.gameObject != null && IsMatoObject(hm.gameObject);
    }

    private static bool IsOroObject(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return false;
        }

        return string.Equals(gameObject.scene.name, OroMatoScene, StringComparison.Ordinal)
            && gameObject.name.StartsWith(OroName, StringComparison.Ordinal);
    }

    private static bool IsMatoObject(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return false;
        }

        return string.Equals(gameObject.scene.name, OroMatoScene, StringComparison.Ordinal)
            && gameObject.name.StartsWith(MatoName, StringComparison.Ordinal);
    }

    private static bool IsOroOrMatoObject(GameObject gameObject)
    {
        return IsOroObject(gameObject) || IsMatoObject(gameObject);
    }

    private static bool ShouldApplySettings(GameObject? gameObject)
    {
        if (gameObject == null || !IsOroOrMatoObject(gameObject))
        {
            return false;
        }

        return hoGEntryAllowed;
    }

    private static void UpdateHoGEntryAllowed(string currentScene, string nextScene)
    {
        if (string.Equals(nextScene, OroMatoScene, StringComparison.Ordinal))
        {
            if (BossManipulateEntryGuard.IsAllowedBossEntry(currentScene, nextScene))
            {
                hoGEntryAllowed = true;
            }
            else if (string.Equals(currentScene, OroMatoScene, StringComparison.Ordinal) && hoGEntryAllowed)
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

    private static void ApplyHealthByTypeAndPhase(GameObject boss, HealthManager hm)
    {
        if (IsMato(hm))
        {
            ApplyMatoHealth(boss, hm);
            return;
        }

        bool phase2 = GetOroPhase2State(hm);
        if (phase2)
        {
            ApplyOroPhase2Health(boss, hm);
        }
        else
        {
            ApplyOroPhase1Health(boss, hm);
        }
    }

    private static bool GetOroPhase2State(HealthManager hm)
    {
        int instanceId = hm.GetInstanceID();
        bool phase2 = oroPhase2ByInstance.TryGetValue(instanceId, out bool stored) && stored;
        if (phase2)
        {
            return true;
        }

        oroPhase2ByInstance[instanceId] = false;
        return false;
    }

    private static void UpdateOroPhaseState(HealthManager hm)
    {
        if (!IsOro(hm))
        {
            return;
        }

        int instanceId = hm.GetInstanceID();
        if (!oroPhase2ByInstance.ContainsKey(instanceId))
        {
            oroPhase2ByInstance[instanceId] = false;
        }
    }

    private static void ApplyOroPhase1Health(GameObject boss, HealthManager? hm = null)
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

        int targetHp = ClampOroMatoHp(oroMatoOroPhase1Hp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static void ApplyOroPhase2Health(GameObject boss, HealthManager? hm = null)
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

        int targetHp = ClampOroMatoHp(oroMatoOroPhase2Hp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static void ApplyMatoHealth(GameObject boss, HealthManager? hm = null)
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

        int targetHp = ClampOroMatoHp(oroMatoMatoHp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static void RestoreVanillaHealth(GameObject boss, HealthManager? hm = null)
    {
        if (!hoGEntryAllowed)
        {
            return;
        }

        if (boss == null || !IsOroOrMatoObject(boss))
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

        int targetHp = ClampOroMatoHp(vanillaHp);
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

    private static int ClampOroMatoHp(int value)
    {
        if (value < MinOroMatoHp)
        {
            return MinOroMatoHp;
        }

        return value > MaxOroMatoHp ? MaxOroMatoHp : value;
    }
}
