using System.Collections;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using Modding;
using Satchel;
using Satchel.Futils;
using UnityEngine;

namespace GodhomeQoL.Modules.BossChallenge;

public abstract partial class ArmoredBossHelper : Module
{
    private protected const string ArmorNamePrefix = "False Knight";
    private protected const string RecoverVariableName = "Recover HP";
    private protected const int MinHp = 1;
    private protected const int MaxHp = 999999;

    internal void RestoreVanillaHealthIfPresentCore()
    {
        foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
        {
            if (hm == null || hm.gameObject == null || !IsArmor(hm))
            {
                continue;
            }

            RestoreVanillaArmorHealth(hm);
            RestoreVanillaRecoverHp(hm.gameObject);
        }

        trackedPhaseByArmorInstance.Clear();
        appliedHeadKillsByArmorInstance.Clear();
    }

    private void TrackHeadHealth(HealthManager headHm)
    {
        int instanceId = headHm.GetInstanceID();
        int hp = Math.Max(headHm.hp, 0);
        if (lastHeadHpByInstance.TryGetValue(instanceId, out int lastHp) && lastHp > 0 && hp <= 0)
        {
            confirmedHeadKillCount = Math.Min(confirmedHeadKillCount + 1, 2);
        }

        lastHeadHpByInstance[instanceId] = hp;
    }

    private void ApplyArmorHealth(HealthManager armorHm, int hp, bool resetCurrentHp)
    {
        if (armorHm == null || armorHm.gameObject == null)
        {
            return;
        }

        RememberVanillaArmorHp(armorHm);
        int targetHp = ClampHp(hp);
        if (resetCurrentHp || armorHm.hp > targetHp)
        {
            armorHm.hp = targetHp;
            ReportBossHealth(armorHm, targetHp);
        }
    }

    private void ApplyRecoverHp(HealthManager armorHm, int hp)
    {
        if (armorHm == null || armorHm.gameObject == null)
        {
            return;
        }

        foreach (PlayMakerFSM fsm in armorHm.gameObject.GetComponents<PlayMakerFSM>())
        {
            if (fsm == null || !IsRecoverFsm(fsm))
            {
                continue;
            }

            RememberVanillaRecoverHp(fsm);
            int targetHp = ClampHp(hp);
            FsmInt? recoverHp = fsm.FsmVariables.GetFsmInt(RecoverVariableName);
            if (recoverHp != null)
            {
                recoverHp.Value = targetHp;
            }

            FsmState? stunState = fsm.Fsm?.GetState("Stun");
            if (stunState?.Actions != null)
            {
                foreach (FsmStateAction action in stunState.Actions)
                {
                    if (action is SetHP setHp && setHp.hp != null)
                    {
                        setHp.hp.Value = targetHp;
                    }
                }
            }

            ReportBossHealth(armorHm, targetHp);
        }
    }

    private static void ReportBossHealth(HealthManager armorHm, int targetHp)
    {
        BossSceneController.ReportHealth(armorHm, targetHp, targetHp);
    }

    private void RememberVanillaArmorHp(HealthManager hm)
    {
        int instanceId = hm.GetInstanceID();
        if (vanillaArmorHpByInstance.ContainsKey(instanceId))
        {
            return;
        }

        int hp = hm.hp;

        if (hp > 0)
        {
            vanillaArmorHpByInstance[instanceId] = hp;
        }
    }

    private void RememberVanillaRecoverHp(PlayMakerFSM fsm)
    {
        int fsmId = fsm.GetInstanceID();
        if (vanillaRecoverHpByFsm.ContainsKey(fsmId))
        {
            return;
        }

        int recoverHp = fsm.FsmVariables.GetFsmInt(RecoverVariableName)?.Value ?? DefaultArmorPhaseHp;
        if (recoverHp <= 0)
        {
            recoverHp = DefaultArmorPhaseHp;
        }

        vanillaRecoverHpByFsm[fsmId] = recoverHp;
    }

    private void RestoreVanillaArmorHealth(HealthManager hm)
    {
        if (!vanillaArmorHpByInstance.TryGetValue(hm.GetInstanceID(), out int vanillaHp) || vanillaHp <= 0)
        {
            return;
        }

        int targetHp = ClampHp(vanillaHp);
        hm.gameObject.manageHealth(targetHp);
        hm.hp = targetHp;
    }

    private void RestoreVanillaRecoverHp(GameObject armorObject)
    {
        if (!hoGEntryAllowed)
        {
            return;
        }

        if (armorObject == null)
        {
            return;
        }

        foreach (PlayMakerFSM fsm in armorObject.GetComponents<PlayMakerFSM>())
        {
            if (fsm == null || !IsRecoverFsm(fsm))
            {
                continue;
            }

            int fsmId = fsm.GetInstanceID();
            if (!vanillaRecoverHpByFsm.TryGetValue(fsmId, out int vanillaRecoverHp) || vanillaRecoverHp <= 0)
            {
                continue;
            }

            FsmInt? recoverHp = fsm.FsmVariables.GetFsmInt(RecoverVariableName);
            if (recoverHp != null)
            {
                recoverHp.Value = vanillaRecoverHp;
            }

            FsmState? stunState = fsm.Fsm?.GetState("Stun");
            if (stunState?.Actions == null)
            {
                continue;
            }

            foreach (FsmStateAction action in stunState.Actions)
            {
                if (action is SetHP setHp && setHp.hp != null)
                {
                    setHp.hp.Value = vanillaRecoverHp;
                }
            }
        }
    }

    private int ClampHp(int value)
    {
        if (value < MinHp)
        {
            return MinHp;
        }

        return value > MaxHp ? MaxHp : value;
    }
}
