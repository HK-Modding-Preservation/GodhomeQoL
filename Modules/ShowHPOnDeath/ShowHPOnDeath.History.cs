namespace GodhomeQoL.Modules.Tools;

public sealed partial class ShowHPOnDeath : Module
{
    private static int historyRecordedGeneration = -1;
    private static int historyCheatGeneration = -1;
    private bool historyHooksInstalled;

    internal static void MarkCheatKill() => historyCheatGeneration = trackingGeneration;

    private void InstallHistoryHooks()
    {
        if (historyHooksInstalled)
        {
            return;
        }

        On.GameManager.ClearSaveFile += OnClearSaveFile;
        On.GameManager.StartNewGame += OnStartNewGame;
        historyHooksInstalled = true;
    }

    private void RemoveHistoryHooks()
    {
        if (!historyHooksInstalled)
        {
            return;
        }

        On.GameManager.ClearSaveFile -= OnClearSaveFile;
        On.GameManager.StartNewGame -= OnStartNewGame;
        historyHooksInstalled = false;
    }

    private static void OnClearSaveFile(On.GameManager.orig_ClearSaveFile orig, GameManager self, int saveSlot, Action<bool> callback)
    {
        orig(self, saveSlot, callback);
        BossHistory.DeleteSlot(saveSlot);
    }

    private static void OnStartNewGame(On.GameManager.orig_StartNewGame orig, GameManager self, bool permadeathMode, bool bossRushMode)
    {
        if (self.profileID > 0)
        {
            BossHistory.DeleteSlot(self.profileID);
        }

        orig(self, permadeathMode, bossRushMode);
    }

    private static IEnumerator OnHeroDie(On.HeroController.orig_Die orig, HeroController self)
    {
        if (self != null && !self.cState.dead)
        {
            RecordHistory(victory: false);
        }

        return orig(self);
    }

    private static void OnEndBossScene(On.BossSceneController.orig_EndBossScene orig, BossSceneController self)
    {
        HeroController? hero = HeroController.instance;
        if (self != null
            && hero != null
            && !hero.cState.dead
            && string.Equals(self.gameObject.scene.name, trackedScene, StringComparison.Ordinal))
        {
            RecordHistory(victory: true);
        }

        orig(self);
    }

    private static void RecordHistory(bool victory)
    {
        try
        {
            if (!Settings.EnabledMod
                || !shouldTrackBosses
                || historyRecordedGeneration == trackingGeneration
                || !BossHistory.TryGetBossForScene(trackedScene, out string bossKey))
            {
                return;
            }

            bool inPantheon = BossSequenceController.IsInSequence;
            if (!inPantheon && !enteredFromWorkshop)
            {
                return;
            }

            if (victory && (inPantheon || historyCheatGeneration == trackingGeneration || IsCombatCheatActive()))
            {
                return;
            }

            BossHistoryEntry entry = new()
            {
                Time = DateTime.Now,
                Victory = victory,
                Parts = CaptureHistoryParts(victory)
            };

            if (!victory && entry.Parts.Count == 0)
            {
                return;
            }

            if (inPantheon)
            {
                entry.Pantheon = GetPantheonTier();
                entry.PantheonBoss = BossSequenceController.BossIndex + 1;
            }
            else
            {
                entry.Level = BossSceneController.Instance != null ? BossSceneController.Instance.BossLevel : -1;
            }

            historyRecordedGeneration = trackingGeneration;
            BossHistory.Add(bossKey, entry);
            LogDebug($"Boss history: {bossKey} {(victory ? "victory" : $"{entry.TotalHp}/{entry.TotalMax}")}");
        }
        catch (Exception ex)
        {
            LogError($"Boss history record failed: {ex}");
        }
    }

    private static readonly Dictionary<string, (string Phase, string Name)> phaseLabelByObject = new(StringComparer.Ordinal)
    {
        { "GG_Mantis_Lords|Mantis Lord", ("Phase 1", "") },
        { "GG_Mantis_Lords|Mantis Lord S1", ("Phase 2", "Lord 1") },
        { "GG_Mantis_Lords|Mantis Lord S2", ("Phase 2", "Lord 2") },
        { "GG_Mantis_Lords_V|Mantis Lord", ("Phase 1", "") },
        { "GG_Mantis_Lords_V|Mantis Lord S1", ("Phase 2", "Sister 1") },
        { "GG_Mantis_Lords_V|Mantis Lord S2", ("Phase 2", "Sister 2") },
        { "GG_Mantis_Lords_V|Mantis Lord S3", ("Phase 2", "Sister 3") },
        { "GG_Soul_Master|Mage Lord", ("Phase 1", "") },
        { "GG_Soul_Master|Mage Lord Phase2", ("Phase 2", "") },
        { "GG_Soul_Tyrant|Dream Mage Lord", ("Phase 1", "") },
        { "GG_Soul_Tyrant|Dream Mage Lord Phase2", ("Phase 2", "") }
    };

    private static readonly FieldInfo? hpScaleField = typeof(HealthManager).GetField("hpScale", BindingFlags.Instance | BindingFlags.NonPublic);

    private static bool IsPrimaryHealthManager(HealthManager hm) => hm.gameObject.GetComponent<HealthManager>() == hm;

    private static List<BossHistoryPart> CaptureHistoryParts(bool victory)
    {
        List<BossHistoryPart> parts = new();
        bool suppressCollector = IsCollectorImmortalTrackingSuppressed();
        bool suppressZote = IsZoteImmortalTrackingSuppressed();
        foreach (string bossKey in trackedBossOrder)
        {
            if (!trackedBossesByKey.TryGetValue(bossKey, out BossState? state))
            {
                continue;
            }

            if ((suppressCollector && string.Equals(state.DisplayName, "The Collector", StringComparison.Ordinal))
                || (suppressZote && string.Equals(state.DisplayName, "Grey Prince Zote", StringComparison.Ordinal)))
            {
                continue;
            }

            int hp = Math.Max(state.CurrentHP, 0);
            int max = Math.Max(initialHPs.TryGetValue(bossKey, out int initialHp) ? initialHp : 0, hp);
            if (max <= 0)
            {
                continue;
            }

            if (!victory && phaseBosses.Contains(state.DisplayName) && phaseTrackers.TryGetValue(state.DisplayName, out PhaseTracker? tracker))
            {
                parts.AddRange(GetPhaseParts(state.DisplayName, tracker, max));
                continue;
            }

            bool phased = TryGetPhaseLabel(bossKey, out string phase, out string phaseName);
            parts.Add(new BossHistoryPart
            {
                Name = phased ? phaseName : state.DisplayName,
                Phase = phased ? phase : null,
                Hp = victory ? 0 : hp,
                Max = max
            });
        }

        if (!victory)
        {
            parts.AddRange(CapturePendingPhaseParts());
        }

        return parts;
    }

    private static bool TryGetPhaseLabel(string bossKey, out string phase, out string name)
    {
        phase = string.Empty;
        name = string.Empty;
        int sceneEnd = bossKey.IndexOf('|');
        int nameStart = bossKey.LastIndexOf('|');
        int nameEnd = bossKey.LastIndexOf('#');
        if (sceneEnd <= 0 || nameStart <= sceneEnd || nameEnd <= nameStart)
        {
            return false;
        }

        string objectKey = bossKey.Substring(0, sceneEnd) + "|" + bossKey.Substring(nameStart + 1, nameEnd - nameStart - 1);
        if (!phaseLabelByObject.TryGetValue(objectKey, out (string Phase, string Name) label))
        {
            return false;
        }

        phase = label.Phase;
        name = label.Name;
        return true;
    }

    private static string GetDisplayLabel(string bossKey, string fallback)
    {
        if (!TryGetPhaseLabel(bossKey, out string phase, out string name))
        {
            return fallback;
        }

        return string.IsNullOrEmpty(name) ? phase : $"{phase} - {name}";
    }

    private static IEnumerable<BossHistoryPart> CapturePendingPhaseParts()
    {
        Scene scene = default;
        for (int i = 0; i < USceneManager.sceneCount; i++)
        {
            Scene candidate = USceneManager.GetSceneAt(i);
            if (candidate.IsValid()
                && candidate.isLoaded
                && string.Equals(candidate.name, trackedScene, StringComparison.Ordinal)
                && (trackedSceneHandle < 0 || candidate.handle == trackedSceneHandle))
            {
                scene = candidate;
                break;
            }
        }

        if (!scene.IsValid())
        {
            yield break;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (HealthManager hm in root.GetComponentsInChildren<HealthManager>(true))
            {
                if (hm == null
                    || hm.gameObject.activeInHierarchy
                    || hm.isDead
                    || bossKeyByInstanceId.ContainsKey(hm.GetInstanceID())
                    || !phaseLabelByObject.TryGetValue($"{trackedScene}|{hm.gameObject.name}", out (string Phase, string Name) label)
                    || !IsPrimaryHealthManager(hm))
                {
                    continue;
                }

                int hp = EstimateStartHp(hm);
                if (hp > 0)
                {
                    yield return new BossHistoryPart { Name = label.Name, Phase = label.Phase, Hp = hp, Max = hp };
                }
            }
        }
    }

    private static int EstimateStartHp(HealthManager hm)
    {
        int hp = hm.hp;
        try
        {
            object? scale = hpScaleField?.GetValue(hm);
            BossSceneController? controller = BossSceneController.Instance;
            if (scale == null || controller == null)
            {
                return hp;
            }

            string levelName = controller.BossLevel switch
            {
                0 => "level1",
                1 => "level2",
                _ => "level3"
            };
            int scaled = scale.GetType().GetField(levelName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(scale) is int value ? value : 0;
            return scaled > 0 ? scaled : hp;
        }
        catch (Exception ex)
        {
            LogDebug($"Boss history: HP estimate failed for {hm.gameObject.name} ({ex.Message})");
            return hp;
        }
    }

    private void OnHealthManagerDie(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion)
    {
        orig(self, attackDirection, attackType, ignoreEvasion);
        if (self != null
            && shouldTrackBosses
            && bossKeyByInstanceId.TryGetValue(self.GetInstanceID(), out string bossKey)
            && trackedBossesByKey.TryGetValue(bossKey, out BossState? state)
            && !phaseBosses.Contains(state.DisplayName))
        {
            UpdateTrackedBossHp(bossKey, self.isDead ? 0 : self.hp);
        }
    }

    private static int GetPantheonTier()
    {
        try
        {
            string door = StaticVariableList.GetValue<string>("currentBossDoorPD") ?? string.Empty;
            const string prefix = "bossDoorStateTier";
            if (door.StartsWith(prefix, StringComparison.Ordinal) && int.TryParse(door.Substring(prefix.Length), out int tier))
            {
                return tier;
            }
        }
        catch (Exception ex)
        {
            LogDebug($"Boss history: pantheon tier unknown ({ex.Message})");
        }

        return 0;
    }

    private static bool IsCombatCheatActive()
    {
        return ModuleManager.IsModuleLoaded<global::GodhomeQoL.Modules.Cheats.Cheats>()
            && (global::GodhomeQoL.Modules.Cheats.Cheats.GetInfiniteHpEnabled()
                || global::GodhomeQoL.Modules.Cheats.Cheats.GetInvincibilityEnabled()
                || global::GodhomeQoL.Modules.Cheats.Cheats.GetNoclipEnabled());
    }
}
