using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Satchel;
using Satchel.BetterMenus;
using Satchel.Futils;
using GodhomeQoL.Utils;

namespace GodhomeQoL.Modules.CollectorPhases;

public sealed partial class CollectorPhases : Module
{
    private protected override void Load()
    {
        moduleActive = true;
        global::GodhomeQoL.Modules.BossChallenge.BossManipulateEntryGuard.EnsureHooks();
        NormalizeP5State();
        On.PlayMakerFSM.OnEnable += FsmChanges;
        On.HealthManager.Awake += OnHealthManagerAwake;
        On.HealthManager.Start += OnHealthManagerStart;
        On.HealthManager.Update += OnHealthManagerUpdate;
        USceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        ModHooks.BeforeSceneLoadHook += OnBeforeSceneLoad;
    }

    private protected override void Unload()
    {
        moduleActive = false;
        On.PlayMakerFSM.OnEnable -= FsmChanges;
        On.HealthManager.Awake -= OnHealthManagerAwake;
        On.HealthManager.Start -= OnHealthManagerStart;
        On.HealthManager.Update -= OnHealthManagerUpdate;
        USceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
        ModHooks.BeforeSceneLoadHook -= OnBeforeSceneLoad;
        hoGEntryAllowed = false;
    }

    private static bool ShouldApplySettings(GameObject? go)
    {
        if (go == null)
        {
            return false;
        }

        return hoGEntryAllowed && IsCollectorScene(go.scene.name);
    }

    private static void SceneManager_activeSceneChanged(Scene from, Scene to)
    {
        UpdateHoGEntryAllowed(from.name, to.name);
    }

    private static string OnBeforeSceneLoad(string newSceneName)
    {
        UpdateHoGEntryAllowed(USceneManager.GetActiveScene().name, newSceneName);
        return newSceneName;
    }

    private static void UpdateHoGEntryAllowed(string currentScene, string nextScene)
    {
        if (IsCollectorScene(nextScene))
        {
            if (global::GodhomeQoL.Modules.BossChallenge.BossManipulateEntryGuard.IsAllowedBossEntry(currentScene, nextScene))
            {
                hoGEntryAllowed = true;
            }
            else if (IsCollectorScene(currentScene) && hoGEntryAllowed)
            {
                hoGEntryAllowed = true;
            }
            else
            {
                hoGEntryAllowed = false;
            }

            return;
        }

        hoGEntryAllowed = false;
    }

    private static void FsmChanges(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
    {
        if (!ShouldApplySettings(self.gameObject))
        {
            orig(self);
            return;
        }
        try
        {
            if (self.gameObject.name == "Jar Collector" && self.FsmName == "Phase Control")
            {
                HandlePhaseControl(self);
            }
            else if (self.gameObject.name == "Jar Collector" && self.FsmName == "Control")
            {
                HandleControl(self);
                SetCollectorSummonHp(self);
                FilterCollectorSummonPool(self);
                ApplySummonCounts(self);
            }
            else
            {
                TryGateSpawnerFSM(self.gameObject);
            }
        }
        catch (Exception ex)
        {
            LogSuppressed(ex, "CollectorPhases.Hooks.cs");
        }

        orig(self);
    }

    private static void OnHealthManagerAwake(On.HealthManager.orig_Awake orig, HealthManager self)
    {
        orig(self);

        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }
        if (IsCollector(self))
        {
            ApplyCollectorHealth(self.gameObject, self);
            return;
        }

        TryGateSpawnerFSM(self.gameObject);

        if (!IsCollectorScene(self.gameObject.scene.name))
        {
            return;
        }

        string name = self.gameObject.name;

        if (!spawnBuzzer && IsBuzzerName(name))
        {
            UObject.Destroy(self.gameObject);
            return;
        }

        if (!spawnRoller && IsRollerName(name))
        {
            UObject.Destroy(self.gameObject);
            return;
        }

        if (!spawnSpitter && IsSpitterName(name))
        {
            UObject.Destroy(self.gameObject);
            return;
        }
    }

    private static void OnHealthManagerStart(On.HealthManager.orig_Start orig, HealthManager self)
    {
        orig(self);

        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }
        if (IsCollector(self))
        {
            ApplyCollectorHealth(self.gameObject, self);
            _ = self.StartCoroutine(DeferredApply(self));
            return;
        }

        if (IsCollectorScene(self.gameObject.scene.name))
        {
            string name = self.gameObject.name;

            if (!spawnBuzzer && IsBuzzerName(name))
            {
                UObject.Destroy(self.gameObject);
                return;
            }

            if (!spawnRoller && IsRollerName(name))
            {
                UObject.Destroy(self.gameObject);
                return;
            }

            if (!spawnSpitter && IsSpitterName(name))
            {
                UObject.Destroy(self.gameObject);
                return;
            }
        }
    }

    private static void OnHealthManagerUpdate(On.HealthManager.orig_Update orig, HealthManager self)
    {
        orig(self);

        if (!ShouldApplySettings(self.gameObject))
        {
            return;
        }
        if (!CollectorImmortal || !IsCollector(self))
        {
            return;
        }

        if (self.hp < 100)
        {
            ApplyCollectorHealth(self.gameObject, self);
        }
    }

    private static void HandleControl(PlayMakerFSM fsm)
    {
        if (!ShouldApplySettings(fsm.gameObject))
        {
            return;
        }
        ApplyCollectorHealth(fsm.gameObject);
        fsm.AddCustomAction("Init", () => ApplyCollectorHealth(fsm.gameObject));
        GateSpawnStates(fsm);

        IntCompare? compare = GetFirstActionOfType<IntCompare>(fsm, "Resummon?");
        if (compare != null)
        {
            compare.integer2.Value = IgnoreInitialJarLimit ? 0 : 3;
        }

        if (CollectorImmortal)
        {
            FsmCompat.RemoveGlobalTransition(fsm, "ZERO HP");
        }
    }

    private static IEnumerator DeferredApply(HealthManager hm)
    {
        yield return null;
        ApplyCollectorHealth(hm.gameObject, hm);

        yield return new WaitForSeconds(0.01f);
        ApplyCollectorHealth(hm.gameObject, hm);
    }
}
