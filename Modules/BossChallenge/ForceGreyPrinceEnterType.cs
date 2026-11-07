using Satchel;
using Satchel.Futils;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed class ForceGreyPrinceEnterType : Module
{
	[GlobalSetting]
	public static EnterType gpzEnterType = EnterType.Off;

    private static readonly SceneEdit handle = new(
        new("GG_Grey_Prince_Zote", "Grey Prince"),
        go =>
        {
            PlayMakerFSM fsm = go.LocateMyFSM("Control");
            fsm.AddCustomAction(
                "Enter 1",
                () =>
                {
                    if (gpzEnterType == EnterType.Off)
                    {
                        return;
                    }

                    fsm.GetVariable<FsmBool>("Faced Zote").Value = gpzEnterType == EnterType.Short;
                }
            );

            LogDebug("Grey Prince enter type modified");
        }
    );

    public override bool DefaultEnabled => false;

    public override ToggleableLevel ToggleableLevel => ToggleableLevel.ChangeScene;

    private protected override void Load() => handle.Enable();

    private protected override void Unload() => handle.Disable();

	public enum EnterType
	{
		Off,
		Long,
		Short
	}
}
