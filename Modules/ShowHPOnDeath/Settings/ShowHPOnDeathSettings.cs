using InControl;
using Modding.Converters;
using Newtonsoft.Json;

namespace GodhomeQoL.Settings;

public sealed class ShowHPOnDeathSettings
{
    public bool EnabledMod = false;
    public bool ShowPB = true;
    public bool HideAfter10Sec = true;
    public float HudFadeSeconds = 5f;

    [JsonConverter(typeof(PlayerActionSetConverter))]
    public ShowHPOnDeathKeyBinds Keybinds = new();
}

public sealed class ShowHPOnDeathKeyBinds : PlayerActionSet
{
    public PlayerAction Hide;
    public PlayerAction History;

    public ShowHPOnDeathKeyBinds()
    {
        Hide = CreatePlayerAction("Hide");
        History = CreatePlayerAction("History");
    }
}
