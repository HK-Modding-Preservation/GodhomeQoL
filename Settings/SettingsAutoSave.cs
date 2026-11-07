using Satchel;
using Satchel.Futils;

namespace GodhomeQoL;

internal static class SettingsAutoSave
{
    private const float IntervalSeconds = 3f;

    private static int generation;
    private static string? lastSnapshot;

    internal static void Start()
    {
        int current = ++generation;
        lastSnapshot = null;
        _ = GlobalCoroutineExecutor.Start(Loop(current));
    }

    internal static void Stop() => generation++;

    internal static void MarkSaved()
    {
        if (generation == 0)
        {
            return;
        }

        lastSnapshot = Capture();
    }

    private static IEnumerator Loop(int current)
    {
        yield return new WaitForSecondsRealtime(IntervalSeconds);
        lastSnapshot = Capture();

        while (current == generation)
        {
            yield return new WaitForSecondsRealtime(IntervalSeconds);
            if (current != generation)
            {
                yield break;
            }

            try
            {
                GodhomeQoL.UpdatePerSaveMirror();
                string? snapshot = Capture();
                if (snapshot != null && snapshot != lastSnapshot)
                {
                    GodhomeQoL.SaveGlobalSettingsSafe();
                    Logger.Log("Autosaved global settings that changed without being saved");
                }
            }
            catch (Exception ex)
            {
                Logger.LogSuppressed(ex, "SettingsAutoSave.cs");
            }
        }
    }

    private static string? Capture()
    {
        try
        {
            return JsonConvert.SerializeObject(GodhomeQoL.GlobalSettings);
        }
        catch (Exception ex)
        {
            Logger.LogSuppressed(ex, "SettingsAutoSave.cs/Capture");
            return null;
        }
    }
}
