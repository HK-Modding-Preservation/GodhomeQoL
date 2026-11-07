using System.Collections.Generic;
using System.Linq;
using Satchel.Futils;
using Vasi;
using Random = System.Random;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed partial class RandomPantheons : Module
{
    private void ApplySequenceToStart(
        On.BossSequenceController.orig_SetupNewSequence orig,
        BossSequence sequence,
        BossSequenceController.ChallengeBindings bindings,
        string playerData
    )
    {
        if (!PantheonSequenceCompatibility.ShouldSkipRandomPantheons())
        {
            if (!AnyPantheonEnabled)
            {
                TryRestoreDisabledSequence(sequence);
            }
            else
            {
                try
                {
                    ApplySequence(sequence);
                }
                catch (Exception ex)
                {
                    LogError($"RandomPantheons failed: {ex}");
                }
            }
        }

        orig(sequence, bindings, playerData);
    }

    private static void RefreshAllPantheons()
    {
        for (int i = 1; i <= 5; i++)
        {
            RefreshPantheon(i);
        }
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
            _ = TryApplyRestoreOrDefault(sequence, pantheon, bossScenes);
            return;
        }

        if (!TryBuildSequence(scenes, pantheon, out List<BossScene>? rebuilt))
        {
            return;
        }

        if (SetBossScenes(sequence, rebuilt.ToArray()))
        {
            SyncFirstScene(sequence);
        }
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

        CacheLiveSequenceDoors();
        bool anyEnabled = AnyPantheonEnabled;
        BossSequence[] sequences = EnumerateKnownSequences().ToArray();
        foreach (BossSequence sequence in sequences)
        {
            if (!anyEnabled)
            {
                TryRestoreDisabledSequence(sequence);
                continue;
            }

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
                LogSuppressed(swallowed, "RandomPantheons.Sequences.cs");
            }
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
            Pantheon.P1 => Pantheon1Enabled,
            Pantheon.P2 => Pantheon2Enabled,
            Pantheon.P3 => Pantheon3Enabled,
            Pantheon.P4 => Pantheon4Enabled,
            Pantheon.P5 => Pantheon5Enabled,
            _ => false
        };
    }

    private static bool IsInvalidSequence(IReadOnlyList<BossScene> scenes)
    {
        if (InvalidFirst.Contains(scenes[0].sceneName))
        {
            return true;
        }

        if (InvalidLast.Contains(scenes[scenes.Count - 1].sceneName))
        {
            return true;
        }

        for (int i = 0; i < scenes.Count - 1; i++)
        {
            if (scenes[i].sceneName == "GG_Spa" && scenes[i + 1].sceneName == "GG_Spa")
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryApplyDefaultSequence(List<BossScene> scenes, Pantheon pantheon, out BossScene[] rebuilt)
    {
        if (pantheon != Pantheon.Unknown && TryBuildDefaultSequence(pantheon, scenes, out rebuilt))
        {
            return true;
        }

        foreach (Pantheon candidate in DefaultPantheonOrder.Keys)
        {
            if (TryBuildDefaultSequence(candidate, scenes, out rebuilt))
            {
                return true;
            }
        }

        rebuilt = Array.Empty<BossScene>();
        return false;
    }

    internal static bool TryBuildVanillaSequence(IReadOnlyList<BossScene> currentScenes, int pantheonNumber, out BossScene[] rebuilt)
    {
        Pantheon pantheon = pantheonNumber switch
        {
            1 => Pantheon.P1,
            2 => Pantheon.P2,
            3 => Pantheon.P3,
            4 => Pantheon.P4,
            5 => Pantheon.P5,
            _ => Pantheon.Unknown
        };

        if (pantheon == Pantheon.Unknown || currentScenes == null)
        {
            rebuilt = Array.Empty<BossScene>();
            return false;
        }

        try
        {
            return TryBuildDefaultSequence(pantheon, currentScenes.Where(scene => scene != null).ToList(), out rebuilt);
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "RandomPantheons.Sequences.cs");
            rebuilt = Array.Empty<BossScene>();
            return false;
        }
    }

    private static bool TryBuildDefaultSequence(Pantheon pantheon, List<BossScene> scenes, out BossScene[] rebuiltArray)
    {
        if (!DefaultPantheonOrder.TryGetValue(pantheon, out string[]? defaultOrder))
        {
            rebuiltArray = Array.Empty<BossScene>();
            return false;
        }

        Dictionary<string, BossScene> sceneMap = new(StringComparer.OrdinalIgnoreCase);
        foreach (BossScene scene in scenes)
        {
            if (!sceneMap.ContainsKey(scene.sceneName))
            {
                sceneMap[scene.sceneName] = scene;
            }
        }

        try
        {
            foreach (BossScene scene in Resources.FindObjectsOfTypeAll<BossScene>())
            {
                if (scene == null || string.IsNullOrEmpty(scene.sceneName) || sceneMap.ContainsKey(scene.sceneName))
                {
                    continue;
                }

                sceneMap[scene.sceneName] = scene;
            }
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "RandomPantheons.Sequences.cs");
        }

        if (!sceneMap.TryGetValue("GG_Spa", out BossScene? spaTemplate))
        {
            spaTemplate = scenes.FirstOrDefault(scene => IsSpa(scene.sceneName));
            if (spaTemplate == null)
            {
                rebuiltArray = Array.Empty<BossScene>();
                return false;
            }
        }

        List<BossScene> rebuilt = new(defaultOrder.Length);
        foreach (string name in defaultOrder)
        {
            if (IsSpa(name))
            {
                rebuilt.Add(spaTemplate);
                continue;
            }

            if (!sceneMap.TryGetValue(name, out BossScene scene))
            {
                rebuiltArray = Array.Empty<BossScene>();
                return false;
            }

            rebuilt.Add(scene);
        }

        rebuiltArray = rebuilt.ToArray();
        return true;
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
            LogSuppressed(swallowed, "RandomPantheons.Sequences.cs");
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
            LogSuppressed(swallowed, "RandomPantheons.Sequences.cs");
            return false;
        }
    }

    private bool TryBuildSequence(List<BossScene> scenes, Pantheon pantheon, out List<BossScene> result)
    {
        result = new List<BossScene>(scenes.Count);
        if (pantheon == Pantheon.Unknown)
        {
            return false;
        }

        int targetSpaCount = pantheon == Pantheon.P5 ? 7 : 1;
        BossScene? spaTemplate = scenes.FirstOrDefault(scene => IsSpa(scene.sceneName));
        if (spaTemplate == null)
        {
            return false;
        }

        Dictionary<string, BossScene> uniqueBosses = new(StringComparer.OrdinalIgnoreCase);
        foreach (BossScene scene in scenes)
        {
            if (IsSpa(scene.sceneName))
            {
                continue;
            }

            if (!uniqueBosses.ContainsKey(scene.sceneName))
            {
                uniqueBosses[scene.sceneName] = scene;
            }
        }

        List<BossScene> bosses = uniqueBosses.Values.ToList();
        if (bosses.Count < 2 || targetSpaCount > bosses.Count - 1)
        {
            return false;
        }

        const int maxAttempts = 200;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            bosses = bosses.OrderBy(_ => rand.Next()).ToList();
            if (InvalidFirst.Contains(bosses[0].sceneName) || InvalidLast.Contains(bosses[bosses.Count - 1].sceneName))
            {
                continue;
            }

            List<int> gaps = Enumerable.Range(0, bosses.Count - 1).OrderBy(_ => rand.Next()).ToList();
            HashSet<int> selectedGaps = new(gaps.Take(targetSpaCount));

            result.Clear();
            for (int i = 0; i < bosses.Count; i++)
            {
                result.Add(bosses[i]);
                if (selectedGaps.Contains(i))
                {
                    result.Add(spaTemplate);
                }
            }

            if (!IsInvalidSequence(result))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSpa(string sceneName) => sceneName.Equals("GG_Spa", StringComparison.OrdinalIgnoreCase);
}
