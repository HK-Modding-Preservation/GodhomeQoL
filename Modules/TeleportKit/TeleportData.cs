namespace GodhomeQoL.Modules.QoL;

internal sealed class TeleportData
{
    internal readonly (int id, string name, Vector3 position, string scene)[] Bosses =
    {
        (1, "Gruz Mother", new Vector3(28.03279f, 6.40824f, 0f), "GG_Workshop"),
        (2, "Vengefly King", new Vector3(36.43205f, 6.408124f, 0f), "GG_Workshop"),
        (3, "Brooding Mawlek", new Vector3(43.84856f, 6.408124f, 0f), "GG_Workshop"),
        (4, "False Knight/Failed Champion", new Vector3(52.05054f, 6.408124f, 0f), "GG_Workshop"),
        (5, "Hornet Protector/Hornet Sentinel", new Vector3(60.69859f, 6.408124f, 0f), "GG_Workshop"),
        (6, "Massive Moss Charger", new Vector3(71.1746f, 6.408124f, 0f), "GG_Workshop"),
        (7, "Flukemarm", new Vector3(81.26862f, 6.408124f, 0f), "GG_Workshop"),
        (8, "Mantis Lords/Sisters of Battle", new Vector3(90.92618f, 6.408124f, 0f), "GG_Workshop"),
        (9, "Oblobbles", new Vector3(100.5547f, 6.408124f, 0f), "GG_Workshop"),
        (10, "Hive Knight", new Vector3(111.3527f, 6.408124f, 0f), "GG_Workshop"),
        (11, "Broken Vessel/Lost Kin", new Vector3(119.6147f, 6.408124f, 0f), "GG_Workshop"),
        (12, "Nosk/Winged Nosk", new Vector3(129.0463f, 6.408124f, 0f), "GG_Workshop"),
        (13, "The Collector", new Vector3(139.9184f, 6.408124f, 0f), "GG_Workshop"),
        (14, "God Tamer", new Vector3(152.4324f, 6.408124f, 0f), "GG_Workshop"),
        (15, "Crystal Guardian/Enraged Guardian", new Vector3(163.0346f, 6.408124f, 0f), "GG_Workshop"),
        (16, "Uumuu", new Vector3(175.5506f, 6.408124f, 0f), "GG_Workshop"),
        (17, "Traitor Lord", new Vector3(186.6932f, 6.408124f, 0f), "GG_Workshop"),
        (18, "Grey Prince Zote", new Vector3(197.9937f, 6.408124f, 0f), "GG_Workshop"),

        (19, "Soul Warrior", new Vector3(33.99411f, 36.40812f, 0f), "GG_Workshop"),
        (20, "Soul Master/Soul Tyrant", new Vector3(44.98816f, 36.40812f, 0f), "GG_Workshop"),
        (21, "Dung Defender/White Defender", new Vector3(57.45609f, 36.40812f, 0f), "GG_Workshop"),
        (22, "Watcher Knights", new Vector3(69.87215f, 36.40812f, 0f), "GG_Workshop"),
        (23, "No Eyes", new Vector3(82.36407f, 36.40812f, 0f), "GG_Workshop"),
        (24, "Marmu", new Vector3(91.36407f, 36.40812f, 0f), "GG_Workshop"),
        (25, "Xero", new Vector3(100.3461f, 36.40812f, 0f), "GG_Workshop"),
        (26, "Markoth", new Vector3(108.8942f, 36.40812f, 0f), "GG_Workshop"),
        (27, "Galien", new Vector3(117.8981f, 36.40812f, 0f), "GG_Workshop"),
        (28, "Gorb", new Vector3(126.3222f, 36.40812f, 0f), "GG_Workshop"),
        (29, "Elder Hu", new Vector3(134.6922f, 36.40812f, 0f), "GG_Workshop"),
        (30, "Brothers Oro & Mato", new Vector3(146.4543f, 36.40812f, 0f), "GG_Workshop"),
        (31, "Paintmaster Sheo", new Vector3(161.4882f, 36.40812f, 0f), "GG_Workshop"),
        (32, "Nailmaster Sly", new Vector3(172.1839f, 36.40812f, 0f), "GG_Workshop"),
        (33, "Pure Vessel", new Vector3(183.3f, 36.4f, 0f), "GG_Workshop"),
        (34, "Grimm/Nightmare King", new Vector3(193.3637f, 36.40812f, 0f), "GG_Workshop"),
        (35, "Absolute Radiance", new Vector3(248.4834f, 36.40812f, 0f), "GG_Workshop"),
        (36, "The Eternal Ordeal", new Vector3(197.8246f, 63.40812f, 0f), "GG_Workshop"),
        (37, "HoG Bench", new Vector3(11.0521f, 36.40812f, 0f), "GG_Workshop"),

        (38, "Pantheon I", new Vector3(97.15343f, 35.40812f, 0f), "GG_Atrium"),
        (39, "Pantheon II", new Vector3(108.4116f, 35.40812f, 0f), "GG_Atrium"),
        (40, "Pantheon III", new Vector3(120.2336f, 35.40812f, 0f), "GG_Atrium"),
        (41, "Pantheon IV", new Vector3(147.3174f, 35.40812f, 0f), "GG_Atrium"),
            (42, "Pantheon V", new Vector3(96.971f, 73.408f, 0f), "GG_Atrium_Roof"),
            (43, "Segmented Pantheon V", new Vector3(53.81337f, 19.40812f, 0f), "GG_Atrium_Roof"),
            (44, "Pantheon Bench", new Vector3(120.97f, 42.40812f, 0f), "GG_Atrium_Roof"),

            (45, "PoP Start", new Vector3(9.735f, 7.408f, 0f), "White_Palace_06"),
        (46, "PoP End", new Vector3(19.5625f, 169.4081f, 0f), "White_Palace_20")
    };

    internal Vector3? CustomTeleportPosition { get; set; }
    internal string? CustomTeleportScene { get; set; }
}
