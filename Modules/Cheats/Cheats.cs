using InControl;
using GodhomeQoL.Modules.Tools;

namespace GodhomeQoL.Modules.Cheats;

public sealed class Cheats : Module
{
    [GlobalSetting] public static bool InfiniteSoulEnabled = false;
    [GlobalSetting] public static bool InfiniteHpEnabled = false;
    [GlobalSetting] public static bool InvincibilityEnabled = false;
    [GlobalSetting] public static bool NoclipEnabled = false;
    [GlobalSetting] public static string KillAllHotkeyRaw = string.Empty;

    public override bool DefaultEnabled => false;
    public override bool Hidden => true;

    private const float NoclipSpeed = 20f;
    private static readonly HashSet<string> FalseKnightBodies = new(StringComparer.OrdinalIgnoreCase)
    {
        "False Knight New",
        "False Knight Dream"
    };
    private static readonly HashSet<string> InstantCompleteScenes = new(StringComparer.OrdinalIgnoreCase)
    {
        "GG_False_Knight",
        "GG_Failed_Champion",
        "GG_Nailmasters",
        "GG_Radiance"
    };

    private static Vector3 noclipPosition;
    private static int invincibilityClaimHandle;

    private protected override void Load()
    {
        ModHooks.TakeHealthHook += OnTakeHealth;
        ModHooks.HeroUpdateHook += OnHeroUpdate;
        SyncInvincibilityClaimState();

        if (NoclipEnabled && HeroController.instance != null)
        {
            noclipPosition = HeroController.instance.transform.position;
        }
    }

    private protected override void Unload()
    {
        ModHooks.TakeHealthHook -= OnTakeHealth;
        ModHooks.HeroUpdateHook -= OnHeroUpdate;
        ReleaseInvincibilityClaim();
    }

    internal static bool GetInfiniteSoulEnabled() => InfiniteSoulEnabled;
    internal static bool GetInfiniteHpEnabled() => InfiniteHpEnabled;
    internal static bool GetInvincibilityEnabled() => InvincibilityEnabled;
    internal static bool GetNoclipEnabled() => NoclipEnabled;
    internal static string GetKillAllHotkeyRaw() => KillAllHotkeyRaw ?? string.Empty;

    internal static void SetInfiniteSoulEnabled(bool value)
    {
        InfiniteSoulEnabled = value;
        GodhomeQoL.SaveGlobalSettingsSafe();
    }

    internal static void SetInfiniteHpEnabled(bool value)
    {
        InfiniteHpEnabled = value;
        GodhomeQoL.SaveGlobalSettingsSafe();
    }

    internal static void SetInvincibilityEnabled(bool value)
    {
        InvincibilityEnabled = value;
        if (ModuleManager.IsModuleLoaded<Cheats>() || invincibilityClaimHandle != 0)
        {
            SyncInvincibilityClaimState();
        }

        GodhomeQoL.SaveGlobalSettingsSafe();
    }

    internal static void SetNoclipEnabled(bool value)
    {
        NoclipEnabled = value;
        if (value && HeroController.instance != null)
        {
            noclipPosition = HeroController.instance.transform.position;
        }

        GodhomeQoL.SaveGlobalSettingsSafe();
    }

    internal static void SetKillAllHotkeyRaw(string value)
    {
        KillAllHotkeyRaw = value ?? string.Empty;
        GodhomeQoL.SaveGlobalSettingsSafe();
    }

    internal static KeyCode GetKillAllHotkey()
    {
        string raw = GetKillAllHotkeyRaw();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return KeyCode.None;
        }

        return Enum.TryParse(raw, true, out KeyCode key) ? key : KeyCode.None;
    }

    internal static int KillAll()
    {
        ShowHPOnDeath.MarkCheatKill();
        if (TryCompleteInstantSceneFight())
        {
            return 1;
        }

        int killed = 0;
        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.isDead)
            {
                continue;
            }

            if (IsFalseKnightBody(hm))
            {
                hm.hp = 0;
                killed++;
                continue;
            }

            hm.Die(null, AttackTypes.Generic, true);
            killed++;
        }

        return killed;
    }

    private static bool TryCompleteInstantSceneFight()
    {
        string sceneName = USceneManager.GetActiveScene().name;
        if (!InstantCompleteScenes.Contains(sceneName))
        {
            return false;
        }

        if (BossSceneController.Instance is not BossSceneController controller)
        {
            return false;
        }

        controller.bossesDeadWaitTime = 0f;
        controller.EndBossScene();
        return true;
    }

    private static bool IsFalseKnightBody(HealthManager hm)
    {
        GameObject? go = hm.gameObject;
        if (go == null)
        {
            return false;
        }

        string name = go.name;
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        if (FalseKnightBodies.Contains(name))
        {
            return true;
        }

        int suffixStart = name.IndexOf(" (", StringComparison.Ordinal);
        return suffixStart > 0 && FalseKnightBodies.Contains(name.Substring(0, suffixStart));
    }

    private static int OnTakeHealth(int damageAmount) =>
        InfiniteHpEnabled ? 0 : damageAmount;

    private static void SyncInvincibilityClaimState()
    {
        if (InvincibilityEnabled)
        {
            if (invincibilityClaimHandle == 0)
            {
                invincibilityClaimHandle = InvincibilityClaims.Acquire(nameof(Cheats));
            }

            return;
        }

        ReleaseInvincibilityClaim();
    }

    private static void ReleaseInvincibilityClaim()
    {
        if (invincibilityClaimHandle == 0)
        {
            return;
        }

        InvincibilityClaims.Release(invincibilityClaimHandle);
        invincibilityClaimHandle = 0;
    }

    private static void OnHeroUpdate()
    {
        HeroController? hero = HeroController.instance;
        PlayerData? pd = PlayerData.instance;
        if (hero == null || pd == null)
        {
            return;
        }

        if (InfiniteSoulEnabled
            && pd.health > 0
            && !hero.cState.dead
            && GameManager.instance != null
            && GameManager.instance.IsGameplayScene())
        {
            pd.MPCharge = Math.Max(0, pd.maxMP - 1);
            if (pd.MPReserveMax > 0)
            {
                pd.MPReserve = Math.Max(0, pd.MPReserveMax - 1);
                hero.AddMPCharge(2);
            }

            hero.AddMPCharge(1);
        }

        if (!NoclipEnabled)
        {
            return;
        }

        HeroActions? actions = InputHandler.Instance?.inputActions;
        float step = Time.deltaTime * NoclipSpeed;
        if (actions != null)
        {
            if (actions.left.IsPressed)
            {
                noclipPosition.x -= step;
            }

            if (actions.right.IsPressed)
            {
                noclipPosition.x += step;
            }

            if (actions.up.IsPressed)
            {
                noclipPosition.y += step;
            }

            if (actions.down.IsPressed)
            {
                noclipPosition.y -= step;
            }
        }

        if (hero.transitionState == HeroTransitionState.WAITING_TO_TRANSITION)
        {
            hero.transform.position = noclipPosition;
        }
        else
        {
            noclipPosition = hero.transform.position;
        }
    }
}
