using InControl;
using Satchel.BetterMenus;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class ShowHPOnDeath : Module
{
    private static PhaseTracker GetPhaseTracker(string displayName)
    {
        if (!phaseTrackers.TryGetValue(displayName, out PhaseTracker? tracker))
        {
            tracker = new PhaseTracker();
            phaseTrackers[displayName] = tracker;
        }
        return tracker;
    }

    private static void UpdatePhaseTracker(string displayName, int hp)
    {
        PhaseTracker tracker = GetPhaseTracker(displayName);
        int phaseIndex = Math.Min(tracker.CurrentPhase, 2);
        int clampedHp = Math.Max(hp, 0);

        if (tracker.MaxHP[phaseIndex] == 0)
        {
            tracker.MaxHP[phaseIndex] = clampedHp;
            tracker.CurrentHP[phaseIndex] = clampedHp;
            tracker.LastHP = clampedHp;
            return;
        }

        if (clampedHp > tracker.MaxHP[phaseIndex] && !tracker.PhaseDamaged[phaseIndex])
        {
            tracker.MaxHP[phaseIndex] = clampedHp;
        }

        if (tracker.LastHP >= 0 && clampedHp > tracker.LastHP && tracker.PhaseDamaged[phaseIndex] && phaseIndex < 2)
        {
            tracker.CurrentHP[phaseIndex] = 0;
            tracker.PhaseDamaged[phaseIndex] = true;
            tracker.CurrentPhase++;
            phaseIndex = tracker.CurrentPhase;
            tracker.MaxHP[phaseIndex] = clampedHp;
            tracker.CurrentHP[phaseIndex] = clampedHp;
            tracker.LastHP = clampedHp;
            return;
        }

        tracker.CurrentHP[phaseIndex] = Math.Min(clampedHp, tracker.MaxHP[phaseIndex]);
        if (tracker.CurrentHP[phaseIndex] < tracker.MaxHP[phaseIndex])
        {
            tracker.PhaseDamaged[phaseIndex] = true;
        }
        tracker.LastHP = clampedHp;
    }

    private static List<BossHistoryPart> GetPhaseParts(string bossName, PhaseTracker tracker, int fallbackInitialHp)
    {
        List<BossHistoryPart> parts = new();
        int fallbackMax = tracker.MaxHP[Math.Min(tracker.CurrentPhase, 2)];
        if (fallbackMax == 0 && fallbackInitialHp > 0)
        {
            fallbackMax = fallbackInitialHp;
        }

        for (int i = 0; i < 3; i++)
        {
            int max = tracker.MaxHP[i] > 0 ? tracker.MaxHP[i] : fallbackMax;
            if (max <= 0)
            {
                continue;
            }

            int current;
            if (i < tracker.CurrentPhase)
            {
                current = 0;
            }
            else if (i == tracker.CurrentPhase)
            {
                current = Math.Min(tracker.CurrentHP[i], max);
            }
            else
            {
                current = max;
            }

            parts.Add(new BossHistoryPart { Name = $"Phase {i + 1}", Hp = current, Max = max });
        }

        return parts;
    }
}
