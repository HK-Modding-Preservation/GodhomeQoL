using Modding;
using Satchel;
using Satchel.Futils;

namespace GodhomeQoL.Modules.BossChallenge;

public abstract class SingleBossHpHelper : Module
{
    private const int MinHpLimit = 1;
    private const int MaxHpLimit = 999999;

    private readonly Dictionary<int, int> vanillaHpByInstance = new();
    private bool moduleActive;
    private bool hoGEntryAllowed;

    private protected abstract string SceneName { get; }
    private protected abstract string BossObjectName { get; }
    private protected abstract int DefaultVanillaHp { get; }
    private protected abstract int P5Hp { get; }
    private protected abstract bool UseMaxHp { get; set; }
    private protected abstract bool P5HpEnabled { get; set; }
    private protected abstract int MaxHp { get; set; }
    private protected abstract int MaxHpBeforeP5 { get; set; }
    private protected abstract bool UseMaxHpBeforeP5 { get; set; }
    private protected abstract bool HasStoredStateBeforeP5 { get; set; }

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
        RestoreVanillaHealthIfPresentCore();
        moduleActive = false;
        On.HealthManager.Awake -= OnHealthManagerAwake;
        On.HealthManager.Start -= OnHealthManagerStart;
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
    }

    internal void SetP5HpEnabledCore(bool value)
    {
        if (value)
        {
            if (!P5HpEnabled)
            {
                MaxHpBeforeP5 = ClampHp(MaxHp);
                UseMaxHpBeforeP5 = UseMaxHp;
                HasStoredStateBeforeP5 = true;
            }

            P5HpEnabled = true;
            UseMaxHp = true;
            MaxHp = P5Hp;
        }
        else
        {
            if (P5HpEnabled && HasStoredStateBeforeP5)
            {
                MaxHp = ClampHp(MaxHpBeforeP5);
                UseMaxHp = UseMaxHpBeforeP5;
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
            return;
        }

        if (!HasStoredStateBeforeP5)
        {
            MaxHpBeforeP5 = ClampHp(MaxHp);
            UseMaxHpBeforeP5 = UseMaxHp;
            HasStoredStateBeforeP5 = true;
        }

        UseMaxHp = true;
        MaxHp = P5Hp;
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

    private bool ShouldApplySettings(GameObject? gameObject)
    {
        if (gameObject == null || !IsBossObject(gameObject))
        {
            return false;
        }

        return hoGEntryAllowed;
    }

    private bool ShouldUseCustomHp() => UseMaxHp;

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
        int targetHp = ClampHp(MaxHp);
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

        int targetHp = ClampHp(vanillaHp);
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

        hp = Math.Max(hp, DefaultVanillaHp);
        if (hp > 0)
        {
            vanillaHpByInstance[instanceId] = hp;
        }
    }

    private bool TryGetVanillaHp(HealthManager hm, out int hp)
    {
        if (vanillaHpByInstance.TryGetValue(hm.GetInstanceID(), out hp) && hp > 0)
        {
            hp = Math.Max(hp, DefaultVanillaHp);
            return true;
        }

        hp = hm.hp;

        hp = Math.Max(hp, DefaultVanillaHp);
        return hp > 0;
    }

    private int ClampHp(int value)
    {
        if (value < MinHpLimit)
        {
            return MinHpLimit;
        }

        return value > MaxHpLimit ? MaxHpLimit : value;
    }
}
