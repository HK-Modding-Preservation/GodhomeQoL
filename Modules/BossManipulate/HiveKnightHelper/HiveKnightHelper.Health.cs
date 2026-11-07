using Modding;
using Satchel;
using Satchel.Futils;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed partial class HiveKnightHelper : Module
{
    internal static void ApplyHiveKnightHealthIfPresent()
    {
        if (!moduleActive || !ShouldUseCustomHp())
        {
            return;
        }

        if (!TryFindHiveKnightHealthManager(out HealthManager? hm))
        {
            return;
        }

        if (hm != null && hm.gameObject != null)
        {
            ApplyHiveKnightHealth(hm.gameObject, hm);
        }
    }

    internal static void RestoreVanillaHealthIfPresent()
    {
        if (!TryFindHiveKnightHealthManager(out HealthManager? hm))
        {
            return;
        }

        if (hm != null && hm.gameObject != null)
        {
            RestoreVanillaHealth(hm.gameObject, hm);
        }
    }

    private static void ApplyHiveKnightHealth(GameObject boss, HealthManager? hm = null)
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
        int targetHp = ClampHiveKnightHp(hiveKnightMaxHp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static void RestoreVanillaHealth(GameObject boss, HealthManager? hm = null)
    {
        if (!hoGEntryAllowed)
        {
            return;
        }

        if (boss == null || !IsHiveKnightObject(boss))
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

        int targetHp = ClampHiveKnightHp(vanillaHp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static bool TryFindHiveKnightHealthManager(out HealthManager? hm)
    {
        hm = null;
        foreach (HealthManager candidate in UObject.FindObjectsOfType<HealthManager>())
        {
            if (candidate != null && IsHiveKnight(candidate))
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

        hp = Math.Max(hp, DefaultHiveKnightVanillaHp);
        if (hp > 0)
        {
            vanillaHpByInstance[instanceId] = hp;
        }
    }

    private static bool TryGetVanillaHp(HealthManager hm, out int hp)
    {
        if (vanillaHpByInstance.TryGetValue(hm.GetInstanceID(), out hp) && hp > 0)
        {
            hp = Math.Max(hp, DefaultHiveKnightVanillaHp);
            return true;
        }

        hp = hm.hp;

        hp = Math.Max(hp, DefaultHiveKnightVanillaHp);
        return hp > 0;
    }

    private static int ClampHiveKnightHp(int value)
    {
        if (value < MinHiveKnightHp)
        {
            return MinHiveKnightHp;
        }

        return value > MaxHiveKnightHp ? MaxHiveKnightHp : value;
    }
}
