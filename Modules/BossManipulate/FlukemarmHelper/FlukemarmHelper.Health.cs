using Modding;
using Satchel;
using Satchel.Futils;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed partial class FlukemarmHelper : Module
{
    internal static void ApplyFlukemarmHealthIfPresent()
    {
        if (!moduleActive)
        {
            return;
        }

        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsFlukemarm(hm))
            {
                continue;
            }

            FlukemarmTarget target = GetFlukemarmTarget(hm.gameObject);
            if (!ShouldUseCustomHp(target))
            {
                continue;
            }

            ApplyFlukemarmHealth(hm.gameObject, hm);
        }
    }

    internal static void RestoreVanillaHealthIfPresent()
    {
        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsFlukemarm(hm))
            {
                continue;
            }

            FlukemarmTarget target = GetFlukemarmTarget(hm.gameObject);
            if (ShouldUseCustomHp(target))
            {
                continue;
            }

            RestoreVanillaHealth(hm.gameObject, hm);
        }
    }

    private static void ApplyFlukemarmHealth(GameObject target, HealthManager? hm = null)
    {
        if (!ShouldApplySettings(target))
        {
            return;
        }

        FlukemarmTarget flukemarmTarget = GetFlukemarmTarget(target);
        if (flukemarmTarget == FlukemarmTarget.Unknown)
        {
            return;
        }

        if (!ShouldUseCustomHp(flukemarmTarget))
        {
            return;
        }

        hm ??= target.GetComponent<HealthManager>();
        if (hm == null)
        {
            return;
        }

        int configuredHp = GetConfiguredHp(flukemarmTarget);
        if (configuredHp <= 0)
        {
            return;
        }

        RememberVanillaHp(hm);
        int targetHp = ClampFlukemarmHp(configuredHp);
        target.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static void RestoreVanillaHealth(GameObject target, HealthManager? hm = null)
    {
        if (!hoGEntryAllowed)
        {
            return;
        }

        if (target == null || !IsFlukemarmObject(target))
        {
            return;
        }

        FlukemarmTarget flukemarmTarget = GetFlukemarmTarget(target);
        if (flukemarmTarget == FlukemarmTarget.Unknown)
        {
            return;
        }

        if (ShouldUseCustomHp(flukemarmTarget))
        {
            return;
        }

        hm ??= target.GetComponent<HealthManager>();
        if (hm == null)
        {
            return;
        }

        if (!TryGetVanillaHp(hm, flukemarmTarget, out int vanillaHp))
        {
            return;
        }

        int targetHp = ClampFlukemarmHp(vanillaHp);
        target.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static FlukemarmTarget GetFlukemarmTarget(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return FlukemarmTarget.Unknown;
        }

        string name = gameObject.name;
        if (name.StartsWith(FlukemarmName, StringComparison.Ordinal))
        {
            return FlukemarmTarget.Boss;
        }

        if (name.StartsWith(FlukeFlyName, StringComparison.Ordinal) || name.IndexOf(FlukeFlyName, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return FlukemarmTarget.Fly;
        }

        return FlukemarmTarget.Unknown;
    }

    private static int GetConfiguredHp(FlukemarmTarget target)
    {
        return target switch
        {
            FlukemarmTarget.Boss => flukemarmMaxHp,
            FlukemarmTarget.Fly => flukemarmFlyHp,
            _ => 0,
        };
    }

    private static int GetDefaultVanillaHp(FlukemarmTarget target)
    {
        return target switch
        {
            FlukemarmTarget.Fly => DefaultFlukeFlyVanillaHp,
            _ => DefaultFlukemarmVanillaHp,
        };
    }

    private static void RememberVanillaHp(HealthManager hm)
    {
        int instanceId = hm.GetInstanceID();
        if (vanillaHpByInstance.ContainsKey(instanceId))
        {
            return;
        }

        FlukemarmTarget target = GetFlukemarmTarget(hm.gameObject);
        if (target == FlukemarmTarget.Unknown)
        {
            return;
        }

        int hp = hm.hp;

        hp = Math.Max(hp, GetDefaultVanillaHp(target));
        if (hp > 0)
        {
            vanillaHpByInstance[instanceId] = hp;
        }
    }

    private static bool TryGetVanillaHp(HealthManager hm, FlukemarmTarget target, out int hp)
    {
        int minimumVanillaHp = GetDefaultVanillaHp(target);
        if (vanillaHpByInstance.TryGetValue(hm.GetInstanceID(), out hp) && hp > 0)
        {
            hp = Math.Max(hp, minimumVanillaHp);
            return true;
        }

        hp = hm.hp;

        hp = Math.Max(hp, minimumVanillaHp);
        return hp > 0;
    }

    private static int ClampFlukemarmHp(int value)
    {
        if (value < MinFlukemarmHp)
        {
            return MinFlukemarmHp;
        }

        return value > MaxFlukemarmHp ? MaxFlukemarmHp : value;
    }
}
