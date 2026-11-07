namespace GodhomeQoL.Modules.Tools;

public sealed class FreezeHitboxes : Module
{
    public override bool DefaultEnabled => false;
    public override bool Hidden => true;
    public override bool AlwaysEnabled => true;

    private static FreezeHitboxesSettings Settings => GodhomeQoL.GlobalSettings.FreezeHitboxes ??= new FreezeHitboxesSettings();

    private static bool freezeActive;
    private static int freezeTimeScaleLockHandle;
    private static float storedGenericTimeScale = 1f;
    private static float lastNonZeroGenericTimeScale = 1f;
    private static bool pendingHitFreeze;
    private static bool pendingHitForceFreeze;
    private static int pendingHitBaselineTotalHealth = -1;
    private static int pendingHitFrame;
    private static int lastTakeHealthBaselineTotalHealth = -1;
    private static FreezeHitboxesViewer? hitboxViewer;

    private static void ResetPendingHitState()
    {
        pendingHitFreeze = false;
        pendingHitForceFreeze = false;
        pendingHitBaselineTotalHealth = -1;
        pendingHitFrame = 0;
        lastTakeHealthBaselineTotalHealth = -1;
    }

    internal static bool GetEnabled() => Settings.Enabled;

    internal static void SetEnabled(bool value)
    {
        if (Settings.Enabled == value)
        {
            if (!value)
            {
                ResetPendingHitState();
                EndFreeze();
            }
            return;
        }

        Settings.Enabled = value;
        GodhomeQoL.SaveGlobalSettingsSafe();
        ResetPendingHitState();

        if (!value)
        {
            EndFreeze();
        }
    }

    private protected override void Load()
    {
        On.GameManager.SetTimeScale_float += OnGameManagerSetTimeScale;
        ModHooks.TakeHealthHook += OnTakeHealthPreDamage;
        ModHooks.AfterTakeDamageHook += OnAfterTakeDamage;
        ModHooks.HeroUpdateHook += OnHeroUpdate;
        On.InputHandler.OnGUI += OnInputHandlerOnGUI;
        On.HeroController.Die += OnHeroDie;
        FreezeHitSource.Hook();
    }

    private protected override void Unload()
    {
        On.GameManager.SetTimeScale_float -= OnGameManagerSetTimeScale;
        ModHooks.TakeHealthHook -= OnTakeHealthPreDamage;
        ModHooks.AfterTakeDamageHook -= OnAfterTakeDamage;
        ModHooks.HeroUpdateHook -= OnHeroUpdate;
        On.InputHandler.OnGUI -= OnInputHandlerOnGUI;
        On.HeroController.Die -= OnHeroDie;
        FreezeHitSource.Unhook();
        ResetPendingHitState();
        EndFreeze();
    }

    private static int OnTakeHealthPreDamage(int damageAmount)
    {
        if (!Settings.Enabled || freezeActive || !Settings.AnyHits)
        {
            return damageAmount;
        }

        if (damageAmount <= 0)
        {
            lastTakeHealthBaselineTotalHealth = -1;
            return damageAmount;
        }

        int totalHealth = GetTotalHealth();
        if (totalHealth >= 0)
        {
            lastTakeHealthBaselineTotalHealth = totalHealth;
        }

        return damageAmount;
    }

    internal static bool GetAnyHitsMode() => Settings.AnyHits;
    internal static bool IsFreezeActive() => freezeActive;
    internal static bool ShouldBlockDeathTimeScaleNormalization() =>
        freezeActive || (Settings.Enabled && !Settings.AnyHits);

    internal static void SetAnyHitsMode(bool value)
    {
        if (Settings.AnyHits == value)
        {
            return;
        }

        Settings.AnyHits = value;
        GodhomeQoL.SaveGlobalSettingsSafe();
        ResetPendingHitState();
    }

    internal static string GetUnfreezeKeybind() => Settings.UnfreezeKeybind ?? string.Empty;

    internal static void SetUnfreezeKeybind(string value)
    {
        Settings.UnfreezeKeybind = value;
        GodhomeQoL.SaveGlobalSettingsSafe();
    }

    internal static KeyCode GetUnfreezeKey()
    {
        string raw = Settings.UnfreezeKeybind ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return KeyCode.None;
        }

        return Enum.TryParse(raw, true, out KeyCode key) ? key : KeyCode.None;
    }

    private static void OnGameManagerSetTimeScale(On.GameManager.orig_SetTimeScale_float orig, GameManager self, float newTimeScale)
    {
        if (!Settings.Enabled)
        {
            orig(self, newTimeScale);
            return;
        }

        if (freezeActive)
        {
            if (newTimeScale > 0.01f)
            {
                storedGenericTimeScale = newTimeScale;
            }

            TimeController.GenericTimeScale = 0f;
            return;
        }

        if (TimeController.GenericTimeScale > 0f)
        {
            lastNonZeroGenericTimeScale = TimeController.GenericTimeScale;
        }

        orig(self, newTimeScale);
    }

    private static int OnAfterTakeDamage(int hazardType, int damageAmount)
    {
        if (!Settings.Enabled)
        {
            lastTakeHealthBaselineTotalHealth = -1;
            return damageAmount;
        }

        if (freezeActive)
        {
            lastTakeHealthBaselineTotalHealth = -1;
            return damageAmount;
        }

        if (Settings.AnyHits && IsDeathOrRespawnState(ignoreControlReqlinquished: true))
        {
            lastTakeHealthBaselineTotalHealth = -1;
            return damageAmount;
        }

        if (Settings.AnyHits)
        {
            int baseline = lastTakeHealthBaselineTotalHealth;
            if (damageAmount <= 0)
            {
                pendingHitFreeze = false;
                pendingHitForceFreeze = false;
                pendingHitBaselineTotalHealth = -1;
                lastTakeHealthBaselineTotalHealth = -1;
                return damageAmount;
            }

            pendingHitFreeze = true;
            pendingHitForceFreeze = baseline < 0;
            pendingHitBaselineTotalHealth = baseline;
            pendingHitFrame = Time.frameCount;
            lastTakeHealthBaselineTotalHealth = -1;
            return damageAmount;
        }

        if (IsPlayerDead())
        {
            StartFreeze();
        }

        lastTakeHealthBaselineTotalHealth = -1;
        return damageAmount;
    }

    private static void OnInputHandlerOnGUI(On.InputHandler.orig_OnGUI orig, InputHandler self)
    {
        orig(self);
        if (!Settings.Enabled)
        {
            return;
        }

        if (QuickMenu.IsHotkeyInputBlocked())
        {
            return;
        }

        if (QuickMenu.IsAnyUiVisible())
        {
            return;
        }

        if (!freezeActive)
        {
            return;
        }

        KeyCode key = GetUnfreezeKey();
        if (key != KeyCode.None && Input.GetKeyDown(key))
        {
            EndFreeze();
        }
    }

    private static void OnHeroUpdate()
    {
        if (!Settings.Enabled)
        {
            return;
        }

        if (!pendingHitFreeze || freezeActive || !Settings.AnyHits)
        {
            return;
        }

        if (Time.frameCount == pendingHitFrame)
        {
            return;
        }

        pendingHitFreeze = false;
        if (pendingHitForceFreeze)
        {
            pendingHitForceFreeze = false;
            pendingHitBaselineTotalHealth = -1;
            StartFreeze();
            return;
        }

        if (pendingHitBaselineTotalHealth < 0)
        {
            return;
        }

        int currentTotalHealthAfterDamage = GetTotalHealth();
        if (currentTotalHealthAfterDamage >= 0 && currentTotalHealthAfterDamage < pendingHitBaselineTotalHealth)
        {
            StartFreeze();
        }

        pendingHitBaselineTotalHealth = -1;
    }

    private static int GetTotalHealth()
    {
        PlayerData? pd = PlayerData.instance;
        if (pd == null)
        {
            return -1;
        }

        int baseHealth = Math.Max(0, pd.health);
        int blueHealth = Math.Max(0, pd.healthBlue);
        return baseHealth + blueHealth;
    }

    private static bool IsPlayerDead()
    {
        if (HeroController.instance != null && HeroController.instance.cState.dead)
        {
            return true;
        }

        return PlayerData.instance != null && PlayerData.instance.health <= 0;
    }


    private static bool IsDeathOrRespawnState(bool ignoreControlReqlinquished = false)
    {
        GameManager? gm = GameManager.instance;
        if (gm == null || gm.gameState != GameState.PLAYING || gm.IsInSceneTransition)
        {
            return true;
        }

        HeroController? hero = HeroController.instance;
        if (hero == null || hero.cState.dead || (!ignoreControlReqlinquished && hero.controlReqlinquished))
        {
            return true;
        }

        return PlayerData.instance != null && PlayerData.instance.health <= 0;
    }

    private static IEnumerator OnHeroDie(On.HeroController.orig_Die orig, HeroController self)
    {
        if (Settings.Enabled && !freezeActive && !Settings.AnyHits)
        {
            StartFreeze();
        }

        IEnumerator origEnum = orig(self);
        while (origEnum.MoveNext())
        {
            yield return origEnum.Current;
        }
    }

    private static void StartFreeze()
    {
        if (freezeActive)
        {
            return;
        }

        freezeActive = true;
        freezeTimeScaleLockHandle = SpeedChanger.BeginTimeScaleFreezeLock();
        storedGenericTimeScale = TimeController.GenericTimeScale > 0f
            ? TimeController.GenericTimeScale
            : lastNonZeroGenericTimeScale;

        TimeController.GenericTimeScale = 0f;
        FreezeHitSource.Freeze();

        EnsureViewer();
        hitboxViewer?.Load();
    }

    private static void EndFreeze()
    {
        if (!freezeActive)
        {
            return;
        }

        freezeActive = false;
        if (freezeTimeScaleLockHandle != 0)
        {
            SpeedChanger.EndTimeScaleFreezeLock(freezeTimeScaleLockHandle);
            freezeTimeScaleLockHandle = 0;
        }

        if (storedGenericTimeScale <= 0f)
        {
            storedGenericTimeScale = lastNonZeroGenericTimeScale > 0f ? lastNonZeroGenericTimeScale : 1f;
        }

        TimeController.GenericTimeScale = storedGenericTimeScale;
        ResetPendingHitState();
        FreezeHitSource.Clear();

        hitboxViewer?.Unload();
    }

    private static void EnsureViewer()
    {
        hitboxViewer ??= new FreezeHitboxesViewer();
    }
}
