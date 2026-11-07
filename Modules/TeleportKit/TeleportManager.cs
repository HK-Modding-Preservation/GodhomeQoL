using GodhomeQoL.Modules.Tools;

namespace GodhomeQoL.Modules.QoL;

internal sealed class TeleportManager : IDisposable
{
    private const string WorkshopScene = "GG_Workshop";
    private const int LastStatueId = 35;
    private const float StatueSearchRadius = 6f;
    private const float StatueFloorTolerance = 12f;
    private const float HeroReadyTimeoutSeconds = 6f;
    private const float StatueFightStartTimeoutSeconds = 10f;
    private static readonly string[] StatueLevelNames = { "Attuned", "Ascended", "Radiant" };

    private readonly TeleportKit mod;
    private bool isBusy;
    private bool statueFightActive;
    private static bool pendingUiReset;
    private static readonly HashSet<string> GodhomeHubScenes = new(StringComparer.Ordinal)
    {
        "GG_Workshop",
        "GG_Atrium",
        "GG_Atrium_Roof"
    };

    internal TeleportManager(TeleportKit mod)
    {
        this.mod = mod;
    }

    internal bool IsBusy => isBusy || statueFightActive;

    public void Dispose()
    {
        isBusy = false;
        statueFightActive = false;
        pendingUiReset = false;
    }

    internal void StartTeleport(Vector3 position, string scene)
    {
        if (IsBusy || GameManager.instance.IsGamePaused())
        {
            return;
        }

        if (SpeedChanger.IsTimeScaleFrozen || FreezeHitboxes.IsFreezeActive())
        {
            mod.Log.Write("Teleport blocked: Freeze Hitboxes is active.");
            return;
        }

        isBusy = true;
        mod.Input.ShowMenu = false;
        mod.Log.Write($"Starting teleport to {position} in {scene}");
        GameManager.instance.StartCoroutine(TeleportToBoss(position, scene));
    }

    internal void StartStatueFight(int bossId, int level, bool altVersion)
    {
        if (IsBusy || GameManager.instance.IsGamePaused())
        {
            return;
        }

        if (level < 0 || level >= StatueLevelNames.Length)
        {
            QuickMenu.ShowStatusMessageFromExternal("TeleportKit/FightBadLevel".Localize());
            return;
        }

        (int id, string name, Vector3 position, string scene) boss = mod.Data.Bosses.FirstOrDefault(b => b.id == bossId);
        if (bossId < 1 || bossId > LastStatueId || !string.Equals(boss.scene, WorkshopScene, StringComparison.Ordinal))
        {
            QuickMenu.ShowStatusMessageFromExternal(string.Format("TeleportKit/FightNotStatue".Localize(), bossId, LastStatueId));
            return;
        }

        if (SpeedChanger.IsTimeScaleFrozen || FreezeHitboxes.IsFreezeActive())
        {
            mod.Log.Write("Statue fight blocked: Freeze Hitboxes is active.");
            return;
        }

        statueFightActive = true;
        mod.Input.ShowMenu = false;
        mod.Log.Write($"Starting statue fight {bossId} ({boss.name}) at level {level}, second version {altVersion}");
        GameManager.instance.StartCoroutine(StatueFightRoutine(boss.position, bossId, level, altVersion));
    }

    private IEnumerator StatueFightRoutine(Vector3 statuePosition, int bossId, int level, bool altVersion)
    {
        try
        {
            yield return TeleportToBoss(statuePosition, WorkshopScene);

            float deadline = Time.realtimeSinceStartup + HeroReadyTimeoutSeconds;
            while (!IsReadyForStatueFight())
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    mod.Log.Write("Statue fight aborted: the hero is not ready in GG_Workshop");
                    yield break;
                }

                yield return null;
            }

            yield return new WaitForSeconds(0.2f);

            if (!IsReadyForStatueFight())
            {
                mod.Log.Write("Statue fight aborted: the hero is not ready in GG_Workshop");
                yield break;
            }

            BeginStatueFight(statuePosition, bossId, level, altVersion);
        }
        finally
        {
            statueFightActive = false;
        }
    }

    private static bool IsReadyForStatueFight()
    {
        GameManager? manager = GameManager.instance;
        HeroController? hero = HeroController.instance;
        if (manager == null || hero == null)
        {
            return false;
        }

        return string.Equals(manager.sceneName, WorkshopScene, StringComparison.Ordinal)
            && !manager.IsInSceneTransition
            && !manager.IsGamePaused()
            && manager.gameState == GameState.PLAYING
            && hero.acceptingInput
            && !hero.controlReqlinquished
            && hero.cState.onGround
            && !hero.cState.transitioning
            && !hero.cState.dead
            && !SpeedChanger.IsTimeScaleFrozen
            && !FreezeHitboxes.IsFreezeActive();
    }

    private void BeginStatueFight(Vector3 statuePosition, int bossId, int level, bool altVersion)
    {
        BossStatue? statue = FindStatueNear(statuePosition, out float offset);
        if (statue == null || statue.bossUIControlFSM == null)
        {
            mod.Log.Write($"Statue fight aborted: no statue near {statuePosition}");
            QuickMenu.ShowStatusMessageFromExternal(string.Format("TeleportKit/FightStatueNotFound".Localize(), bossId));
            return;
        }

        mod.Log.Write($"Statue for {bossId}: {statue.name} (alt version now: {statue.UsingDreamVersion}, wanted: {altVersion}, offset {offset:0.00})");

        if (statue.hasNoTiers)
        {
            QuickMenu.ShowStatusMessageFromExternal(string.Format("TeleportKit/FightNotStatue".Localize(), bossId, LastStatueId));
            return;
        }

        if (altVersion && !statue.HasDreamVersion)
        {
            BossStatue.BossUIDetails mainDetails = statue.bossDetails;
            QuickMenu.ShowStatusMessageFromExternal(string.Format("TeleportKit/FightNoSecondVersion".Localize(), Lang.Get(mainDetails.nameKey, mainDetails.nameSheet)));
            return;
        }

        BossStatue.Completion completion = altVersion ? statue.DreamStatueState : statue.StatueState;
        BossStatue.BossUIDetails details = altVersion ? statue.dreamBossDetails : statue.bossDetails;
        string bossName = Lang.Get(details.nameKey, details.nameSheet);

        if (!completion.isUnlocked)
        {
            QuickMenu.ShowStatusMessageFromExternal(string.Format("TeleportKit/FightLocked".Localize(), bossName));
            return;
        }

        if (level == 2 && !completion.completedTier2 && !ModuleManager.IsModuleLoaded<UnlockRadiant>())
        {
            QuickMenu.ShowStatusMessageFromExternal(string.Format("TeleportKit/FightRadiantLocked".Localize(), bossName));
            return;
        }

        if (BossSequenceController.IsInSequence)
        {
            BossSequenceController.Reset();
            mod.Log.Write("Cleared a pantheon run that was left without finishing");
        }

        if (statue.UsingDreamVersion != altVersion)
        {
            bool lever = ReflectionHelper.GetField<BossStatue, IBossStatueToggle>(statue, "dreamToggle") is BossStatueLever;
            statue.SetDreamVersion(altVersion, lever, false);
            mod.Log.Write($"Statue {statue.name} switched to {(altVersion ? "second" : "first")} version (lever: {lever})");
        }

        if (!TryLoadStatueBoss(statue, level))
        {
            QuickMenu.ShowStatusMessageFromExternal(string.Format("TeleportKit/FightFailed".Localize(), bossName));
            return;
        }

        PlayMakerFSM challengeFsm = statue.bossUIControlFSM;
        GameObject? camLock = challengeFsm.FsmVariables.FindFsmGameObject("CamLock")?.Value;
        if (camLock != null)
        {
            camLock.SetActive(true);
        }

        HeroController hero = HeroController.instance;
        FSMUtility.SendEventToGameObject(challengeFsm.gameObject, "NPC CONTROL OFF");
        PlayerData.instance.SetBool("disablePause", true);
        hero.RelinquishControl();
        hero.StopAnimationControl();

        challengeFsm.SetState("Take Control");
        mod.Log.Write($"Statue fight started: {bossName} {StatueLevelNames[level]}");
        QuickMenu.ShowStatusMessageFromExternal(string.Format("TeleportKit/FightStarting".Localize(), bossName, StatueLevelNames[level]));
        GameManager.instance.StartCoroutine(StatueFightWatchdog(challengeFsm, camLock, bossName, USceneManager.GetActiveScene().handle));
    }

    private IEnumerator StatueFightWatchdog(PlayMakerFSM challengeFsm, GameObject? camLock, string bossName, int sceneHandle)
    {
        yield return new WaitForSecondsRealtime(StatueFightStartTimeoutSeconds);

        GameManager? manager = GameManager.instance;
        HeroController? hero = HeroController.instance;
        if (manager == null
            || hero == null
            || manager.IsInSceneTransition
            || USceneManager.GetActiveScene().handle != sceneHandle)
        {
            yield break;
        }

        string state = challengeFsm != null ? challengeFsm.ActiveStateName : "none";
        mod.Log.Write($"Statue fight did not start in {StatueFightStartTimeoutSeconds} s (state {state}); giving control back");

        BossSceneController.SetupEvent = null;
        if (challengeFsm != null)
        {
            challengeFsm.SetState("Inert");
            Collider2D? inspectCollider = challengeFsm.GetComponent<Collider2D>();
            if (inspectCollider != null)
            {
                inspectCollider.enabled = true;
            }

            FSMUtility.SendEventToGameObject(challengeFsm.gameObject, "CONVO CANCEL");
        }

        if (camLock != null)
        {
            camLock.SetActive(false);
        }

        FsmBool? dreamConvo = hero.gameObject.LocateMyFSM("Dream Nail")?.FsmVariables.FindFsmBool("Dream Convo");
        if (dreamConvo != null)
        {
            dreamConvo.Value = false;
        }

        PlayerData.instance.SetBool("disablePause", false);
        hero.RegainControl();
        hero.StartAnimationControl();
        QuickMenu.ShowStatusMessageFromExternal(string.Format("TeleportKit/FightFailed".Localize(), bossName));
    }

    private static BossStatue? FindStatueNear(Vector3 position, out float offset)
    {
        BossStatue? best = null;
        offset = float.MaxValue;
        foreach (BossStatue statue in UObject.FindObjectsOfType<BossStatue>())
        {
            Vector3 point = statue.bossUIControlFSM != null
                ? statue.bossUIControlFSM.transform.position
                : statue.transform.position;
            if (Mathf.Abs(point.y - position.y) > StatueFloorTolerance)
            {
                continue;
            }

            float dx = Mathf.Abs(point.x - position.x);
            if (dx < offset)
            {
                offset = dx;
                best = statue;
            }
        }

        return offset <= StatueSearchRadius ? best : null;
    }

    private bool TryLoadStatueBoss(BossStatue statue, int level)
    {
        GameObject holder = new("GodhomeQoL_StatueFight");
        holder.SetActive(false);
        try
        {
            BossChallengeUI challengeUi = holder.AddComponent<BossChallengeUI>();
            ReflectionHelper.SetField(challengeUi, "bossStatue", statue);
            bool levelSelected = false;
            challengeUi.OnLevelSelected += () => levelSelected = true;
            challengeUi.LoadBoss(level, false);
            return levelSelected;
        }
        catch (Exception ex)
        {
            mod.Log.Write($"Statue fight setup failed: {ex}");
            return false;
        }
        finally
        {
            UObject.Destroy(holder);
        }
    }

    private static void PlaceHero(Vector3 position)
    {
        HeroController.instance.transform.position = position;
        HeroController.instance.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
    }

    private static void FadeSceneInIfPresent()
    {
        if (GameManager.instance.cameraCtrl != null)
        {
            GameManager.instance.cameraCtrl.FadeSceneIn();
        }
    }

    private IEnumerator TeleportToBoss(Vector3 targetPos, string scene)
    {
        if (GameManager.instance.IsGamePaused())
        {
            isBusy = false;
            mod.Log.Write("Teleport aborted - game is paused");
            yield break;
        }

        if (SpeedChanger.IsTimeScaleFrozen || FreezeHitboxes.IsFreezeActive())
        {
            isBusy = false;
            mod.Log.Write("Teleport aborted - Freeze Hitboxes is active");
            yield break;
        }

        bool timeScaleOverridden = SpeedChanger.TryBeginTimeScaleOverride(1f, out int timeScaleOverrideHandle);

        try
        {
            mod.Log.Write($"Beginning teleport process to {targetPos} in {scene}");

            if (scene == "GG_Workshop" && !PlayerData.instance.GetBool("godseekerUnlocked"))
            {
                PlayerData.instance.SetBool("godseekerUnlocked", true);
                PlayerData.instance.SetBool("godseekerMet", true);
                mod.Log.Write("Unlocked Godhome access");
            }

            bool isSameScene = GameManager.instance.sceneName == scene;
            bool isDreamRoom = scene == "Dream_Room_Believer_Shrine";
            bool isPOP46 = scene == "White_Palace_20" && targetPos == new Vector3(19.5625f, 169.4081f, 0f);
            bool isPantheonI = (scene == "GG_Atrium") && targetPos == new Vector3(97.15343f, 35.40812f, 0f);
            bool isPantheonII = (scene == "GG_Atrium") && targetPos == new Vector3(108.4116f, 35.40812f, 0f);
            bool isPantheonIII = (scene == "GG_Atrium") && targetPos == new Vector3(120.2336f, 35.40812f, 0f);
            bool isPantheonIV = (scene == "GG_Atrium") && targetPos == new Vector3(147.3174f, 35.40812f, 0f);
            bool isPantheonV = (scene == "GG_Atrium_Roof") && targetPos == new Vector3(96.971f, 73.408f, 0f);
            bool isPantheonVSegmented = (scene == "GG_Atrium_Roof") && targetPos == new Vector3(53.81337f, 19.40812f, 0f);
            bool isPantheonBench = (scene == "GG_Atrium_Roof") && targetPos == new Vector3(120.97f, 42.40812f, 0f);
            bool shouldSetHazard = !(isPantheonV || isPantheonVSegmented || isPantheonBench);

            if (!isSameScene)
            {
                HeroController.instance.StopAnimationControl();
                HeroController.instance.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
                HeroController.instance.RegainControl();

                string targetScene = scene;
                Vector3 entryPos = targetPos;
                string entryGate = "left1";

                if (isDreamRoom)
                {
                    targetScene = "Dream_Room_Believer_Shrine";
                    entryPos = targetPos;
                }
                else if (isPOP46)
                {
                    targetScene = "White_Palace_20";
                    entryPos = targetPos;
                    entryGate = "bot1";
                }
                else if (isPantheonI || isPantheonII || isPantheonIII)
                {
                    targetScene = "GG_Atrium";
                    entryPos = targetPos;
                    entryGate = "top1";
                }
                else if (isPantheonIV)
                {
                    targetScene = "GG_Atrium";
                    entryPos = targetPos;
                    entryGate = "top1";
                }
                else if (isPantheonV || isPantheonVSegmented || isPantheonBench)
                {
                    entryGate = "bot1";
                    entryPos = targetPos;
                }
                else if (scene.StartsWith("White_Palace", StringComparison.Ordinal))
                {
                    targetScene = "White_Palace_06";
                    entryPos = new Vector3(0.1111f, 7.408124f, 0f);
                }

                mod.Log.Write($"Loading target scene: {targetScene}");

                if (isPantheonV || isPantheonVSegmented || isPantheonBench || isPantheonIV || isPantheonI || isPantheonII || isPantheonIII)
                {
                    CleanSceneState();
                }

                var loadInfo = new GameManager.SceneLoadInfo
                {
                    SceneName = targetScene,
                    EntryGateName = entryGate,
                    EntryDelay = 0f,
                    WaitForSceneTransitionCameraFade = false,
                    Visualization = GameManager.SceneLoadVisualizations.Default,
                    PreventCameraFadeOut = targetScene.StartsWith("White_Palace", StringComparison.Ordinal) || isDreamRoom
                };

                GameManager.instance.BeginSceneTransition(loadInfo);
                yield return new WaitWhile(() => GameManager.instance.IsInSceneTransition);

                if (targetScene.StartsWith("White_Palace", StringComparison.Ordinal) || isDreamRoom)
                {
                    yield return new WaitForSeconds(0.2f);
                    FadeSceneInIfPresent();
                }

                PlaceHero(entryPos);
                if (shouldSetHazard)
                {
                    HeroController.instance.SetHazardRespawn(entryPos, true);
                }

                if (isDreamRoom)
                {
                    yield return new WaitForSeconds(0.5f);
                    PlaceHero(targetPos);
                }
                else if (isPantheonI || isPantheonII || isPantheonIII)
                {
                    GameManager.instance.sceneName = "GG_Atrium";
                    PlaceHero(targetPos);
                    if (shouldSetHazard)
                    {
                        HeroController.instance.SetHazardRespawn(targetPos, true);
                    }
                    FadeSceneInIfPresent();
                    yield return new WaitForSeconds(0.05f);
                    PlaceHero(targetPos);
                    if (shouldSetHazard)
                    {
                        HeroController.instance.SetHazardRespawn(targetPos, true);
                    }
                }
                else if (isPantheonV || isPantheonVSegmented || isPantheonBench)
                {
                    GameManager.instance.sceneName = "GG_Atrium_Roof";
                    PlaceHero(targetPos);
                    FadeSceneInIfPresent();
                    yield return new WaitForSeconds(0.05f);
                    PlaceHero(targetPos);
                }
            }
            else
            {
                PlaceHero(targetPos);
            }

            if (!isSameScene && (scene.StartsWith("White_Palace", StringComparison.Ordinal) || isDreamRoom))
            {
                yield return new WaitForEndOfFrame();
                FadeSceneInIfPresent();
            }

            mod.Log.Write("Teleport completed successfully");
            isBusy = false;
            NotifyTeleportCompleteForUi();
        }
        finally
        {
            isBusy = false;
            if (timeScaleOverridden)
            {
                GameManager? manager = GameManager.instance;
                if (manager != null)
                {
                    _ = manager.StartCoroutine(RestoreTimeScaleAfterTeleport(timeScaleOverrideHandle));
                }
                else
                {
                    SpeedChanger.EndTimeScaleOverride(timeScaleOverrideHandle);
                }
            }
        }
    }

    private static IEnumerator RestoreTimeScaleAfterTeleport(int timeScaleOverrideHandle)
    {
        yield return null;

        if (GameManager.instance != null && GameManager.instance.IsInSceneTransition)
        {
            yield return new WaitWhile(() => GameManager.instance.IsInSceneTransition);
        }

        yield return new WaitForSecondsRealtime(0.5f);

        SpeedChanger.EndTimeScaleOverride(timeScaleOverrideHandle);
    }

    private static void CleanSceneState()
    {
        UIManager.instance.UIClosePauseMenu();
        GameManager.instance.FadeSceneIn();
        GameManager.instance.isPaused = false;
        GameCameras.instance.ResumeCameraShake();

        if (HeroController.SilentInstance != null)
        {
            HeroController.instance.UnPause();

            if (HeroController.instance.cState.onConveyor || HeroController.instance.cState.onConveyorV || HeroController.instance.cState.inConveyorZone)
            {
                HeroController.instance.GetComponent<ConveyorMovementHero>()?.StopConveyorMove();
                HeroController.instance.cState.inConveyorZone = false;
                HeroController.instance.cState.onConveyor = false;
                HeroController.instance.cState.onConveyorV = false;
            }

            HeroController.instance.cState.nearBench = false;
        }

        MenuButtonList.ClearAllLastSelected();
        GameManager.instance.actorSnapshotUnpaused.TransitionTo(0f);
        GameManager.instance.ui.AudioGoToGameplay(0.2f);
        PlayerData.instance.atBench = false;
    }

    internal static void TryResetUiInput(GameObject? preferredSelection)
    {
        if (!pendingUiReset)
        {
            return;
        }

        pendingUiReset = false;

        EventSystem? eventSystem = EventSystem.current;
        if (eventSystem == null || InputHandler.Instance == null)
        {
            return;
        }

        GameObject? target = preferredSelection ?? eventSystem.currentSelectedGameObject;
        if (target != null)
        {
            eventSystem.SetSelectedGameObject(null);
            eventSystem.SetSelectedGameObject(target);
        }

        InputHandler.Instance.StopUIInput();
        InputHandler.Instance.StartUIInput();
    }

    private static void NotifyTeleportCompleteForUi()
    {
        string? sceneName = GameManager.instance?.sceneName;
        pendingUiReset = !string.IsNullOrEmpty(sceneName) && GodhomeHubScenes.Contains(sceneName!);
    }
}
