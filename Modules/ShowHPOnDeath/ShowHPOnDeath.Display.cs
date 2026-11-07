using InControl;
using Satchel.BetterMenus;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class ShowHPOnDeath : Module
{
    private void OnPlayerDead()
    {
        if (!shouldTrackBosses)
        {
            return;
        }

        ShowBossDisplayForScene();
    }

    private void ShowBossDisplayForScene()
    {
        string displayText = BuildDisplayText();
        DisplayWithAutoHide(displayText);
    }

    private string BuildDisplayText()
    {
        var snapshot = new List<(string Key, string Name, int HP)>();
        bool suppressCollector = IsCollectorImmortalTrackingSuppressed();
        bool suppressZote = IsZoteImmortalTrackingSuppressed();
        foreach (string bossKey in trackedBossOrder)
        {
            if (!trackedBossesByKey.TryGetValue(bossKey, out BossState? state))
            {
                continue;
            }

            if (suppressCollector && string.Equals(state.DisplayName, "The Collector", StringComparison.Ordinal))
            {
                continue;
            }

            if (suppressZote && string.Equals(state.DisplayName, "Grey Prince Zote", StringComparison.Ordinal))
            {
                continue;
            }

            int hp = Math.Max(state.CurrentHP, 0);
            if (hp <= 0)
            {
                continue;
            }

            snapshot.Add((bossKey, state.DisplayName, hp));
            if (!personalBestByBoss.TryGetValue(bossKey, out int best) || hp < best)
            {
                personalBestByBoss[bossKey] = hp;
            }
        }

        if (snapshot.Count == 0)
        {
            return string.Empty;
        }

        string hideKey = Settings.Keybinds.Hide.UnfilteredBindings.Count > 0
            ? Settings.Keybinds.Hide.UnfilteredBindings[0].Name
            : "Unbound";
        string title = BossHistory.TryGetBossForScene(trackedScene, out string fightName) ? fightName : snapshot[0].Name;
        System.Text.StringBuilder builder = new();
        builder.Append(ShowHPOnDeathDisplay.TitleLine).Append('\t').Append(title).Append('\n');

        Dictionary<string, int> nameInstances = [];
        foreach (var (bossKey, name, currentHP) in snapshot)
        {
            int count = nameInstances.TryGetValue(name, out int existingCount) ? existingCount + 1 : 1;
            nameInstances[name] = count;
            string finalName = count > 1 ? $"{name} ({count})" : name;
            int pb = Settings.ShowPB && personalBestByBoss.TryGetValue(bossKey, out int best) ? best : -1;

            if (phaseBosses.Contains(name) && phaseTrackers.TryGetValue(name, out PhaseTracker? tracker))
            {
                int fallbackInitialHp = initialHPs.TryGetValue(bossKey, out int initialForPhase) ? initialForPhase : 0;
                foreach (BossHistoryPart part in GetPhaseParts(name, tracker, fallbackInitialHp))
                {
                    AppendDisplayRow(builder, part.Name, part.Hp, part.Max, -1);
                }

                if (pb >= 0)
                {
                    builder.Append(ShowHPOnDeathDisplay.NoteLine).Append('\t').Append($"PB {pb}").Append('\n');
                }

                continue;
            }

            int max = initialHPs.TryGetValue(bossKey, out int initialHP) ? initialHP : 0;
            AppendDisplayRow(builder, GetDisplayLabel(bossKey, finalName), currentHP, max, pb);
        }

        builder.Append(ShowHPOnDeathDisplay.HintLine).Append('\t').Append($"Press [{hideKey}] to hide");
        return builder.ToString();
    }

    private static void AppendDisplayRow(System.Text.StringBuilder builder, string label, int hp, int max, int pb)
    {
        builder.Append(ShowHPOnDeathDisplay.BossLine).Append('\t')
            .Append(label.Replace('\t', ' ')).Append('\t')
            .Append(hp).Append('\t')
            .Append(max).Append('\t')
            .Append(pb).Append('\n');
    }

    private void DisplayWithAutoHide(string text)
    {
        int displayGeneration = ++autoHideGeneration;

        lastDisplayText = text;
        UpdateDisplay(text);

        if (Settings.HideAfter10Sec && !string.IsNullOrWhiteSpace(text))
        {
            float seconds = Math.Max(1f, Math.Min(10f, Settings.HudFadeSeconds));
            _ = GlobalCoroutineExecutor.Start(HideDisplayAfterDelay(displayGeneration, seconds));
        }
    }

    private static IEnumerator HideDisplayAfterDelay(int displayGeneration, float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (displayGeneration != autoHideGeneration)
        {
            yield break;
        }

        UpdateDisplay("");
    }

    private void CreateUI()
    {
        if (ShowHPOnDeathDisplay.Instance == null)
        {
            _ = new ShowHPOnDeathDisplay();
        }
    }

    private static void EnsureDisplayUi()
    {
        if (ShowHPOnDeathDisplay.Instance != null || activeInstance == null || !activeInstance.Loaded)
        {
            return;
        }

        activeInstance.CreateUI();
    }

    private static void UpdateDisplay(string text)
    {
        if (ShowHPOnDeathDisplay.Instance == null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                isHudVisible = false;
                return;
            }

            EnsureDisplayUi();
        }

        ShowHPOnDeathDisplay.Instance?.Display(text);
        isHudVisible = !string.IsNullOrWhiteSpace(text);
    }

    private void ToggleHud()
    {
        if (isHudVisible)
        {
            CancelAutoHide();
            UpdateDisplay("");
            return;
        }

        if (string.IsNullOrWhiteSpace(lastDisplayText))
        {
            return;
        }

        DisplayWithAutoHide(lastDisplayText);
    }
}
