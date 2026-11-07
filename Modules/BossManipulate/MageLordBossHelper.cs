using Modding;
using Satchel;
using Satchel.Futils;

namespace GodhomeQoL.Modules.BossChallenge;

public abstract class MageLordBossHelper : Module
{
    private protected const string BossPhase2Marker = "Phase2";
    private protected const int P5BossPhase2Hp = 350;
    private protected const int MinBossHp = 1;
    private protected const int MaxBossHp = 999999;

    private readonly Dictionary<int, int> vanillaHpByInstance = new();
    private bool moduleActive;
    private bool hoGEntryAllowed;

    private protected abstract string SceneName { get; }
    private protected abstract string BossObjectName { get; }
    private protected abstract int P5Phase1Hp { get; }
    private protected abstract bool P5HpEnabled { get; set; }
    private protected abstract int Phase1Hp { get; set; }
    private protected abstract int Phase2Hp { get; set; }
    private protected abstract int Phase1HpBeforeP5 { get; set; }
    private protected abstract int Phase2HpBeforeP5 { get; set; }
    private protected abstract bool HasStoredStateBeforeP5 { get; set; }

    public override ToggleableLevel ToggleableLevel => ToggleableLevel.ChangeScene;

    private protected override void Load()
    {
        moduleActive = true;
        BossManipulateEntryGuard.EnsureHooks();
        NormalizeP5State();
        vanillaHpByInstance.Clear();
        On.HealthManager.Awake += OnHealthManagerAwake_Boss;
        On.HealthManager.Start += OnHealthManagerStart_Boss;
        USceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        ModHooks.BeforeSceneLoadHook += OnBeforeSceneLoad;
    }

    private protected override void Unload()
    {
        RestoreVanillaHealthIfPresentCore();
        moduleActive = false;
        On.HealthManager.Awake -= OnHealthManagerAwake_Boss;
        On.HealthManager.Start -= OnHealthManagerStart_Boss;
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

        ApplyHealthIfPresentCore();
    }

    internal void SetP5HpEnabledCore(bool value)
    {
        if (value)
        {
            if (!P5HpEnabled)
            {
                Phase1HpBeforeP5 = ClampBossHp(Phase1Hp);
                Phase2HpBeforeP5 = ClampBossHp(Phase2Hp);
                HasStoredStateBeforeP5 = true;
            }

            P5HpEnabled = true;
            Phase1Hp = P5Phase1Hp;
            Phase2Hp = P5BossPhase2Hp;
        }
        else
        {
            if (P5HpEnabled && HasStoredStateBeforeP5)
            {
                Phase1Hp = ClampBossHp(Phase1HpBeforeP5);
                Phase2Hp = ClampBossHp(Phase2HpBeforeP5);
            }

            P5HpEnabled = false;
            HasStoredStateBeforeP5 = false;
        }

        ReapplyLiveSettingsCore();
    }

    private void NormalizeP5State()
    {
        if (!P5HpEnabled)
        {
            Phase1Hp = ClampBossHp(Phase1Hp);
            Phase2Hp = ClampBossHp(Phase2Hp);
            return;
        }

        if (!HasStoredStateBeforeP5)
        {
            Phase1HpBeforeP5 = ClampBossHp(Phase1Hp);
            Phase2HpBeforeP5 = ClampBossHp(Phase2Hp);
            HasStoredStateBeforeP5 = true;
        }

        Phase1Hp = P5Phase1Hp;
        Phase2Hp = P5BossPhase2Hp;
    }

    internal void ApplyHealthIfPresentCore()
    {
        if (!moduleActive)
        {
            return;
        }

        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || !IsBoss(hm) || hm.gameObject == null || !ShouldApplySettings(hm.gameObject))
            {
                continue;
            }

            if (IsBossPhase2Object(hm.gameObject))
            {
                ApplyPhase2Health(hm.gameObject, hm);
            }
            else
            {
                ApplyPhase1Health(hm.gameObject, hm);
            }
        }
    }

    internal void RestoreVanillaHealthIfPresentCore()
    {
        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || !IsBoss(hm) || hm.gameObject == null)
            {
                continue;
            }

            RestoreVanillaHealth(hm.gameObject, hm);
        }
    }

    private void OnHealthManagerAwake_Boss(On.HealthManager.orig_Awake orig, HealthManager self)
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

        if (IsBossPhase2Object(self.gameObject))
        {
            ApplyPhase2Health(self.gameObject, self);
        }
        else
        {
            ApplyPhase1Health(self.gameObject, self);
        }
    }

    private void OnHealthManagerStart_Boss(On.HealthManager.orig_Start orig, HealthManager self)
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

        if (IsBossPhase2Object(self.gameObject))
        {
            ApplyPhase2Health(self.gameObject, self);
        }
        else
        {
            ApplyPhase1Health(self.gameObject, self);
        }
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

        ApplyHealthIfPresentCore();
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
            && gameObject.name.StartsWith(BossObjectName, StringComparison.Ordinal);
    }

    private bool IsBossPhase2Object(GameObject gameObject)
    {
        return gameObject != null
            && IsBossObject(gameObject)
            && gameObject.name.IndexOf(BossPhase2Marker, StringComparison.Ordinal) >= 0;
    }

    private bool ShouldApplySettings(GameObject? gameObject)
    {
        if (gameObject == null || !IsBossObject(gameObject))
        {
            return false;
        }

        return hoGEntryAllowed;
    }

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

    private void ApplyPhase1Health(GameObject boss, HealthManager? hm = null)
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

        int targetHp = ClampBossHp(Phase1Hp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private void ApplyPhase2Health(GameObject boss, HealthManager? hm = null)
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

        int targetHp = ClampBossHp(Phase2Hp);
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

    private void RememberVanillaHp(HealthManager hm)
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

    private bool TryGetVanillaHp(HealthManager hm, out int hp)
    {
        if (vanillaHpByInstance.TryGetValue(hm.GetInstanceID(), out hp) && hp > 0)
        {
            return true;
        }

        hp = hm.hp;

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
}
