namespace GodhomeQoL.Settings;

public sealed class SettingsPreset
{
    public string Name { get; set; } = string.Empty;

    public Dictionary<string, bool> Modules { get; set; } = new();

    public Dictionary<string, object> GlobalValues { get; set; } = new();

    public Dictionary<string, object> LocalValues { get; set; } = new();

    public Dictionary<string, string> Sections { get; set; } = new();
}
