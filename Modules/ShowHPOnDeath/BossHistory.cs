using System.IO;

namespace GodhomeQoL.Modules.Tools;

internal sealed class BossHistoryPart
{
    public string Name = "";
    public int Hp;
    public int Max;

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? Phase;
}

internal sealed class BossHistoryEntry
{
    public DateTime Time;
    public int Level = -1;
    public int Pantheon;
    public int PantheonBoss;
    public bool Victory;
    public List<BossHistoryPart> Parts = new();

    [JsonIgnore]
    public int TotalHp => Parts.Sum(part => Math.Max(part.Hp, 0));

    [JsonIgnore]
    public int TotalMax => Parts.Sum(part => Math.Max(part.Max, 0));

    [JsonIgnore]
    public float Ratio => Victory ? 0f : TotalMax > 0 ? (float)TotalHp / TotalMax : 1f;
}

internal sealed class BossHistoryFile
{
    public int Version = 1;
    public Dictionary<string, List<BossHistoryEntry>> Bosses = new(StringComparer.Ordinal);
}

internal sealed class BossHistoryBoss
{
    internal BossHistoryBoss(string key, string image, params string[] scenes)
    {
        Key = key;
        Image = image;
        Scenes = scenes;
    }

    internal string Key { get; }

    internal string Image { get; }

    internal string[] Scenes { get; }
}

internal static class BossHistory
{
    internal const int MaxEntriesPerBoss = 400;

    internal static readonly BossHistoryBoss[] Bosses =
    {
        new("Gruz Mother", "Gruz Mother.png", "GG_Gruz_Mother", "GG_Gruz_Mother_V"),
        new("Vengefly King", "Vengefly King.png", "GG_Vengefly", "GG_Vengefly_V"),
        new("Brooding Mawlek", "Brooding Mawlek.png", "GG_Brooding_Mawlek", "GG_Brooding_Mawlek_V"),
        new("False Knight", "False Knight.png", "GG_False_Knight"),
        new("Failed Champion", "Failed Champion.png", "GG_Failed_Champion"),
        new("Hornet Protector", "Hornet Protector.png", "GG_Hornet_1"),
        new("Hornet Sentinel", "Hornet Sentinel.png", "GG_Hornet_2"),
        new("Massive Moss Charger", "Massive Moss Charger.png", "GG_Mega_Moss_Charger"),
        new("Flukemarm", "Flukemarm.png", "GG_Flukemarm"),
        new("Mantis Lords", "Mantis Lords.png", "GG_Mantis_Lords"),
        new("Sisters of Battle", "Sisters of Battle.png", "GG_Mantis_Lords_V"),
        new("Oblobbles", "Oblobbles.png", "GG_Oblobbles"),
        new("Hive Knight", "Hive Knight.png", "GG_Hive_Knight"),
        new("Broken Vessel", "Broken Vessel.png", "GG_Broken_Vessel"),
        new("Lost Kin", "Lost Kin.png", "GG_Lost_Kin"),
        new("Nosk", "Nosk.png", "GG_Nosk", "GG_Nosk_V"),
        new("Winged Nosk", "Winged Nosk.png", "GG_Nosk_Hornet"),
        new("The Collector", "The Collector.png", "GG_Collector", "GG_Collector_V"),
        new("God Tamer", "God Tamer.png", "GG_God_Tamer"),
        new("Crystal Guardian", "Crystal Guardian.png", "GG_Crystal_Guardian"),
        new("Enraged Guardian", "Enraged Guardian.png", "GG_Crystal_Guardian_2"),
        new("Uumuu", "Uumuu.png", "GG_Uumuu", "GG_Uumuu_V"),
        new("Traitor Lord", "Traitor Lord.png", "GG_Traitor_Lord"),
        new("Grey Prince Zote", "Grey Prince Zote.png", "GG_Grey_Prince_Zote"),
        new("Soul Warrior", "Soul Warrior.png", "GG_Mage_Knight", "GG_Mage_Knight_V"),
        new("Soul Master", "Soul Master.png", "GG_Soul_Master"),
        new("Soul Tyrant", "Soul Tyrant.png", "GG_Soul_Tyrant"),
        new("Dung Defender", "Dung Defender.png", "GG_Dung_Defender"),
        new("White Defender", "White Defender.png", "GG_White_Defender"),
        new("Watcher Knights", "Watcher Knight.png", "GG_Watcher_Knights"),
        new("No Eyes", "No Eyes.png", "GG_Ghost_No_Eyes", "GG_Ghost_No_Eyes_V"),
        new("Marmu", "Marmu.png", "GG_Ghost_Marmu", "GG_Ghost_Marmu_V"),
        new("Xero", "Xero.png", "GG_Ghost_Xero", "GG_Ghost_Xero_V"),
        new("Markoth", "Markoth.png", "GG_Ghost_Markoth", "GG_Ghost_Markoth_V"),
        new("Galien", "Galien.png", "GG_Ghost_Galien"),
        new("Gorb", "Gorb.png", "GG_Ghost_Gorb", "GG_Ghost_Gorb_V"),
        new("Elder Hu", "Elder Hu.png", "GG_Ghost_Hu"),
        new("Oro & Mato", "Oro & Mato.png", "GG_Nailmasters"),
        new("Paintmaster Sheo", "Paintmaster Sheo.png", "GG_Painter"),
        new("Nailsage Sly", "Great Nailsage Sly.png", "GG_Sly"),
        new("Pure Vessel", "Pure Vessel.png", "GG_Hollow_Knight"),
        new("Grimm", "Grimm.png", "GG_Grimm"),
        new("Nightmare King Grimm", "Nightmare King.png", "GG_Grimm_Nightmare"),
        new("Absolute Radiance", "Absolute Radiance.png", "GG_Radiance")
    };

    private static readonly Dictionary<string, string> bossByScene = Bosses
        .SelectMany(boss => boss.Scenes.Select(scene => (scene, boss.Key)))
        .ToDictionary(pair => pair.scene, pair => pair.Key, StringComparer.Ordinal);

    private static BossHistoryFile? loaded;
    private static int loadedSlot = -1;

    internal static int CurrentSlot
    {
        get
        {
            GameManager? manager = GameManager.instance;
            return manager != null && manager.profileID > 0 && manager.IsGameplayScene() ? manager.profileID : 0;
        }
    }

    internal static bool TryGetBossForScene(string scene, out string key) => bossByScene.TryGetValue(scene ?? string.Empty, out key);

    internal static BossHistoryBoss? Find(string key) => Bosses.FirstOrDefault(boss => string.Equals(boss.Key, key, StringComparison.Ordinal));

    internal static List<BossHistoryEntry> GetEntries(string key)
    {
        BossHistoryFile? file = Load(CurrentSlot);
        return file != null && file.Bosses.TryGetValue(key, out List<BossHistoryEntry>? entries) ? entries : new List<BossHistoryEntry>();
    }

    internal static void Add(string key, BossHistoryEntry entry)
    {
        int slot = CurrentSlot;
        BossHistoryFile? file = Load(slot);
        if (file == null)
        {
            return;
        }

        if (!file.Bosses.TryGetValue(key, out List<BossHistoryEntry>? entries) || entries == null)
        {
            entries = new List<BossHistoryEntry>();
            file.Bosses[key] = entries;
        }

        entries.Insert(0, entry);
        if (entries.Count > MaxEntriesPerBoss)
        {
            entries.RemoveRange(MaxEntriesPerBoss, entries.Count - MaxEntriesPerBoss);
        }

        Save(slot, file);
    }

    internal static void Remove(string key, BossHistoryEntry entry)
    {
        int slot = CurrentSlot;
        BossHistoryFile? file = Load(slot);
        if (file == null || !file.Bosses.TryGetValue(key, out List<BossHistoryEntry>? entries) || !entries.Remove(entry))
        {
            return;
        }

        if (entries.Count == 0)
        {
            file.Bosses.Remove(key);
        }

        Save(slot, file);
    }

    internal static void ClearBoss(string key)
    {
        int slot = CurrentSlot;
        BossHistoryFile? file = Load(slot);
        if (file != null && file.Bosses.Remove(key))
        {
            Save(slot, file);
        }
    }

    internal static void ClearAll()
    {
        int slot = CurrentSlot;
        BossHistoryFile? file = Load(slot);
        if (file != null && file.Bosses.Count > 0)
        {
            file.Bosses.Clear();
            Save(slot, file);
        }
    }

    internal static void DeleteSlot(int slot)
    {
        if (slot <= 0)
        {
            return;
        }

        if (slot == loadedSlot)
        {
            loaded = null;
            loadedSlot = -1;
        }

        try
        {
            string path = GetPath(slot);
            if (File.Exists(path))
            {
                File.Delete(path);
                LogDebug($"Boss history of save {slot} deleted");
            }
        }
        catch (Exception ex)
        {
            LogError($"Boss history of save {slot} could not be deleted: {ex.Message}");
        }
    }

    private static string GetPath(int slot) => Path.Combine(Application.persistentDataPath, $"{ModInfo.Name}.BossHistory.user{slot}.json");

    private static BossHistoryFile? Load(int slot)
    {
        if (slot <= 0)
        {
            return null;
        }

        if (slot == loadedSlot && loaded != null)
        {
            return loaded;
        }

        BossHistoryFile file = new();
        string path = GetPath(slot);
        try
        {
            if (File.Exists(path))
            {
                BossHistoryFile? read = JsonConvert.DeserializeObject<BossHistoryFile>(File.ReadAllText(path));
                if (read?.Bosses != null)
                {
                    file.Bosses = new Dictionary<string, List<BossHistoryEntry>>(read.Bosses.Where(pair => pair.Value != null).ToDictionary(pair => pair.Key, pair => pair.Value), StringComparer.Ordinal);
                }
            }
        }
        catch (Exception ex)
        {
            LogError($"Boss history of save {slot} could not be read: {ex.Message}");
            try
            {
                File.Copy(path, path + ".broken", true);
            }
            catch (Exception copyEx)
            {
                LogError($"Boss history backup failed: {copyEx.Message}");
            }
        }

        loaded = file;
        loadedSlot = slot;
        return file;
    }

    private static void Save(int slot, BossHistoryFile file)
    {
        string path = GetPath(slot);
        string temp = path + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonConvert.SerializeObject(file, Formatting.None));
            File.Copy(temp, path, true);
            File.Delete(temp);
        }
        catch (Exception ex)
        {
            LogError($"Boss history of save {slot} could not be saved: {ex.Message}");
        }
    }
}
