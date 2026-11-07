using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using Satchel;
using Satchel.Futils;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed partial class AbsoluteRadianceHelper : Module
{
    internal static void ApplyAbsoluteRadianceHealthIfPresent()
    {
        if (!moduleActive || !ShouldUseCustomHp())
        {
            return;
        }

        if (!TryFindAbsoluteRadianceHealthManager(out HealthManager? hm))
        {
            return;
        }

        if (hm != null && hm.gameObject != null)
        {
            ApplyAbsoluteRadianceHealth(hm.gameObject, hm);
        }
    }

    internal static void RestoreVanillaHealthIfPresent()
    {
        if (!TryFindAbsoluteRadianceHealthManager(out HealthManager? hm))
        {
            return;
        }

        if (hm != null && hm.gameObject != null)
        {
            RestoreVanillaHealth(hm.gameObject, hm);
        }
    }

    private static void ApplyAbsoluteRadianceHealth(GameObject boss, HealthManager? hm = null)
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
        int targetHp = ClampAbsoluteRadianceHp(absoluteRadianceMaxHp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static void RestoreVanillaHealth(GameObject boss, HealthManager? hm = null)
    {
        if (!hoGEntryAllowed)
        {
            return;
        }

        if (boss == null || !IsAbsoluteRadianceObject(boss))
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

        int targetHp = ClampAbsoluteRadianceHp(vanillaHp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static bool TryFindAbsoluteRadianceHealthManager(out HealthManager? hm)
    {
        hm = null;
        foreach (HealthManager candidate in UObject.FindObjectsOfType<HealthManager>())
        {
            if (candidate != null && IsAbsoluteRadiance(candidate))
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

        hp = Math.Max(hp, DefaultAbsoluteRadianceVanillaHp);
        if (hp > 0)
        {
            vanillaHpByInstance[instanceId] = hp;
        }
    }

    private static bool TryGetVanillaHp(HealthManager hm, out int hp)
    {
        if (vanillaHpByInstance.TryGetValue(hm.GetInstanceID(), out hp) && hp > 0)
        {
            hp = Math.Max(hp, DefaultAbsoluteRadianceVanillaHp);
            return true;
        }

        hp = hm.hp;

        hp = Math.Max(hp, DefaultAbsoluteRadianceVanillaHp);
        return hp > 0;
    }

    private static int ClampAbsoluteRadianceHp(int value)
    {
        if (value < MinAbsoluteRadianceHp)
        {
            return MinAbsoluteRadianceHp;
        }

        return value > MaxAbsoluteRadianceHp ? MaxAbsoluteRadianceHp : value;
    }
}
