using HutongGames.PlayMaker.Actions;
using Modding;
using Satchel.Futils;
using Satchel;

namespace GodhomeQoL.Modules.BossChallenge;

public abstract partial class UumuuHelperBase : Module
{
    private protected void ApplyHealthIfPresentCore()
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

    private protected void RestoreVanillaHealthIfPresentCore()
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

    private protected void ApplySummonHealthIfPresentCore()
    {
        if (!moduleActive || !ShouldUseCustomSummonHp())
        {
            return;
        }

        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsBossSummon(hm))
            {
                continue;
            }

            ApplyBossSummonHealth(hm.gameObject, hm);
        }
    }

    private protected void RestoreVanillaSummonHealthIfPresentCore()
    {
        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsBossSummon(hm))
            {
                continue;
            }

            RestoreVanillaSummonHealth(hm.gameObject, hm);
        }
    }

    private protected void ApplyBossHealth(GameObject boss, HealthManager? hm = null)
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
        int targetHp = ClampBossHp(MaxHp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private protected void ApplyBossSummonHealth(GameObject summon, HealthManager? hm = null)
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
        int targetHp = ClampBossHp(SummonHp);
        summon.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private protected void RestoreVanillaHealth(GameObject boss, HealthManager? hm = null)
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

    private protected void RestoreVanillaSummonHealth(GameObject summon, HealthManager? hm = null)
    {
        if (!hoGEntryAllowed)
        {
            return;
        }

        if (summon == null || !IsBossSummonObject(summon))
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

        int targetHp = ClampBossHp(vanillaHp);
        summon.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private protected bool TryFindBossHealthManager(out HealthManager? hm)
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

    private protected void RememberVanillaHp(HealthManager hm)
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

    private protected bool TryGetVanillaHp(HealthManager hm, out int hp)
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

    private protected void RememberVanillaSummonHp(HealthManager hm)
    {
        int instanceId = hm.GetInstanceID();
        if (vanillaSummonHpByInstance.ContainsKey(instanceId))
        {
            return;
        }

        int hp = hm.hp;

        if (hp > 0)
        {
            vanillaSummonHpByInstance[instanceId] = hp;
        }
    }

    private protected bool TryGetVanillaSummonHp(HealthManager hm, out int hp)
    {
        if (vanillaSummonHpByInstance.TryGetValue(hm.GetInstanceID(), out hp) && hp > 0)
        {
            return true;
        }

        hp = hm.hp;

        return hp > 0;
    }

    private protected int ClampBossHp(int value)
    {
        if (value < MinHp)
        {
            return MinHp;
        }

        return value > MaxHpLimit ? MaxHpLimit : value;
    }
}
