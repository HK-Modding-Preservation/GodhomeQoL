using HutongGames.PlayMaker;
using Modding;
using Satchel;
using Satchel.Futils;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed partial class NightmareKingGrimmHelper : Module
{
    internal static void ApplyNightmareKingGrimmHealthIfPresent()
    {
        if (!moduleActive || !ShouldUseCustomHp())
        {
            return;
        }

        if (!TryFindNightmareKingGrimmHealthManager(out HealthManager? hm))
        {
            return;
        }

        if (hm != null && hm.gameObject != null)
        {
            ApplyNightmareKingGrimmHealth(hm.gameObject, hm);
        }
    }

    internal static void RestoreVanillaHealthIfPresent()
    {
        if (!TryFindNightmareKingGrimmHealthManager(out HealthManager? hm))
        {
            return;
        }

        if (hm != null && hm.gameObject != null)
        {
            RestoreVanillaHealth(hm.gameObject, hm);
        }
    }

    private static void ApplyNightmareKingGrimmHealth(GameObject boss, HealthManager? hm = null)
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
        int targetHp = ClampNightmareKingGrimmHp(nightmareKingGrimmMaxHp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static void RestoreVanillaHealth(GameObject boss, HealthManager? hm = null)
    {
        if (!hoGEntryAllowed)
        {
            return;
        }

        if (boss == null || !IsNightmareKingGrimmObject(boss))
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

        int targetHp = ClampNightmareKingGrimmHp(vanillaHp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static bool TryFindNightmareKingGrimmHealthManager(out HealthManager? hm)
    {
        hm = null;
        foreach (HealthManager candidate in UObject.FindObjectsOfType<HealthManager>())
        {
            if (candidate != null && IsNightmareKingGrimm(candidate))
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

        hp = Math.Max(hp, DefaultNightmareKingGrimmVanillaHp);
        if (hp > 0)
        {
            vanillaHpByInstance[instanceId] = hp;
        }
    }

    private static bool TryGetVanillaHp(HealthManager hm, out int hp)
    {
        if (vanillaHpByInstance.TryGetValue(hm.GetInstanceID(), out hp) && hp > 0)
        {
            hp = Math.Max(hp, DefaultNightmareKingGrimmVanillaHp);
            return true;
        }

        hp = hm.hp;

        hp = Math.Max(hp, DefaultNightmareKingGrimmVanillaHp);
        return hp > 0;
    }

    private static int ClampNightmareKingGrimmHp(int value)
    {
        if (value < MinNightmareKingGrimmHp)
        {
            return MinNightmareKingGrimmHp;
        }

        return value > MaxNightmareKingGrimmHp ? MaxNightmareKingGrimmHp : value;
    }
}
