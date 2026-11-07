using InControl;
using Satchel.BetterMenus;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class ShowHPOnDeath : Module
{
    private void OnHeroUpdate()
    {
        if (!Settings.EnabledMod)
        {
            return;
        }

        TryBeginPendingTrackingAfterTransition();

        if (QuickMenu.IsHotkeyInputBlocked())
        {
            return;
        }

        if (Settings.Keybinds.Hide.WasPressed)
        {
            ToggleHud();
        }
    }

    private static void StartTracking(string sceneName, bool fromWorkshop, bool clearDisplay = true)
    {
        trackingGeneration++;
        shouldTrackBosses = true;
        enteredFromWorkshop = fromWorkshop;
        trackedScene = sceneName;
        Scene activeScene = USceneManager.GetActiveScene();
        trackedSceneHandle = activeScene.IsValid()
            && activeScene.isLoaded
            && string.Equals(activeScene.name, sceneName, StringComparison.Ordinal)
            ? activeScene.handle
            : -1;
        pendingTrackingStart = false;
        pendingTrackingScene = "";
        pendingTrackingFromWorkshop = false;
        pendingTrackingClearDisplay = true;
        ClearPendingDisplay();
        ClearTrackedBossCollection();
        phaseTrackers.Clear();
        if (clearDisplay)
        {
            UpdateDisplay("");
        }
    }

    private static void ResetTracking()
    {
        trackingGeneration++;
        shouldTrackBosses = false;
        enteredFromWorkshop = false;
        trackedScene = "";
        trackedSceneHandle = -1;
        pendingTrackingStart = false;
        pendingTrackingScene = "";
        pendingTrackingFromWorkshop = false;
        pendingTrackingClearDisplay = true;
        ClearTrackedBossCollection();
        phaseTrackers.Clear();
    }

    private static void ScheduleTrackingStart(string sceneName, bool fromWorkshop, bool clearDisplay)
    {
        trackingGeneration++;
        shouldTrackBosses = false;
        enteredFromWorkshop = false;
        trackedScene = "";
        trackedSceneHandle = -1;
        pendingTrackingStart = true;
        pendingTrackingScene = sceneName;
        pendingTrackingFromWorkshop = fromWorkshop;
        pendingTrackingClearDisplay = clearDisplay;
        ClearPendingDisplay();
        ClearTrackedBossCollection();
        phaseTrackers.Clear();
        if (clearDisplay)
        {
            UpdateDisplay("");
        }
    }

    private static void ClearTrackedBossCollection()
    {
        trackedBossesByKey.Clear();
        trackedBossOrder.Clear();
        bossKeyByInstanceId.Clear();
        bossSignatureCounts.Clear();
        initialHPs.Clear();
    }

    private static void ClearPendingDisplay()
    {
        hasPendingDisplay = false;
        pendingDisplayText = "";
    }

    private static void CancelAutoHide()
    {
        autoHideGeneration++;
    }

    private void ClearRuntimeState(bool clearLastDisplay)
    {
        CancelAutoHide();
        ClearPendingDisplay();
        ResetTracking();
        personalBestByBoss.Clear();
        if (clearLastDisplay)
        {
            lastDisplayText = "";
        }

        UpdateDisplay("");
    }

    private string BeforeSceneLoad(string newSceneName)
    {
        string currentScene = USceneManager.GetActiveScene().name;
        bool enteringFromWorkshop = currentScene == "GG_Workshop" && allowedBossScenes.Contains(newSceneName);
        bool enteringBossFromOther = !enteringFromWorkshop && allowedBossScenes.Contains(newSceneName) && currentScene != newSceneName;
        bool reloadingTrackedBoss = shouldTrackBosses && enteredFromWorkshop && allowedBossScenes.Contains(currentScene) && currentScene == trackedScene && currentScene == newSceneName;
        bool leavingTrackedBoss = shouldTrackBosses && allowedBossScenes.Contains(currentScene) && currentScene == trackedScene && currentScene != newSceneName;
        bool showingAtReturn = hasPendingDisplay && showAtScenes.Contains(newSceneName);

        if (!Settings.EnabledMod)
        {
            ClearRuntimeState(clearLastDisplay: true);
            return newSceneName;
        }
        if (enteringFromWorkshop)
        {
            StartTracking(newSceneName, fromWorkshop: true);
            return newSceneName;
        }

        if (reloadingTrackedBoss)
        {
            string text = BuildDisplayText();
            DisplayWithAutoHide(text);
            ScheduleTrackingStart(newSceneName, fromWorkshop: true, clearDisplay: false);
            return newSceneName;
        }

        if (enteringBossFromOther)
        {
            StartTracking(newSceneName, fromWorkshop: false);
            return newSceneName;
        }

        if (leavingTrackedBoss)
        {
            string text = BuildDisplayText();
            if (enteredFromWorkshop)
            {
                DisplayWithAutoHide(text);
            }
            else if (showAtScenes.Contains(newSceneName))
            {
                if (!BossSequenceController.WasCompleted)
                {
                    DisplayWithAutoHide(text);
                }
                ClearPendingDisplay();
            }
            else
            {
                hasPendingDisplay = true;
                pendingDisplayText = text;
            }

            ResetTracking();
            return newSceneName;
        }
        if (showingAtReturn)
        {
            if (!BossSequenceController.WasCompleted)
            {
                DisplayWithAutoHide(pendingDisplayText);
            }
            ClearPendingDisplay();
            ResetTracking();
            return newSceneName;
        }

        if (!shouldTrackBosses && !hasPendingDisplay)
        {
            ClearTrackedBossCollection();
            UpdateDisplay("");
        }

        return newSceneName;
    }

    private static void TryBeginPendingTrackingAfterTransition()
    {
        if (!pendingTrackingStart || string.IsNullOrEmpty(pendingTrackingScene))
        {
            return;
        }

        GameManager? manager = GameManager.instance;
        if (manager == null || manager.IsInSceneTransition)
        {
            return;
        }

        Scene activeScene = USceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            return;
        }

        if (!string.Equals(activeScene.name, pendingTrackingScene, StringComparison.Ordinal))
        {
            return;
        }

        StartTracking(pendingTrackingScene, pendingTrackingFromWorkshop, pendingTrackingClearDisplay);
    }
}
