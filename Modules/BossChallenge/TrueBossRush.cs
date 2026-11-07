using System.Collections.Generic;
using System.Linq;
using Satchel.Futils;
using Vasi;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed class TrueBossRush : Module
{
    internal static TrueBossRush? Instance;

    [GlobalSetting] public static bool TrueBossRushPantheon1Enabled = false;
    [GlobalSetting] public static bool TrueBossRushPantheon2Enabled = false;
    [GlobalSetting] public static bool TrueBossRushPantheon3Enabled = false;
    [GlobalSetting] public static bool TrueBossRushPantheon4Enabled = false;
    [GlobalSetting] public static bool TrueBossRushPantheon5Enabled = false;

    internal static bool AnyPantheonEnabled =>
        TrueBossRushPantheon1Enabled
        || TrueBossRushPantheon2Enabled
        || TrueBossRushPantheon3Enabled
        || TrueBossRushPantheon4Enabled
        || TrueBossRushPantheon5Enabled;

    public override bool DefaultEnabled => false;
    public override bool Hidden => true;

    private static readonly Dictionary<BossSequence, BossScene[]> OriginalSequences = new();
    private static readonly Dictionary<BossSequence, List<BossSequenceDoor>> SequenceDoors = new();
    private static readonly FieldInfo BossScenesField = typeof(BossSequence).GetField("bossScenes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
    private const int FirstSceneSyncRetryFrames = 120;
    private static readonly Dictionary<int, int> DoorFirstSceneSyncGenerations = new();

    private static readonly HashSet<string> BenchScenes = new(StringComparer.OrdinalIgnoreCase)
    {
        "GG_Spa"
    };

    private static readonly HashSet<string> TransitionScenes = new(StringComparer.OrdinalIgnoreCase)
    {
        "GG_Engine",
        "GG_Engine_Prime",
        "GG_Engine_Root",
        "GG_Unn",
        "GG_Wyrm"
    };

    private static readonly HashSet<string> VanishedHud = new(StringComparer.OrdinalIgnoreCase)
    {
        "GG_Hollow_Knight",
        "GG_Radiance"
    };

    private enum Pantheon
    {
        Unknown,
        P1,
        P2,
        P3,
        P4,
        P5
    }

    private protected override void Load()
    {
        ClearSequenceCaches();
        if (AnyPantheonEnabled)
        {
            _ = PantheonSequenceCompatibility.DisableRandomPantheons();
            _ = PantheonSequenceCompatibility.DisableSegmentedP5();
        }

        Instance = this;
        On.BossSequenceDoor.Start += CacheSequenceDoor;
        On.BossSequenceController.SetupNewSequence += ApplySequenceToStart;
        USceneManager.activeSceneChanged += OnSceneChange;
        CacheLiveSequenceDoors();
        if (AnyPantheonEnabled)
        {
            RefreshAllPantheons();
        }
    }

    private protected override void Unload()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        On.BossSequenceDoor.Start -= CacheSequenceDoor;
        On.BossSequenceController.SetupNewSequence -= ApplySequenceToStart;
        USceneManager.activeSceneChanged -= OnSceneChange;
        RestoreAllKnownSequencesOnDisable();
        ClearSequenceCaches();
    }

    private static void ClearSequenceCaches()
    {
        OriginalSequences.Clear();
        SequenceDoors.Clear();
    }

    private static void PruneSequenceCaches()
    {
        foreach (BossSequence sequence in OriginalSequences.Keys.ToArray())
        {
            if (sequence == null)
            {
                if (sequence is not null)
                {
                    _ = OriginalSequences.Remove(sequence);
                }
            }
        }

        foreach ((BossSequence sequence, List<BossSequenceDoor> doors) in SequenceDoors.ToArray())
        {
            if (sequence == null)
            {
                if (sequence is not null)
                {
                    _ = SequenceDoors.Remove(sequence);
                }
                continue;
            }

            doors.RemoveAll(door => door == null);
            if (doors.Count == 0)
            {
                _ = SequenceDoors.Remove(sequence);
            }
        }
    }

    private static void RestoreCachedSequences()
    {
        PruneSequenceCaches();

        foreach ((BossSequence sequence, BossScene[] original) in OriginalSequences.ToArray())
        {
            if (sequence == null || original == null || original.Length == 0)
            {
                continue;
            }

            try
            {
                if (SetBossScenes(sequence, original.ToArray()))
                {
                    SyncFirstScene(sequence);
                }
            }
            catch (Exception swallowed)
            {
                LogSuppressed(swallowed, "TrueBossRush.cs");
            }
        }
    }

    private static void RestoreAllKnownSequencesOnDisable()
    {
        foreach (BossSequence sequence in EnumerateKnownSequences().ToArray())
        {
            TryRestoreDisabledSequence(sequence);
        }

        RestoreCachedSequences();
    }

    private void OnSceneChange(Scene prev, Scene next)
    {
        GameManager? manager = GameManager.instance;
        if (manager == null || !VanishedHud.Contains(prev.name))
        {
            return;
        }

        manager.StartCoroutine(EnsureHudVisible());
    }

    private static IEnumerator EnsureHudVisible()
    {
        yield return null;

        if (GameCameras.instance != null)
        {
            GameCameras.instance.hudCanvas.LocateMyFSM("Slide Out").SendEvent("IN");
        }
    }

    private static void CacheSequenceDoor(On.BossSequenceDoor.orig_Start orig, BossSequenceDoor self)
    {
        orig(self);

        CacheSequenceDoorEntry(self);
        if (Instance == null || self.bossSequence == null || PantheonSequenceCompatibility.ShouldSkipTrueBossRush() || !AnyPantheonEnabled)
        {
            return;
        }

        try
        {
            Instance.ApplySequence(self.bossSequence);
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "TrueBossRush.cs");
        }
    }

    private static void CacheLiveSequenceDoors()
    {
        try
        {
            foreach (BossSequenceDoor door in UObject.FindObjectsOfType<BossSequenceDoor>())
            {
                CacheSequenceDoorEntry(door);
            }
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "TrueBossRush.cs");
        }
    }

    private static void CacheSequenceDoorEntry(BossSequenceDoor? door)
    {
        if (door == null || door.bossSequence == null)
        {
            return;
        }

        if (!SequenceDoors.TryGetValue(door.bossSequence, out List<BossSequenceDoor>? doors))
        {
            doors = new List<BossSequenceDoor>();
            SequenceDoors[door.bossSequence] = doors;
        }

        doors.RemoveAll(cachedDoor => cachedDoor == null);
        if (!doors.Contains(door))
        {
            doors.Add(door);
        }
    }

    private static void RefreshAllPantheons()
    {
        for (int i = 1; i <= 5; i++)
        {
            RefreshPantheon(i);
        }
    }

    internal static void ForceRestoreNow()
    {
        RestoreAllKnownSequencesOnDisable();
    }

    private static IEnumerable<BossSequence> EnumerateKnownSequences()
    {
        PruneSequenceCaches();
        CacheLiveSequenceDoors();

        HashSet<BossSequence> knownSequences = new();
        foreach (BossSequence sequence in SequenceDoors.Keys)
        {
            if (sequence != null)
            {
                _ = knownSequences.Add(sequence);
            }
        }

        foreach (BossSequence sequence in OriginalSequences.Keys)
        {
            if (sequence != null)
            {
                _ = knownSequences.Add(sequence);
            }
        }

        try
        {
            foreach (BossSequence sequence in Resources.FindObjectsOfTypeAll<BossSequence>())
            {
                if (sequence != null)
                {
                    _ = knownSequences.Add(sequence);
                }
            }
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "TrueBossRush.cs");
        }

        return knownSequences;
    }

    private static void TryRestoreDisabledSequence(BossSequence? sequence)
    {
        if (sequence == null)
        {
            return;
        }

        try
        {
            BossScene[]? bossScenes = GetBossScenes(sequence);
            if (bossScenes == null || bossScenes.Length <= 1)
            {
                return;
            }

            List<BossScene> scenes = bossScenes.ToList();
            Pantheon pantheon = GetPantheon(scenes);
            if (!IsPantheonEnabled(pantheon)
                && TryRestoreOriginalSequence(sequence, out BossScene[] restored)
                && SetBossScenes(sequence, restored))
            {
                SyncFirstScene(sequence);
            }
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "TrueBossRush.cs");
        }
    }

    private void ApplySequenceToStart(
        On.BossSequenceController.orig_SetupNewSequence orig,
        BossSequence sequence,
        BossSequenceController.ChallengeBindings bindings,
        string playerData
    )
    {
        if (!PantheonSequenceCompatibility.ShouldSkipTrueBossRush() && AnyPantheonEnabled)
        {
            try
            {
                ApplySequence(sequence);
            }
            catch (Exception ex)
            {
                LogError($"TrueBossRush failed: {ex}");
            }
        }

        orig(sequence, bindings, playerData);
    }

    private void ApplySequence(BossSequence? sequence)
    {
        if (sequence == null)
        {
            return;
        }

        BossScene[]? bossScenes = GetBossScenes(sequence);
        if (bossScenes == null || bossScenes.Length <= 1)
        {
            return;
        }

        List<BossScene> scenes = bossScenes.ToList();
        Pantheon pantheon = GetPantheon(scenes);
        CacheOriginalSequence(sequence, bossScenes);

        if (!IsPantheonEnabled(pantheon))
        {
            if (TryRestoreOriginalSequence(sequence, out BossScene[] restored) && SetBossScenes(sequence, restored))
            {
                SyncFirstScene(sequence);
            }

            return;
        }

        if (!TryBuildSequence(scenes, out List<BossScene>? rebuilt))
        {
            return;
        }

        if (SetBossScenes(sequence, rebuilt.ToArray()))
        {
            SyncFirstScene(sequence);
        }
    }

    private static bool TryBuildSequence(List<BossScene> scenes, out List<BossScene> result)
    {
        result = scenes
            .Where(scene => !IsTransitionOrBench(scene.sceneName))
            .ToList();

        return result.Count > 0;
    }

    internal static void RefreshPantheon(int pantheonNumber)
    {
        if (Instance == null)
        {
            return;
        }

        Pantheon pantheon = pantheonNumber switch
        {
            1 => Pantheon.P1,
            2 => Pantheon.P2,
            3 => Pantheon.P3,
            4 => Pantheon.P4,
            5 => Pantheon.P5,
            _ => Pantheon.Unknown
        };

        if (pantheon == Pantheon.Unknown)
        {
            return;
        }

        BossSequence[] sequences = EnumerateKnownSequences().ToArray();
        foreach (BossSequence sequence in sequences)
        {
            try
            {
                BossScene[]? bossScenes = GetBossScenes(sequence);
                if (bossScenes == null || bossScenes.Length <= 1)
                {
                    continue;
                }

                List<BossScene> scenes = bossScenes.ToList();
                if (GetPantheon(scenes) != pantheon)
                {
                    continue;
                }

                Instance.ApplySequence(sequence);
            }
            catch (Exception swallowed)
            {
                LogSuppressed(swallowed, "TrueBossRush.cs");
            }
        }
    }

    private static void CacheOriginalSequence(BossSequence sequence, BossScene[] scenes)
    {
        if (!OriginalSequences.ContainsKey(sequence))
        {
            OriginalSequences[sequence] = scenes.ToArray();
        }
    }

    private static bool TryRestoreOriginalSequence(BossSequence sequence, out BossScene[] restored)
    {
        if (!OriginalSequences.TryGetValue(sequence, out BossScene[]? original))
        {
            return TryRestoreVanillaSequence(sequence, out restored);
        }

        restored = original.ToArray();
        return true;
    }

    private static bool TryRestoreVanillaSequence(BossSequence sequence, out BossScene[] restored)
    {
        restored = Array.Empty<BossScene>();
        BossScene[]? current = GetBossScenes(sequence);
        if (current == null || current.Length == 0)
        {
            return false;
        }

        int pantheonNumber = GetPantheon(current) switch
        {
            Pantheon.P1 => 1,
            Pantheon.P2 => 2,
            Pantheon.P3 => 3,
            Pantheon.P4 => 4,
            Pantheon.P5 => 5,
            _ => 0
        };

        if (pantheonNumber == 0
            || !RandomPantheons.TryBuildVanillaSequence(current, pantheonNumber, out BossScene[] vanilla)
            || !HasSameBosses(current, vanilla)
            || current.Select(scene => scene?.sceneName).SequenceEqual(vanilla.Select(scene => scene?.sceneName), StringComparer.Ordinal))
        {
            return false;
        }

        LogWarn($"TrueBossRush: original order of pantheon {pantheonNumber} was not cached, restored the default order");
        restored = vanilla;
        return true;
    }

    private static bool HasSameBosses(IEnumerable<BossScene> left, IEnumerable<BossScene> right)
    {
        static List<string> Bosses(IEnumerable<BossScene> scenes) => scenes
            .Where(scene => scene != null && !string.IsNullOrEmpty(scene.sceneName) && !IsTransitionOrBench(scene.sceneName))
            .Select(scene => scene.sceneName.ToUpperInvariant())
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        return Bosses(left).SequenceEqual(Bosses(right), StringComparer.Ordinal);
    }

    private static BossScene[]? GetBossScenes(BossSequence sequence)
    {
        if (sequence == null)
        {
            return null;
        }

        try
        {
            return BossScenesField.GetValue(sequence) as BossScene[];
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "TrueBossRush.cs");
            return null;
        }
    }

    private static bool SetBossScenes(BossSequence sequence, BossScene[] scenes)
    {
        if (sequence == null || scenes == null)
        {
            return false;
        }

        try
        {
            BossScenesField.SetValue(sequence, scenes);
            return true;
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "TrueBossRush.cs");
            return false;
        }
    }

    private static Pantheon GetPantheon(IReadOnlyList<BossScene> scenes)
    {
        HashSet<string> names = new(scenes.Select(scene => scene.sceneName), StringComparer.OrdinalIgnoreCase);

        if (names.Contains("GG_Wyrm")
            || names.Contains("GG_Radiance")
            || names.Contains("GG_Engine_Root")
            || names.Contains("GG_Grimm_Nightmare"))
        {
            return Pantheon.P5;
        }

        if (names.Contains("GG_Engine_Prime") || names.Contains("GG_Hollow_Knight"))
        {
            return Pantheon.P4;
        }

        if (names.Contains("GG_Sly"))
        {
            return Pantheon.P3;
        }

        if (names.Contains("GG_Painter"))
        {
            return Pantheon.P2;
        }

        if (names.Contains("GG_Nailmasters"))
        {
            return Pantheon.P1;
        }

        return Pantheon.Unknown;
    }

    private static bool IsPantheonEnabled(Pantheon pantheon)
    {
        return pantheon switch
        {
            Pantheon.P1 => TrueBossRushPantheon1Enabled,
            Pantheon.P2 => TrueBossRushPantheon2Enabled,
            Pantheon.P3 => TrueBossRushPantheon3Enabled,
            Pantheon.P4 => TrueBossRushPantheon4Enabled,
            Pantheon.P5 => TrueBossRushPantheon5Enabled,
            _ => false
        };
    }

    private static bool IsTransitionOrBench(string sceneName) =>
        BenchScenes.Contains(sceneName) || TransitionScenes.Contains(sceneName);

    private static void SyncFirstScene(BossSequence sequence)
    {
        try
        {
            CacheLiveSequenceDoors();
            if (!TryGetFirstSceneName(sequence, out string firstScene))
            {
                return;
            }
            HashSet<int> syncedDoors = new();

            if (SequenceDoors.TryGetValue(sequence, out List<BossSequenceDoor>? doors))
            {
                doors.RemoveAll(door => door == null);
                foreach (BossSequenceDoor door in doors)
                {
                    if (door == null)
                    {
                        continue;
                    }

                    _ = syncedDoors.Add(door.GetInstanceID());
                    TrySetDoorFirstScene(door, firstScene);
                }
            }

            foreach (BossSequenceDoor door in Resources.FindObjectsOfTypeAll<BossSequenceDoor>())
            {
                if (door == null || door.bossSequence != sequence)
                {
                    continue;
                }

                if (syncedDoors.Contains(door.GetInstanceID()))
                {
                    continue;
                }

                TrySetDoorFirstScene(door, firstScene);
            }
        }
        catch (Exception ex)
        {
            LogDebug($"TrueBossRush: failed to sync first scene - {ex.Message}");
        }
    }

    private static bool TryGetFirstSceneName(BossSequence sequence, out string firstScene)
    {
        firstScene = string.Empty;

        try
        {
            if (sequence == null || sequence.Count <= 0)
            {
                return false;
            }

            string sceneName = sequence.GetSceneAt(0);
            if (string.IsNullOrEmpty(sceneName))
            {
                return false;
            }

            firstScene = sceneName;
            return true;
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "TrueBossRush.cs");
            return false;
        }
    }

    private static void TrySetDoorFirstScene(BossSequenceDoor door, string firstScene)
    {
        int doorId = door.GetInstanceID();
        int generation = DoorFirstSceneSyncGenerations.TryGetValue(doorId, out int current) ? current + 1 : 1;
        DoorFirstSceneSyncGenerations[doorId] = generation;

        try
        {
            PlayMakerFSM? challengeFsm = door.challengeFSM;
            if (challengeFsm != null)
            {
                challengeFsm.GetVariable<FsmString>("To Scene").Value = firstScene;
                return;
            }
        }
        catch (Exception ex)
        {
            LogDebug($"TrueBossRush: failed to set door first scene immediately - {ex.Message}");
        }

        _ = GlobalCoroutineExecutor.Start(RetrySetDoorFirstScene(door, doorId, firstScene, generation));
    }

    private static IEnumerator RetrySetDoorFirstScene(BossSequenceDoor door, int doorId, string firstScene, int generation)
    {
        for (int frame = 0; frame < FirstSceneSyncRetryFrames; frame++)
        {
            if (door == null)
            {
                yield break;
            }

            if (!DoorFirstSceneSyncGenerations.TryGetValue(doorId, out int activeGeneration) || activeGeneration != generation)
            {
                yield break;
            }

            PlayMakerFSM? challengeFsm = door.challengeFSM;
            if (challengeFsm != null)
            {
                try
                {
                    challengeFsm.GetVariable<FsmString>("To Scene").Value = firstScene;
                }
                catch (Exception ex)
                {
                    LogDebug($"TrueBossRush: failed to set door first scene on retry - {ex.Message}");
                }

                yield break;
            }

            yield return null;
        }
    }
}
