using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using Satchel;
using Satchel.Futils;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed partial class SoulWarriorHelper : Module
{
    private const int DefaultSoulWarriorVanillaHp = 1000;

    internal static void RestoreVanillaHealthIfPresent()
    {
        if (!TryFindSoulWarriorHealthManager(out HealthManager? hm))
        {
            return;
        }

        if (hm != null && hm.gameObject != null)
        {
            RestoreVanillaHealth(hm.gameObject, hm);
        }
    }

    private static void ApplySoulWarriorHealth(GameObject boss, HealthManager? hm = null)
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
        int targetHp = ClampSoulWarriorHp(soulWarriorMaxHp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static void RestoreVanillaHealth(GameObject boss, HealthManager? hm = null)
    {
        if (!hoGEntryAllowed)
        {
            return;
        }

        if (boss == null || !IsSoulWarriorObject(boss))
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

        int targetHp = ClampSoulWarriorHp(vanillaHp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static bool TryFindSoulWarriorHealthManager(out HealthManager? hm)
    {
        hm = null;
        foreach (HealthManager candidate in UObject.FindObjectsOfType<HealthManager>())
        {
            if (candidate != null && IsSoulWarrior(candidate))
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

        hp = Math.Max(hp, DefaultSoulWarriorVanillaHp);
        if (hp > 0)
        {
            vanillaHpByInstance[instanceId] = hp;
        }
    }

    private static bool TryGetVanillaHp(HealthManager hm, out int hp)
    {
        if (vanillaHpByInstance.TryGetValue(hm.GetInstanceID(), out hp) && hp > 0)
        {
            hp = Math.Max(hp, DefaultSoulWarriorVanillaHp);
            return true;
        }

        hp = hm.hp;

        hp = Math.Max(hp, DefaultSoulWarriorVanillaHp);
        return hp > 0;
    }

    private static int ClampSoulWarriorHp(int value)
    {
        if (value < MinSoulWarriorHp)
        {
            return MinSoulWarriorHp;
        }

        return value > MaxSoulWarriorHp ? MaxSoulWarriorHp : value;
    }
}
