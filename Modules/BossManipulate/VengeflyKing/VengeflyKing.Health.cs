using Modding;
using Satchel;
using Satchel.Futils;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed partial class VengeflyKing : Module
{
    private const int DefaultLeftMaxHp = 750;
    private const int DefaultRightMaxHp = 430;
    private const int DefaultSummonMaxHp = 8;
    private const int P5LeftHp = 450;
    private const int P5RightHp = 190;
    private const int P5SummonHp = 8;
    private const int MinHp = 1;
    private const int MaxHp = 999999;

    internal static void ApplyVengeflyHealthIfPresent()
    {
        if (!moduleActive || !ShouldUseCustomHp())
        {
            return;
        }

        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsVengefly(hm))
            {
                continue;
            }

            ApplyVengeflyHealth(hm.gameObject, hm);
        }
    }

    internal static void RestoreVanillaHealthIfPresent()
    {
        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsVengefly(hm))
            {
                continue;
            }

            RestoreVanillaHealth(hm.gameObject, hm);
        }
    }

    private static void ApplyVengeflyHealth(GameObject boss, HealthManager? hm = null)
    {
        if (!ShouldApplySettings(boss) || !ShouldUseCustomHp())
        {
            return;
        }

        VengeflySide side = GetVengeflySide(boss);
        if (side == VengeflySide.Unknown)
        {
            return;
        }

        hm ??= boss.GetComponent<HealthManager>();
        if (hm == null)
        {
            return;
        }

        RememberVanillaHp(hm);
        int configuredHp = side switch
        {
            VengeflySide.Left => vengeflyKingLeftMaxHp,
            VengeflySide.Right => vengeflyKingRightMaxHp,
            VengeflySide.Summon => vengeflyKingSummonMaxHp,
            _ => 0,
        };
        if (configuredHp <= 0)
        {
            return;
        }

        int targetHp = ClampHp(configuredHp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static void RestoreVanillaHealth(GameObject boss, HealthManager? hm = null)
    {
        if (!hoGEntryAllowed)
        {
            return;
        }

        if (boss == null || !IsVengeflyObject(boss))
        {
            return;
        }

        VengeflySide side = GetVengeflySide(boss);
        if (side == VengeflySide.Unknown)
        {
            return;
        }

        hm ??= boss.GetComponent<HealthManager>();
        if (hm == null)
        {
            return;
        }

        if (!TryGetVanillaHp(hm, side, out int vanillaHp))
        {
            return;
        }

        int targetHp = ClampHp(vanillaHp);
        boss.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private static VengeflySide GetVengeflySide(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return VengeflySide.Unknown;
        }

        string name = gameObject.name;
        if (name.StartsWith(LeftVengeflyName, StringComparison.Ordinal))
        {
            return VengeflySide.Left;
        }

        if (name.StartsWith(RightVengeflyName, StringComparison.Ordinal))
        {
            return VengeflySide.Right;
        }

        if (name.IndexOf("Buzzer", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return VengeflySide.Summon;
        }

        return VengeflySide.Unknown;
    }

    private static void RememberVanillaHp(HealthManager hm)
    {
        int instanceId = hm.GetInstanceID();
        if (vanillaHpByInstance.ContainsKey(instanceId))
        {
            return;
        }

        VengeflySide side = GetVengeflySide(hm.gameObject);
        if (side == VengeflySide.Unknown)
        {
            return;
        }

        int hp = hm.hp;

        hp = Math.Max(hp, GetDefaultVanillaHp(side));
        if (hp > 0)
        {
            vanillaHpByInstance[instanceId] = hp;
        }
    }

    private static bool TryGetVanillaHp(HealthManager hm, VengeflySide side, out int hp)
    {
        if (vanillaHpByInstance.TryGetValue(hm.GetInstanceID(), out hp) && hp > 0)
        {
            hp = Math.Max(hp, GetDefaultVanillaHp(side));
            return true;
        }

        hp = hm.hp;

        hp = Math.Max(hp, GetDefaultVanillaHp(side));
        return hp > 0;
    }

    private static int GetDefaultVanillaHp(VengeflySide side)
    {
        return side switch
        {
            VengeflySide.Right => DefaultRightMaxHp,
            VengeflySide.Summon => DefaultSummonMaxHp,
            _ => DefaultLeftMaxHp,
        };
    }

    private static int ClampHp(int value)
    {
        if (value < MinHp)
        {
            return MinHp;
        }

        return value > MaxHp ? MaxHp : value;
    }
}
