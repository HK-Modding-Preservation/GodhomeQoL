using UnityEngine.Audio;
using Satchel.Futils;
using GodhomeQoL.Modules.QoL;
using GodhomeQoL.Modules.Tools;
namespace GodhomeQoL.Modules.BossChallenge;

public sealed class InfiniteChallenge : Module
{
    private const string WorkshopScene = "GG_Workshop";
    private const string AtriumScene = "GG_Atrium";
    private const string AtriumRoofScene = "GG_Atrium_Roof";
    private const string SpaScene = "GG_Spa";
    private const string HealthHudChildName = "Health";
    private const string BlueHealthControlFsmName = "Blue Health Control";
    private const string UpdateBlueHealthEvent = "UPDATE BLUE HEALTH";
    private const string LastHpAddedEvent = "LAST HP ADDED";
    private const string WeaverlingTag = "Weaverling";
    private const string CharmEffectsName = "Charm Effects";
    private const string WeaverlingControlFsmName = "Weaverling Control";
    private const string CharmEquipCheckEvent = "CHARM EQUIP CHECK";
    private static readonly float[] WeaverlingRecheckDelays = [1.5f, 2f];

    [GlobalSetting]
    [BoolOption]
    public static bool restartFightOnSuccess = false;

    [GlobalSetting]
    [BoolOption]
    public static bool restartFightAndMusic = false;

    private static readonly Func<GameManager.SceneLoadInfo, bool>[] defaultReturnScenePredicates = [
        (info) => info.SceneName is "GG_Workshop",
        (info) => info.SceneName is "White_Palace_09"
    ];
    private static readonly Dictionary<int, Func<GameManager.SceneLoadInfo, bool>> externalReturnScenePredicates = [];
    private static int nextExternalReturnScenePredicateHandle;
    private static bool pendingDeathRestart;
    private static string? pendingDeathSceneName;
    private static bool autoplayGuardArmed;
    private static int autoplayGuardGeneration;

    public override ToggleableLevel ToggleableLevel => ToggleableLevel.ChangeScene;

    internal static bool OwnsFinishedBossReturningFlag() =>
        ModuleManager.TryGetLoadedModule(typeof(InfiniteChallenge), out Module? module)
        && module.Enabled;

    internal static int AddReturnScenePredicate(Func<GameManager.SceneLoadInfo, bool>? predicate)
    {
        if (predicate == null)
        {
            return 0;
        }

        foreach ((int handle, Func<GameManager.SceneLoadInfo, bool> existing) in externalReturnScenePredicates)
        {
            if (existing == predicate)
            {
                return handle;
            }
        }

        int handleId = ++nextExternalReturnScenePredicateHandle;
        externalReturnScenePredicates[handleId] = predicate;
        return handleId;
    }

    internal static void RemoveReturnScenePredicate(int handle)
    {
        if (handle == 0)
        {
            return;
        }

        _ = externalReturnScenePredicates.Remove(handle);
    }

    internal static int RemoveReturnScenePredicate(Func<GameManager.SceneLoadInfo, bool>? predicate)
    {
        if (predicate == null || externalReturnScenePredicates.Count == 0)
        {
            return 0;
        }

        int[] handles = externalReturnScenePredicates
            .Where(pair => pair.Value == predicate)
            .Select(pair => pair.Key)
            .ToArray();

        foreach (int handle in handles)
        {
            _ = externalReturnScenePredicates.Remove(handle);
        }

        return handles.Length;
    }

    private static bool MatchesReturnScenePredicate(GameManager.SceneLoadInfo info)
    {
        foreach (Func<GameManager.SceneLoadInfo, bool> predicate in defaultReturnScenePredicates)
        {
            if (predicate.Invoke(info))
            {
                return true;
            }
        }

        if (externalReturnScenePredicates.Count == 0)
        {
            return false;
        }

        List<int>? brokenHandles = null;
        foreach ((int handle, Func<GameManager.SceneLoadInfo, bool> predicate) in externalReturnScenePredicates.ToArray())
        {
            try
            {
                if (predicate.Invoke(info))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                brokenHandles ??= [];
                brokenHandles.Add(handle);
                LogWarn($"InfiniteChallenge return-scene predicate handle {handle} failed and will be removed: {ex}");
            }
        }

        if (brokenHandles != null)
        {
            foreach (int handle in brokenHandles)
            {
                _ = externalReturnScenePredicates.Remove(handle);
            }
        }

        return false;
    }

    private protected override void Load()
    {
        On.BossSceneController.Awake += RecordSetupEvent;
        On.HeroController.Die += RecordBossDeath;
        On.GameManager.BeginSceneTransition += RestartFight;
        On.tk2dSpriteAnimator.Start += KeepClipStartedBeforeAutoplay;
        pendingDeathRestart = false;
        pendingDeathSceneName = null;
        autoplayGuardArmed = false;
        BossFightRestartCompatibility.RecordCurrentSetupEvent();
    }

    private protected override void Unload()
    {
        On.BossSceneController.Awake -= RecordSetupEvent;
        On.HeroController.Die -= RecordBossDeath;
        On.GameManager.BeginSceneTransition -= RestartFight;
        On.tk2dSpriteAnimator.Start -= KeepClipStartedBeforeAutoplay;
        pendingDeathRestart = false;
        pendingDeathSceneName = null;
        autoplayGuardArmed = false;
        autoplayGuardGeneration++;
    }

    private static void KeepClipStartedBeforeAutoplay(On.tk2dSpriteAnimator.orig_Start orig, tk2dSpriteAnimator self)
    {
        try
        {
            if (autoplayGuardArmed
                && BossSceneController.IsBossScene
                && self.playAutomatically
                && self.Playing
                && self.CurrentClip != null
                && self.CurrentClip != self.DefaultClip)
            {
                Log($"InfiniteChallenge: kept clip '{self.CurrentClip.name}' on '{self.gameObject.name}' instead of letting tk2dSpriteAnimator autoplay '{self.DefaultClip?.name}' over it");
                return;
            }
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "InfiniteChallenge.cs");
        }

        orig(self);
    }

    private static IEnumerator DisarmAutoplayGuardAfterEntry(int generation)
    {
        try
        {
            float deadline = Time.unscaledTime + 30f;
            while (!Ref.GM.IsInSceneTransition && Time.unscaledTime < deadline)
            {
                yield return null;
            }

            while (Ref.GM.IsInSceneTransition && Time.unscaledTime < deadline)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(2f);
        }
        finally
        {
            if (generation == autoplayGuardGeneration)
            {
                autoplayGuardArmed = false;
            }
        }
    }

    private static void RecordSetupEvent(On.BossSceneController.orig_Awake orig, BossSceneController self)
    {
        pendingDeathRestart = false;
        pendingDeathSceneName = null;
        BossFightRestartCompatibility.RecordCurrentSetupEvent();
        orig(self);
        BossFightRestartCompatibility.RecordCurrentSetupEvent();
    }

    private static IEnumerator RecordBossDeath(On.HeroController.orig_Die orig, HeroController self)
    {
        if (BossSceneController.IsBossScene && !BossSequenceController.IsInSequence)
        {
            pendingDeathRestart = true;
            pendingDeathSceneName = GameManager.instance?.sceneName;
        }

        IEnumerator enumerator = orig(self);
        while (enumerator.MoveNext())
        {
            yield return enumerator.Current;
        }
    }

    private static void RestartFight(On.GameManager.orig_BeginSceneTransition orig, GameManager self, GameManager.SceneLoadInfo info)
    {
        string? targetSceneName = info.SceneName;
        if (IsBossFightScene(targetSceneName))
        {
            BossFightRestartCompatibility.RecordCurrentSetupEvent(targetSceneName);
        }

        if (BossFightRestartCompatibility.IsFastReloadTransitionInProgress)
        {
            pendingDeathRestart = false;
            pendingDeathSceneName = null;
            orig(self, info);
            return;
        }

        try
        {
            string currentSceneName = self.sceneName ?? string.Empty;
            BossFightRestartCompatibility.RecordCurrentSetupEvent();
            bool matchedReturnScene = MatchesReturnScenePredicate(info);
            bool trackedBossDeath = pendingDeathRestart || !string.IsNullOrEmpty(pendingDeathSceneName);
            bool liveBossDeath = BossSceneController.IsBossScene
                && !BossSequenceController.IsInSequence
                && Ref.HC.heroDeathPrefab.activeSelf;
            bool deathRestartRequested = trackedBossDeath || liveBossDeath;
            string restartSceneName = currentSceneName;
            if (deathRestartRequested && !string.IsNullOrEmpty(pendingDeathSceneName))
            {
                restartSceneName = pendingDeathSceneName!;
            }
            bool inBossContext = BossSceneController.IsBossScene || deathRestartRequested;
            bool successRestartRequested =
                inBossContext
                && matchedReturnScene
                && restartFightOnSuccess
                && StaticVariableList.GetValue<bool>("finishedBossReturning");
            bool shouldRestart = deathRestartRequested || successRestartRequested;

            if (shouldRestart)
            {
                if (!BossFightRestartCompatibility.TryApplySetupEventForScene(restartSceneName))
                {
                    LogError($"InfiniteChallenge: setup event is not available for scene {restartSceneName}");
                    pendingDeathRestart = false;
                    pendingDeathSceneName = null;
                    orig(self, info);
                    return;
                }

                StaticVariableList.SetValue("finishedBossReturning", false);
                pendingDeathRestart = false;
                pendingDeathSceneName = null;

                autoplayGuardArmed = true;
                _ = GlobalCoroutineExecutor.Start(DisarmAutoplayGuardAfterEntry(++autoplayGuardGeneration));
                _ = GlobalCoroutineExecutor.Start(DelayedEnableRenderer());
                _ = GlobalCoroutineExecutor.Start(RecoverAfterRestart());
                Ref.HC.EnterWithoutInput(true);
                Ref.HC.AcceptInput();
                Ref.HC.StartCoroutine(UpdateHUD());
                Ref.HC.gameObject.LocateMyFSM("Dream Return").GetVariable<FsmBool>("Dream Returning").Value = false;

                if (ModuleManager.TryGetLoadedModule<CarefreeMelodyReset>(out _))
                {
                    CarefreeMelodyReset.MarkEnteredFromWorkshop(currentSceneName);
                    CarefreeMelodyReset.TryResetNow(ignoreBossScene: true);
                }

                info.SceneName = restartSceneName;
                info.EntryGateName = "door_dreamEnter";

                _ = GlobalCoroutineExecutor.Start(SetAudio(restartFightAndMusic));
            }
            else if (pendingDeathRestart)
            {
                pendingDeathRestart = false;
                pendingDeathSceneName = null;
            }
        }
        catch (Exception e) { LogError(e.Message); }

        orig(self, info);
    }

    private static bool IsBossFightScene(string? sceneName)
    {
        string scene = sceneName ?? string.Empty;
        if (scene.Length == 0)
        {
            return false;
        }

        return scene.StartsWith("GG_", StringComparison.Ordinal)
            && scene != WorkshopScene
            && scene != AtriumScene
            && scene != AtriumRoofScene
            && scene != SpaScene;
    }

    private static IEnumerator DelayedEnableRenderer()
    {
        yield return new WaitUntil(() => Ref.GM.IsInSceneTransition);
        yield return new WaitWhile(() => Ref.GM.IsInSceneTransition);

        Ref.HC.EnableRenderer();
    }

    private static IEnumerator RecoverAfterRestart()
    {
        float deadline = Time.unscaledTime + 30f;
        while (!Ref.GM.IsInSceneTransition && Time.unscaledTime < deadline)
        {
            yield return null;
        }

        while (Ref.GM.IsInSceneTransition && Time.unscaledTime < deadline)
        {
            yield return null;
        }

        NormalizeTimeAfterRestart();

        foreach (float delay in WeaverlingRecheckDelays)
        {
            yield return new WaitForSecondsRealtime(delay);
            EnsureWeaverlingsAfterRestart();
        }
    }

    private static void NormalizeTimeAfterRestart()
    {
        try
        {
            if (FreezeHitboxes.IsFreezeActive()
                || SpeedChanger.IsTimeScaleFrozen
                || Ref.GM == null
                || Ref.GM.IsGamePaused())
            {
                return;
            }

            if (TimeController.SlowMotionTimeScale < 1f)
            {
                TimeController.SlowMotionTimeScale = 1f;
            }

            if (TimeController.GenericTimeScale < 1f)
            {
                TimeController.GenericTimeScale = 1f;
            }
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "InfiniteChallenge.cs/NormalizeTime");
        }
    }

    private static void EnsureWeaverlingsAfterRestart()
    {
        try
        {
            PlayerData? data = PlayerData.instance;
            if (Ref.HC == null || data == null || !data.GetBool("equippedCharm_39"))
            {
                return;
            }

            if (GameObject.FindGameObjectsWithTag(WeaverlingTag).Length > 0)
            {
                return;
            }

            GameObject.Find(CharmEffectsName)?.LocateMyFSM(WeaverlingControlFsmName)?.SendEvent(CharmEquipCheckEvent);
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "InfiniteChallenge.cs/Weaverlings");
        }
    }

    private static IEnumerator SetAudio(bool reset)
    {
        AudioMixer mixer = Ref.GM.AudioManager.Reflect().musicSources[0].outputAudioMixerGroup.audioMixer;

        if (reset)
        {
            Ref.GM.AudioManager.Reflect().musicSources.ForEach(
                i => i.Stop()
            );
            Ref.GM.AudioManager.Reflect().currentMusicCue = null;
        }
        else
        {
            mixer.FindSnapshot("Silent").TransitionTo(0.5f);
        }

        yield return new WaitUntil(() => Ref.GM.IsInSceneTransition);
        yield return new WaitWhile(() => Ref.GM.IsInSceneTransition);
        mixer.FindSnapshot("Normal").TransitionTo(0f);
    }


    private static IEnumerator UpdateHUD()
    {
        yield return new WaitUntil(() => Ref.GM.gameState == GameState.PLAYING);

        Ref.GC.hudCanvas?.LocateMyFSM("Slide Out")?.SendEvent("IN");
        Ref.GC.hudCanvas?.gameObject?.SetActive(true);

        if (UIManager.instance != null)
        {
            try
            {
                ReflectionHelper.SetField(UIManager.instance, "uiState", UIState.PLAYING);
            }
            catch (Exception swallowed)
            {
                LogSuppressed(swallowed, "InfiniteChallenge.cs");
            }
        }

        Ref.HC.ClearMP();
        Ref.HC.ClearMPSendEvents();
        RefreshBlueHealthHud();

        yield return null;
        RefreshBlueHealthHud();

        yield return null;
        RefreshBlueHealthHud();
    }

    private static void RefreshBlueHealthHud()
    {
        try
        {
            PlayMakerFSM.BroadcastEvent(UpdateBlueHealthEvent);
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "InfiniteChallenge.cs");
        }

        try
        {
            PlayMakerFSM? blueHealthControl = Ref.GC?.hudCanvas?
                .Child(HealthHudChildName)?
                .LocateMyFSM(BlueHealthControlFsmName);
            if (blueHealthControl is { ActiveStateName: "Wait" })
            {
                blueHealthControl.SendEvent(LastHpAddedEvent);
            }
        }
        catch (Exception swallowed)
        {
            LogSuppressed(swallowed, "InfiniteChallenge.cs");
        }
    }
}
