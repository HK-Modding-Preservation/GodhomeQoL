namespace GodhomeQoL.Settings;

public sealed class PerSaveMirror
{
    public string Token { get; set; } = string.Empty;

    public Dictionary<string, object> Values { get; set; } = new();

    public Dictionary<string, bool> Modules { get; set; } = new();
}
