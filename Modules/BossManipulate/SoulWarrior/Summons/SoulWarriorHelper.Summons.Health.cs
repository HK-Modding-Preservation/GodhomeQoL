using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using Satchel;
using Satchel.Futils;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed partial class SoulWarriorHelper : Module
{
    private const int DefaultSoulWarriorSummonHp = 13;

    internal static void ApplySoulWarriorSummonHealthIfPresent()
    {
        if (!moduleActive || !ShouldUseCustomSummonHp())
        {
            return;
        }

        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsSoulWarriorSummon(hm))
            {
                continue;
            }

            ApplySoulWarriorSummonHealth(hm.gameObject, hm);
        }
    }

    internal static void RestoreVanillaSummonHealthIfPresent()
    {
        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsSoulWarriorSummon(hm))
            {
                continue;
            }

            RestoreVanillaSummonHealth(hm.gameObject, hm);
        }
    }

    private static void ApplySoulWarriorSummonHealth(GameObject summon, HealthManager? hm = null)
    {
        if (!ShouldApplySummonSettings(summon) || !ShouldUseCustomSummonHp())
        {
            return;
        }

        hm ??= summon.GetComponent<HealthManager>();
        if (hm == null)
        {
            return;
        }

        RememberVanillaSummonHp(hm);
        int targetHp = ClampSoulWarriorHp(soulWarriorSummonHp);
        summon.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static void RestoreVanillaSummonHealth(GameObject summon, HealthManager? hm = null)
    {
        if (!hoGEntryAllowed)
        {
            return;
        }

        if (summon == null || !IsSoulWarriorSummonObject(summon))
        {
            return;
        }

        hm ??= summon.GetComponent<HealthManager>();
        if (hm == null)
        {
            return;
        }

        if (!TryGetVanillaSummonHp(hm, out int vanillaHp))
        {
            return;
        }

        int targetHp = ClampSoulWarriorHp(vanillaHp);
        summon.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static void RememberVanillaSummonHp(HealthManager hm)
    {
        int instanceId = hm.GetInstanceID();
        if (vanillaSummonHpByInstance.ContainsKey(instanceId))
        {
            return;
        }

        int hp = hm.hp;

        if (hp <= 0)
        {
            hp = DefaultSoulWarriorSummonVanillaHp;
        }

        vanillaSummonHpByInstance[instanceId] = hp;
    }

    private static bool TryGetVanillaSummonHp(HealthManager hm, out int hp)
    {
        if (vanillaSummonHpByInstance.TryGetValue(hm.GetInstanceID(), out hp) && hp > 0)
        {
            return true;
        }

        hp = hm.hp;

        if (hp <= 0)
        {
            hp = DefaultSoulWarriorSummonVanillaHp;
        }

        return hp > 0;
    }
}
