using System.Collections.Generic;
using System.Linq;
using Satchel.Futils;
using Vasi;
using Random = System.Random;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed partial class RandomPantheons : Module
{
    internal static RandomPantheons? Instance;

    [GlobalSetting] public static bool Pantheon1Enabled = false;
    [GlobalSetting] public static bool Pantheon2Enabled = false;
    [GlobalSetting] public static bool Pantheon3Enabled = false;
    [GlobalSetting] public static bool Pantheon4Enabled = false;
    [GlobalSetting] public static bool Pantheon5Enabled = false;

    internal static bool AnyPantheonEnabled =>
        Pantheon1Enabled || Pantheon2Enabled || Pantheon3Enabled || Pantheon4Enabled || Pantheon5Enabled;

    public override bool DefaultEnabled => false;

    private static readonly Dictionary<BossSequence, BossScene[]> OriginalSequences = new();
    private static readonly Dictionary<BossSequence, List<BossSequenceDoor>> SequenceDoors = new();
    private static readonly FieldInfo BossScenesField = typeof(BossSequence).GetField("bossScenes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
    private const int FirstSceneSyncRetryFrames = 120;
    private static readonly Dictionary<int, int> DoorFirstSceneSyncGenerations = new();

    private static readonly Dictionary<Pantheon, string[]> DefaultPantheonOrder = new()
    {
        {
            Pantheon.P1,
            new[]
            {
                "GG_Vengefly",
                "GG_Gruz_Mother",
                "GG_False_Knight",
                "GG_Mega_Moss_Charger",
                "GG_Hornet_1",
                "GG_Spa",
                "GG_Ghost_Gorb",
                "GG_Dung_Defender",
                "GG_Mage_Knight",
                "GG_Brooding_Mawlek",
                "GG_Engine",
                "GG_Nailmasters"
            }
        },
        {
            Pantheon.P2,
            new[]
            {
                "GG_Ghost_Xero",
                "GG_Crystal_Guardian",
                "GG_Soul_Master",
                "GG_Oblobbles",
                "GG_Mantis_Lords",
                "GG_Spa",
                "GG_Ghost_Marmu",
                "GG_Nosk",
                "GG_Flukemarm",
                "GG_Broken_Vessel",
                "GG_Engine",
                "GG_Painter"
            }
        },
        {
            Pantheon.P3,
            new[]
            {
                "GG_Hive_Knight",
                "GG_Ghost_Hu",
                "GG_Collector",
                "GG_God_Tamer",
                "GG_Grimm",
                "GG_Spa",
                "GG_Ghost_Galien",
                "GG_Grey_Prince_Zote",
                "GG_Uumuu",
                "GG_Hornet_2",
                "GG_Engine",
                "GG_Sly"
            }
        },
        {
            Pantheon.P4,
            new[]
            {
                "GG_Crystal_Guardian_2",
                "GG_Lost_Kin",
                "GG_Ghost_No_Eyes",
                "GG_Traitor_Lord",
                "GG_White_Defender",
                "GG_Spa",
                "GG_Failed_Champion",
                "GG_Ghost_Markoth",
                "GG_Watcher_Knights",
                "GG_Soul_Tyrant",
                "GG_Engine_Prime",
                "GG_Hollow_Knight"
            }
        },
        {
            Pantheon.P5,
            new[]
            {
                "GG_Vengefly_V",
                "GG_Gruz_Mother_V",
                "GG_False_Knight",
                "GG_Mega_Moss_Charger",
                "GG_Hornet_1",
                "GG_Engine",
                "GG_Ghost_Gorb_V",
                "GG_Dung_Defender",
                "GG_Mage_Knight_V",
                "GG_Brooding_Mawlek_V",
                "GG_Nailmasters",
                "GG_Spa",
                "GG_Ghost_Xero_V",
                "GG_Crystal_Guardian",
                "GG_Soul_Master",
                "GG_Oblobbles",
                "GG_Mantis_Lords_V",
                "GG_Spa",
                "GG_Ghost_Marmu_V",
                "GG_Flukemarm",
                "GG_Broken_Vessel",
                "GG_Ghost_Galien",
                "GG_Painter",
                "GG_Spa",
                "GG_Hive_Knight",
                "GG_Ghost_Hu",
                "GG_Collector_V",
                "GG_God_Tamer",
                "GG_Grimm",
                "GG_Spa",
                "GG_Unn",
                "GG_Watcher_Knights",
                "GG_Uumuu_V",
                "GG_Nosk_Hornet",
                "GG_Sly",
                "GG_Hornet_2",
                "GG_Spa",
                "GG_Crystal_Guardian_2",
                "GG_Lost_Kin",
                "GG_Ghost_No_Eyes_V",
                "GG_Traitor_Lord",
                "GG_White_Defender",
                "GG_Spa",
                "GG_Engine_Root",
                "GG_Soul_Tyrant",
                "GG_Ghost_Markoth_V",
                "GG_Grey_Prince_Zote",
                "GG_Failed_Champion",
                "GG_Grimm_Nightmare",
                "GG_Spa",
                "GG_Wyrm",
                "GG_Hollow_Knight",
                "GG_Radiance"
            }
        }
    };

    private static readonly HashSet<string> InvalidFirst = new(StringComparer.OrdinalIgnoreCase)
    {
        "GG_Unn",
        "GG_Wyrm",
        "GG_Engine",
        "GG_Engine_Prime",
        "GG_Engine_Root",
        "GG_Spa",
        "GG_Gruz_Mother",
        "GG_Gruz_Mother_V",
        "GG_Crystal_Guardian"
    };

    private static readonly HashSet<string> InvalidLast = new(StringComparer.OrdinalIgnoreCase)
    {
        "GG_Unn",
        "GG_Wyrm",
        "GG_Engine",
        "GG_Engine_Prime",
        "GG_Engine_Root",
        "GG_Spa"
    };

    private static readonly HashSet<string> VanishedHud = new(StringComparer.OrdinalIgnoreCase)
    {
        "GG_Hollow_Knight",
        "GG_Radiance"
    };

    private readonly Random rand = new();

    private enum Pantheon
    {
        Unknown,
        P1,
        P2,
        P3,
        P4,
        P5
    }
}
