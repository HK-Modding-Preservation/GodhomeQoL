using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using Satchel;
using Satchel.Futils;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed partial class WingedNoskHelper : Module
{
    private const string WingedNoskSummonStateName = "Summon";
    private const string WingedNoskSummonRoarStateName = "Summon Roar";
    private const int DefaultWingedNoskSummonHp = 1;
    private const int DefaultWingedNoskSummonVanillaHp = 8;
    private const int DefaultWingedNoskSummonLimit = 5;
    private const int DefaultWingedNoskVanillaSummonLimit = 5;
    private const int MinWingedNoskSummonLimit = 0;
    private const int MaxWingedNoskSummonLimit = 999;

    internal static void ApplyWingedNoskSummonHealthIfPresent()
    {
        if (!moduleActive || !ShouldUseCustomSummonHp())
        {
            return;
        }

        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsWingedNoskSummon(hm))
            {
                continue;
            }

            ApplyWingedNoskSummonHealth(hm.gameObject, hm);
        }
    }

    internal static void RestoreVanillaSummonHealthIfPresent()
    {
        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsWingedNoskSummon(hm))
            {
                continue;
            }

            RestoreVanillaSummonHealth(hm.gameObject, hm);
        }
    }

    internal static void ApplySummonLimitSettingsIfPresent()
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

    internal static void RestoreVanillaSummonLimitsIfPresent()
    {
        foreach (PlayMakerFSM fsm in UObject.FindObjectsOfType<PlayMakerFSM>())
        {
            if (fsm == null || fsm.gameObject == null || !IsWingedNoskSummonControlFsm(fsm))
            {
                continue;
            }

            if (!ShouldApplySettings(fsm.gameObject))
            {
                continue;
            }

            RememberVanillaSummonLimit(fsm);
            SetSummonLimitOnFsm(fsm, GetVanillaSummonLimit(fsm), useExactCustomLimit: false);
        }
    }

    private static void ApplyWingedNoskSummonHealth(GameObject summon, HealthManager? hm = null)
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
        int targetHp = ClampWingedNoskHp(wingedNoskSummonHp);
        summon.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static void ApplySummonLimitSettings(PlayMakerFSM fsm)
    {
        if (fsm == null || fsm.gameObject == null || !IsWingedNoskSummonControlFsm(fsm))
        {
            return;
        }

        if (!ShouldApplySettings(fsm.gameObject))
        {
            return;
        }

        RememberVanillaSummonLimit(fsm);
        bool useCustomSummonLimit = ShouldUseCustomSummonLimit();
        int targetLimit = useCustomSummonLimit
            ? ClampWingedNoskSummonLimit(wingedNoskSummonLimit)
            : GetVanillaSummonLimit(fsm);
        SetSummonLimitOnFsm(fsm, targetLimit, useCustomSummonLimit);
    }

    private static void SetSummonLimitOnFsm(PlayMakerFSM fsm, int value, bool useExactCustomLimit)
    {
        IntCompare? compare = FindSummonEnemyCountCompareAction(fsm);
        if (compare == null)
        {
            return;
        }

        int clampedLimit = ClampWingedNoskSummonLimit(value);
        int thresholdValue = useExactCustomLimit
            ? ConvertCustomSummonLimitToThreshold(clampedLimit)
            : clampedLimit;
        if (compare.integer1 != null
            && string.Equals(compare.integer1.Name, WingedNoskEnemyCountVariableName, StringComparison.Ordinal)
            && compare.integer2 != null)
        {
            compare.integer2.UseVariable = false;
            compare.integer2.Name = string.Empty;
            compare.integer2.Value = thresholdValue;
            return;
        }

        if (compare.integer2 != null
            && string.Equals(compare.integer2.Name, WingedNoskEnemyCountVariableName, StringComparison.Ordinal)
            && compare.integer1 != null)
        {
            compare.integer1.UseVariable = false;
            compare.integer1.Name = string.Empty;
            compare.integer1.Value = thresholdValue;
            return;
        }

        if (compare.integer2 != null)
        {
            compare.integer2.UseVariable = false;
            compare.integer2.Name = string.Empty;
            compare.integer2.Value = thresholdValue;
        }
    }

    private static int ConvertCustomSummonLimitToThreshold(int desiredLimit)
    {
        int clampedDesiredLimit = ClampWingedNoskSummonLimit(desiredLimit);
        return clampedDesiredLimit <= int.MinValue + 1 ? int.MinValue : clampedDesiredLimit - 1;
    }

    private static IntCompare? FindSummonEnemyCountCompareAction(PlayMakerFSM fsm)
    {
        if (fsm == null || fsm.Fsm == null)
        {
            return null;
        }

        IntCompare? inSummon = FindEnemyCountCompareInState(fsm, WingedNoskSummonStateName);
        if (inSummon != null)
        {
            return inSummon;
        }

        return FindEnemyCountCompareInState(fsm, WingedNoskSummonRoarStateName);
    }

    private static void RememberVanillaSummonLimit(PlayMakerFSM fsm)
    {
        int fsmId = fsm.GetInstanceID();
        if (vanillaSummonLimitByFsm.ContainsKey(fsmId))
        {
            return;
        }

        int limit = DefaultWingedNoskVanillaSummonLimit;
        IntCompare? compare = FindSummonEnemyCountCompareAction(fsm);
        if (compare != null)
        {
            limit = ReadEnemyCountThreshold(compare, DefaultWingedNoskVanillaSummonLimit);
        }

        vanillaSummonLimitByFsm[fsmId] = ClampWingedNoskSummonLimit(limit);
    }

    private static int GetVanillaSummonLimit(PlayMakerFSM fsm)
    {
        int fsmId = fsm.GetInstanceID();
        if (vanillaSummonLimitByFsm.TryGetValue(fsmId, out int limit))
        {
            return ClampWingedNoskSummonLimit(limit);
        }

        RememberVanillaSummonLimit(fsm);
        if (vanillaSummonLimitByFsm.TryGetValue(fsmId, out limit))
        {
            return ClampWingedNoskSummonLimit(limit);
        }

        return DefaultWingedNoskVanillaSummonLimit;
    }

    private static void RestoreVanillaSummonHealth(GameObject summon, HealthManager? hm = null)
    {
        if (!hoGEntryAllowed)
        {
            return;
        }

        if (summon == null || !IsWingedNoskSummonObject(summon))
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

        int targetHp = ClampWingedNoskHp(vanillaHp);
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
            hp = DefaultWingedNoskSummonVanillaHp;
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
            hp = DefaultWingedNoskSummonVanillaHp;
        }

        return hp > 0;
    }

    private static int ClampWingedNoskSummonLimit(int value)
    {
        if (value < MinWingedNoskSummonLimit)
        {
            return MinWingedNoskSummonLimit;
        }

        return value > MaxWingedNoskSummonLimit ? MaxWingedNoskSummonLimit : value;
    }
}
