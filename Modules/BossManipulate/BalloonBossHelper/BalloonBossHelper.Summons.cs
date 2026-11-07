using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using Satchel;
using Satchel.Futils;

namespace GodhomeQoL.Modules.BossChallenge;

public abstract partial class BalloonBossHelper : Module
{
    private protected const int DefaultBossSummonHp = 1;
    private protected const int DefaultBossSummonVanillaHp = 13;
    private protected const int MinBossSummonLimit = 0;
    private protected const int MaxBossSummonLimit = 999;

    internal void ApplySummonHealthIfPresentCore()
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

    internal void RestoreVanillaSummonHealthIfPresentCore()
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

    internal void ApplySummonLimitSettingsIfPresentCore()
    {
        if (!moduleActive)
        {
            return;
        }

        foreach (PlayMakerFSM fsm in UObject.FindObjectsOfType<PlayMakerFSM>())
        {
            if (fsm == null || fsm.gameObject == null)
            {
                continue;
            }

            ApplySummonLimitSettings(fsm);
        }
    }

    internal void RestoreVanillaSummonLimitsIfPresentCore()
    {
        foreach (PlayMakerFSM fsm in UObject.FindObjectsOfType<PlayMakerFSM>())
        {
            if (fsm == null || fsm.gameObject == null || !IsBossSpawnBalloonFsm(fsm))
            {
                continue;
            }

            if (!ShouldApplySettings(fsm.gameObject))
            {
                continue;
            }

            RememberVanillaSummonLimit(fsm);
            SetSummonLimitOnSpawnBalloonFsm(fsm, GetVanillaSummonLimit(fsm));
        }
    }

    internal void EnforceCustomSummonLimitIfPresentCore()
    {
        if (!moduleActive || !ShouldUseCustomSummonLimit())
        {
            return;
        }

        int maxAllowed = ClampBossSummonLimit(SummonLimit);
        int currentAlive = 0;
        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsBossSummon(hm))
            {
                continue;
            }

            if (!ShouldApplySummonSettings(hm.gameObject) || !hm.gameObject.activeInHierarchy)
            {
                continue;
            }

            currentAlive++;
            if (currentAlive > maxAllowed)
            {
                DespawnBossSummon(hm.gameObject);
            }
        }
    }

    private void ApplySummonLimitSettings(PlayMakerFSM fsm)
    {
        if (fsm == null || fsm.gameObject == null || !IsBossSpawnBalloonFsm(fsm))
        {
            return;
        }

        if (!ShouldApplySettings(fsm.gameObject))
        {
            return;
        }

        RememberVanillaSummonLimit(fsm);
        int targetLimit = ShouldUseCustomSummonLimit()
            ? ClampBossSummonLimit(SummonLimit)
            : GetVanillaSummonLimit(fsm);
        SetSummonLimitOnSpawnBalloonFsm(fsm, targetLimit);
    }

    private void SetSummonLimitOnSpawnBalloonFsm(PlayMakerFSM fsm, int value)
    {
        IntCompare? compare = FindSpawnEnemyCountCompareAction(fsm);
        if (compare?.integer2 == null)
        {
            return;
        }

        compare.integer2.UseVariable = false;
        compare.integer2.Name = string.Empty;
        compare.integer2.Value = ClampBossSummonLimit(value);
    }

    private void RememberVanillaSummonLimit(PlayMakerFSM fsm)
    {
        int fsmId = fsm.GetInstanceID();
        if (vanillaSummonLimitByFsm.ContainsKey(fsmId))
        {
            return;
        }

        int limit = DefaultVanillaSummonLimit;
        IntCompare? compare = FindSpawnEnemyCountCompareAction(fsm);
        if (compare?.integer2 != null)
        {
            limit = ClampBossSummonLimit(compare.integer2.Value);
        }

        vanillaSummonLimitByFsm[fsmId] = limit;
    }

    private int GetVanillaSummonLimit(PlayMakerFSM fsm)
    {
        int fsmId = fsm.GetInstanceID();
        if (vanillaSummonLimitByFsm.TryGetValue(fsmId, out int limit))
        {
            return ClampBossSummonLimit(limit);
        }

        RememberVanillaSummonLimit(fsm);
        if (vanillaSummonLimitByFsm.TryGetValue(fsmId, out limit))
        {
            return ClampBossSummonLimit(limit);
        }

        return DefaultVanillaSummonLimit;
    }

    private void ApplyBossSummonHealth(GameObject summon, HealthManager? hm = null)
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

    private void RestoreVanillaSummonHealth(GameObject summon, HealthManager? hm = null)
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

    private void TryEnforceCustomSummonLimit(HealthManager hm)
    {
        if (hm == null || hm.gameObject == null || !ShouldUseCustomSummonLimit())
        {
            return;
        }

        if (!ShouldApplySummonSettings(hm.gameObject) || !hm.gameObject.activeInHierarchy)
        {
            return;
        }

        int maxAllowed = ClampBossSummonLimit(SummonLimit);
        int activeSummons = CountActiveBossSummons();
        if (activeSummons > maxAllowed)
        {
            DespawnBossSummon(hm.gameObject);
        }
    }

    private int CountActiveBossSummons()
    {
        int count = 0;
        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsBossSummon(hm))
            {
                continue;
            }

            if (!ShouldApplySummonSettings(hm.gameObject) || !hm.gameObject.activeInHierarchy)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private void DespawnBossSummon(GameObject summon)
    {
        if (summon == null)
        {
            return;
        }

        try
        {
            if (ObjectPool.IsSpawned(summon))
            {
                ObjectPool.Recycle(summon);
                return;
            }
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, GetType().Name);
        }

        summon.SetActive(false);
    }

    private void RememberVanillaSummonHp(HealthManager hm)
    {
        int instanceId = hm.GetInstanceID();
        if (vanillaSummonHpByInstance.ContainsKey(instanceId))
        {
            return;
        }

        int hp = hm.hp;

        if (hp <= 0)
        {
            hp = DefaultBossSummonVanillaHp;
        }

        vanillaSummonHpByInstance[instanceId] = hp;
    }

    private bool TryGetVanillaSummonHp(HealthManager hm, out int hp)
    {
        if (vanillaSummonHpByInstance.TryGetValue(hm.GetInstanceID(), out hp) && hp > 0)
        {
            return true;
        }

        hp = hm.hp;

        if (hp <= 0)
        {
            hp = DefaultBossSummonVanillaHp;
        }

        return hp > 0;
    }

    private int ClampBossSummonLimit(int value)
    {
        if (value < MinBossSummonLimit)
        {
            return MinBossSummonLimit;
        }

        return value > MaxBossSummonLimit ? MaxBossSummonLimit : value;
    }
}
