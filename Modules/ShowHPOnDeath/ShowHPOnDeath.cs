using InControl;
using Satchel.BetterMenus;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class ShowHPOnDeath : Module
{
    public override bool DefaultEnabled => false;
    public override bool Hidden => true;
    public override bool AlwaysEnabled => true;

    private sealed class BossState
    {
        public string DisplayName = "";
        public int CurrentHP;
    }

    private static readonly Dictionary<string, BossState> trackedBossesByKey = [];
    private static readonly List<string> trackedBossOrder = [];
    private static readonly Dictionary<int, string> bossKeyByInstanceId = [];
    private static readonly Dictionary<string, int> bossSignatureCounts = [];
    private static readonly Dictionary<string, int> initialHPs = [];
    private static readonly Dictionary<string, int> personalBestByBoss = [];
    private static ShowHPOnDeath? activeInstance;
    private static bool shouldTrackBosses = false;
    private static bool enteredFromWorkshop = false;
    private static string trackedScene = "";
    private static int trackedSceneHandle = -1;
    private static bool pendingTrackingStart = false;
    private static string pendingTrackingScene = "";
    private static bool pendingTrackingFromWorkshop = false;
    private static bool pendingTrackingClearDisplay = true;
    private static bool hasPendingDisplay = false;
    private static string pendingDisplayText = "";
    private static string lastDisplayText = "";
    private static bool isHudVisible = false;
    private static int trackingGeneration;
    private static int autoHideGeneration;
    private bool runtimeHooksInstalled;

    private static readonly HashSet<string> showAtScenes = new() { "GG_Workshop", "GG_Atrium", "GG_Atrium_Roof" };
    private static readonly HashSet<string> phaseBosses = new() { "False Knight", "Failed Champion" };

    private sealed class PhaseTracker
    {
        public int CurrentPhase;
        public int[] MaxHP = new int[3];
        public int[] CurrentHP = new int[3];
        public bool[] PhaseDamaged = new bool[3];
        public int LastHP = -1;
    }

    private static readonly Dictionary<string, PhaseTracker> phaseTrackers = new();

    private static readonly HashSet<string> allowedBossScenes = new()
    {
        "GG_Gruz_Mother",
        "GG_Gruz_Mother_V",
        "GG_Vengefly",
        "GG_Vengefly_V",
        "GG_Brooding_Mawlek",
        "GG_Brooding_Mawlek_V",
        "GG_False_Knight",
        "GG_Failed_Champion",
        "GG_Hornet_1",
        "GG_Hornet_2",
        "GG_Mega_Moss_Charger",
        "GG_Flukemarm",
        "GG_Mantis_Lords",
        "GG_Mantis_Lords_V",
        "GG_Oblobbles",
        "GG_Hive_Knight",
        "GG_Broken_Vessel",
        "GG_Lost_Kin",
        "GG_Nosk",
        "GG_Nosk_V",
        "GG_Nosk_Hornet",
        "GG_Collector",
        "GG_Collector_V",
        "GG_God_Tamer",
        "GG_Crystal_Guardian",
        "GG_Crystal_Guardian_2",
        "GG_Uumuu",
        "GG_Uumuu_V",
        "GG_Traitor_Lord",
        "GG_Grey_Prince_Zote",
        "GG_Mage_Knight",
        "GG_Mage_Knight_V",
        "GG_Soul_Master",
        "GG_Soul_Tyrant",
        "GG_Dung_Defender",
        "GG_White_Defender",
        "GG_Watcher_Knights",
        "GG_Ghost_No_Eyes",
        "GG_Ghost_No_Eyes_V",
        "GG_Ghost_Marmu",
        "GG_Ghost_Marmu_V",
        "GG_Ghost_Xero",
        "GG_Ghost_Xero_V",
        "GG_Ghost_Markoth",
        "GG_Ghost_Markoth_V",
        "GG_Ghost_Galien",
        "GG_Ghost_Gorb",
        "GG_Ghost_Gorb_V",
        "GG_Ghost_Hu",
        "GG_Nailmasters",
        "GG_Painter",
        "GG_Sly",
        "GG_Hollow_Knight",
        "GG_Grimm",
        "GG_Grimm_Nightmare",
        "GG_Radiance"
    };

    private static ShowHPOnDeathSettings Settings => GodhomeQoL.GlobalSettings.ShowHPOnDeath ??= new ShowHPOnDeathSettings();
}
