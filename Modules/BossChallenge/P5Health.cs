using MonoMod.RuntimeDetour;
using Satchel;

namespace GodhomeQoL.Modules.BossChallenge;

public sealed class P5Health : Module {
	private static readonly Detour levelGetterDetour = new(
		Info.OfPropertyGet<BossSceneController>(nameof(BossSceneController.BossLevel)),
		Info.OfMethod<P5Health>(nameof(OverrideLevel)),
		new() { ManualApply = true }
	);

	private static readonly SceneEdit ascendedMarkothHandle = new(
		new("GG_Markoth_V", "Warrior", "Ghost Warrior Markoth"),
		go => go.manageHealth(650)
	);

	internal static bool IsActive { get; private set; }
	internal static int LastOriginalBossLevel { get; private set; } = -1;
	internal static bool HasOriginalBossLevel { get; private set; }

	public override ToggleableLevel ToggleableLevel => ToggleableLevel.ChangeScene;

	private protected override void Load() {
		IsActive = true;
		LastOriginalBossLevel = -1;
		HasOriginalBossLevel = false;
		levelGetterDetour.Apply();
		ascendedMarkothHandle.Enable();

		On.BossSceneController.Start += CaptureOriginalBossLevel;
		ModHooks.TakeDamageHook += FixDamage;
	}

	private protected override void Unload() {
		IsActive = false;
		LastOriginalBossLevel = -1;
		HasOriginalBossLevel = false;
		levelGetterDetour.Undo();
		ascendedMarkothHandle.Disable();

		On.BossSceneController.Start -= CaptureOriginalBossLevel;
		ModHooks.TakeDamageHook -= FixDamage;
	}

	private static int OverrideLevel(BossSceneController _1, int _2) {
		LastOriginalBossLevel = _2;
		HasOriginalBossLevel = true;
		return 0;
	}

	private static IEnumerator CaptureOriginalBossLevel(On.BossSceneController.orig_Start orig, BossSceneController self)
	{
		yield return orig(self);

		if (!BossSceneController.IsBossScene)
		{
			yield break;
		}

		const int maxFrames = 20;
		for (int i = 0; i < maxFrames; i++)
		{
			int candidate = self.Reflect().bossLevel;
			if (candidate > 0)
			{
				LastOriginalBossLevel = candidate;
				HasOriginalBossLevel = true;
				yield break;
			}

			yield return null;
		}

		int fallback = self.Reflect().bossLevel;
		if (fallback >= 0)
		{
			LastOriginalBossLevel = fallback;
			HasOriginalBossLevel = true;
		}
	}

	private static int FixDamage(ref int hazardType, int damage) => damage switch {
		int i when i <= 0 => i,
		int i when BossSceneController.IsBossScene is false => i,
		int i => BossSceneController.Instance.Reflect().bossLevel switch {
			1 => i * 2,
			2 => 9999,
			_ => i,
		}
	};
}
