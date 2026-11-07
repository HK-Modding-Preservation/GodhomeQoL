using InControl;
using Satchel.BetterMenus;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class ShowHPOnDeath : Module
{
    private void OnHealthManagerEnable(On.HealthManager.orig_OnEnable orig, HealthManager self)
    {
        orig(self);

        if (!Settings.EnabledMod || !shouldTrackBosses)
        {
            return;
        }

        if (BossNames.Map.TryGetValue(self.gameObject.name, out string displayName)
            && IsPrimaryHealthManager(self)
            && TryEnsureTrackedBossKey(self, displayName, out string bossKey))
        {
            UpdateTrackedBossHp(bossKey, self.hp);
            if (!initialHPs.ContainsKey(bossKey))
            {
                int generation = trackingGeneration;
                string sceneName = trackedScene;
                _ = GlobalCoroutineExecutor.Start(CaptureBossStateAfterDelay(self, bossKey, sceneName, generation));
            }
        }
    }

    private static IEnumerator CaptureBossStateAfterDelay(
        HealthManager source,
        string bossKey,
        string sceneName,
        int generation)
    {
        yield return new WaitForSecondsRealtime(1f);

        if (generation != trackingGeneration || !Settings.EnabledMod || !shouldTrackBosses)
        {
            yield break;
        }

        if (source == null || source.gameObject == null)
        {
            yield break;
        }

        if (!string.Equals(source.gameObject.scene.name, sceneName, StringComparison.Ordinal))
        {
            yield break;
        }

        HealthManager? hm = source.gameObject.GetComponent<HealthManager>();
        if (hm == null)
        {
            yield break;
        }

        if (!trackedBossesByKey.TryGetValue(bossKey, out BossState? state))
        {
            yield break;
        }

        int hp = Math.Max(hm.hp, 0);
        if (!initialHPs.ContainsKey(bossKey) && hp > 0)
        {
            initialHPs[bossKey] = hp;
        }

        state.CurrentHP = hp;
    }

    private static bool TryEnsureTrackedBossKey(HealthManager source, string displayName, out string bossKey)
    {
        bossKey = "";
        if (source == null || source.gameObject == null)
        {
            return false;
        }

        string sourceScene = source.gameObject.scene.name;
        if (!string.Equals(sourceScene, trackedScene, StringComparison.Ordinal))
        {
            return false;
        }

        int sourceSceneHandle = source.gameObject.scene.handle;
        if (trackedSceneHandle < 0)
        {
            Scene activeScene = USceneManager.GetActiveScene();
            if (activeScene.IsValid()
                && activeScene.isLoaded
                && string.Equals(activeScene.name, trackedScene, StringComparison.Ordinal))
            {
                trackedSceneHandle = activeScene.handle;
            }
            else
            {
                trackedSceneHandle = sourceSceneHandle;
            }
        }

        if (sourceSceneHandle != trackedSceneHandle)
        {
            return false;
        }

        int instanceId = source.GetInstanceID();
        if (bossKeyByInstanceId.TryGetValue(instanceId, out bossKey))
        {
            if (trackedBossesByKey.TryGetValue(bossKey, out BossState? existingState))
            {
                existingState.DisplayName = displayName;
            }
            return true;
        }

        string signature = $"{sourceScene}|{displayName}|{source.gameObject.name}";
        int slot = bossSignatureCounts.TryGetValue(signature, out int count) ? count + 1 : 1;
        bossSignatureCounts[signature] = slot;

        bossKey = $"{signature}#{slot}";
        bossKeyByInstanceId[instanceId] = bossKey;

        if (!trackedBossesByKey.ContainsKey(bossKey))
        {
            trackedBossesByKey[bossKey] = new BossState
            {
                DisplayName = displayName,
                CurrentHP = Math.Max(source.hp, 0)
            };
            trackedBossOrder.Add(bossKey);
        }

        return true;
    }

    private static void UpdateTrackedBossHp(string bossKey, int hp)
    {
        if (trackedBossesByKey.TryGetValue(bossKey, out BossState? state))
        {
            state.CurrentHP = Math.Max(hp, 0);
        }
    }

    private static bool IsCollectorImmortalTrackingSuppressed(GameObject? target = null)
    {
        return global::GodhomeQoL.Modules.CollectorPhases.CollectorPhases.IsCollectorImmortalityActiveFor(target);
    }

    private static bool IsZoteImmortalTrackingSuppressed(GameObject? target = null)
    {
        return global::GodhomeQoL.Modules.BossChallenge.ZoteHelper.IsGreyPrinceImmortalityActiveFor(target);
    }

    private static void RemoveTrackedBossEntriesByName(string displayName)
    {
        List<string> keysToRemove = trackedBossesByKey
            .Where(kvp => string.Equals(kvp.Value.DisplayName, displayName, StringComparison.Ordinal))
            .Select(kvp => kvp.Key)
            .ToList();

        if (keysToRemove.Count == 0)
        {
            return;
        }

        HashSet<string> keySet = [.. keysToRemove];
        for (int i = trackedBossOrder.Count - 1; i >= 0; i--)
        {
            if (keySet.Contains(trackedBossOrder[i]))
            {
                trackedBossOrder.RemoveAt(i);
            }
        }

        foreach (string key in keysToRemove)
        {
            trackedBossesByKey.Remove(key);
            initialHPs.Remove(key);
            personalBestByBoss.Remove(key);

            int hashIndex = key.LastIndexOf('#');
            if (hashIndex > 0)
            {
                bossSignatureCounts.Remove(key[..hashIndex]);
            }
        }

        foreach ((int instanceId, string key) in bossKeyByInstanceId.ToArray())
        {
            if (keySet.Contains(key))
            {
                bossKeyByInstanceId.Remove(instanceId);
            }
        }
    }

    private void OnHealthManagerUpdate(On.HealthManager.orig_Update orig, HealthManager self)
    {
        orig(self);
        if (!Settings.EnabledMod || !shouldTrackBosses)
        {
            return;
        }

        if (self.gameObject.name == "Jar Collector" && IsCollectorImmortalTrackingSuppressed(self.gameObject))
        {
            if (BossNames.Map.TryGetValue(self.gameObject.name, out string collectorName))
            {
                RemoveTrackedBossEntriesByName(collectorName);
            }

            return;
        }

        if (self.gameObject.name == "Grey Prince" && IsZoteImmortalTrackingSuppressed(self.gameObject))
        {
            if (BossNames.Map.TryGetValue(self.gameObject.name, out string zoteName))
            {
                RemoveTrackedBossEntriesByName(zoteName);
            }

            return;
        }

        if (!BossNames.Map.TryGetValue(self.gameObject.name, out string displayName) || !IsPrimaryHealthManager(self))
        {
            return;
        }

        if (TryEnsureTrackedBossKey(self, displayName, out string bossKey))
        {
            int hp = Math.Max(self.hp, 0);
            UpdateTrackedBossHp(bossKey, hp);
            if (!initialHPs.ContainsKey(bossKey) && hp > 0)
            {
                initialHPs[bossKey] = hp;
            }
        }

        if (phaseBosses.Contains(displayName))
        {
            UpdatePhaseTracker(displayName, self.hp);
        }
    }
}
