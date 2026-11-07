using Satchel;
using Satchel.Futils;
using Satchel.BetterMenus;
using Osmi.FsmActions;
using Satchel.BetterMenus.Config;

namespace GodhomeQoL.Modules.QoL;

public sealed class FastSuperDash : Module {
	private static readonly GameObjectRef knightRef = new(GameObjectRef.DONT_DESTROY_ON_LOAD, "Knight");
	private static readonly string[] requiredStateNames = ["Wall Charge", "Ground Charge", "Left", "Right", "Dash Start", "Dashing", "Air Cancel", "Hit Wall"];
	private static readonly Dictionary<int, PatchedFsmSnapshot> patchedFsms = [];

	private sealed class PatchedFsmSnapshot {
		public PlayMakerFSM fsm = null!;
		public int fsmId;
		public List<(string stateName, FsmStateAction action)> addedActions = [];
	}

	private sealed class SuperDashPatchMarker : MonoBehaviour {
		public int patchedFsmId;
	}

	[GlobalSetting]
	[BoolOption]
	public static bool instantSuperDash = false;

	[GlobalSetting]
	[FloatOption(1.0f, 10.0f, 0.1f)]
	public static float fastSuperDashSpeedMultiplier = 1f;

	[GlobalSetting]
	[BoolOption]
	public static bool fastSuperDashEverywhere = false;

	public override bool DefaultEnabled => false;

	private protected override void Load() {
		patchedFsms.Clear();
		On.PlayMakerFSM.Start += ModifySuperDashFSM;
		TryPatchExistingSuperDashFSM();
	}

	private protected override void Unload() {
		On.PlayMakerFSM.Start -= ModifySuperDashFSM;
		RestorePatchedFsms();
	}

	private void ModifySuperDashFSM(On.PlayMakerFSM.orig_Start orig, PlayMakerFSM self) {
		orig(self);

		if (self.FsmName == "Superdash" && knightRef.MatchGameObject(self.gameObject)) {
			if (TryPatchSuperDashFSM(self)) {
				LogDebug("Superdash FSM modified");
			}
		}
	}

	private void TryPatchExistingSuperDashFSM() {
		GameObject? knight = GameObject.Find("Knight");
		if (knight == null) {
			return;
		}

		PlayMakerFSM? fsm = knight
			.GetComponents<PlayMakerFSM>()
			.FirstOrDefault(candidate => candidate != null && candidate.FsmName == "Superdash");
		if (fsm != null && TryPatchSuperDashFSM(fsm)) {
			LogDebug("Superdash FSM patched from existing knight instance");
		}
	}

	private static bool HasState(PlayMakerFSM fsm, string stateName) =>
		fsm.Fsm.GetState(stateName) != null;

	private static bool TryCreateSnapshot(PlayMakerFSM fsm, int fsmId, out PatchedFsmSnapshot snapshot) {
		snapshot = new PatchedFsmSnapshot {
			fsm = fsm,
			fsmId = fsmId
		};

		foreach (string stateName in requiredStateNames) {
			FsmState? state = fsm.Fsm.GetState(stateName);
			if (state == null) {
				return false;
			}
		}

		return true;
	}

	private static void RestorePatchedFsms() {
		foreach ((_, PatchedFsmSnapshot snapshot) in patchedFsms) {
			PlayMakerFSM fsm = snapshot.fsm;
			if (fsm == null) {
				continue;
			}

			foreach ((string stateName, FsmStateAction action) in snapshot.addedActions) {
				FsmState? state = fsm.Fsm.GetState(stateName);
				if (state == null || state.Actions == null) {
					continue;
				}

				List<FsmStateAction> actions = [.. state.Actions];
				_ = actions.RemoveAll(item => ReferenceEquals(item, action));
				state.Actions = [.. actions];
			}

			SuperDashPatchMarker? marker = fsm.gameObject.GetComponent<SuperDashPatchMarker>();
			if (marker != null && marker.patchedFsmId == snapshot.fsmId) {
				UObject.Destroy(marker);
			}
		}

		patchedFsms.Clear();
	}

	private bool TryPatchSuperDashFSM(PlayMakerFSM fsm) {
		int currentFsmId = fsm.GetInstanceID();
		if (patchedFsms.ContainsKey(currentFsmId)) {
			return false;
		}

		SuperDashPatchMarker? marker = fsm.gameObject.GetComponent<SuperDashPatchMarker>();
		if (marker != null && marker.patchedFsmId == currentFsmId) {
			return false;
		}

		if (!requiredStateNames.All(stateName => HasState(fsm, stateName))) {
			LogWarn("FastSuperDash: required states are missing, patch skipped.");
			return false;
		}

		FsmFloat? speedVar = fsm.GetVariable<FsmFloat>("Current SD Speed");
		if (speedVar == null) {
			LogWarn("FastSuperDash: missing \"Current SD Speed\" variable, patch skipped.");
			return false;
		}

		if (!TryCreateSnapshot(fsm, currentFsmId, out PatchedFsmSnapshot snapshot)) {
			LogWarn("FastSuperDash: failed to snapshot FSM state actions, patch skipped.");
			return false;
		}

		FsmFloat speed = speedVar;
		float currentDashBaseSpeed = speed.Value;
		bool dashBaseCaptured = false;

		bool shouldActivate() {
			if (!Loaded) {
				return false;
			}

			GameManager? manager = GameManager.instance;
			if (manager == null) {
				return false;
			}

			string scene = manager.sceneName ?? string.Empty;
			if (fastSuperDashEverywhere) {
				return !BossSceneController.IsBossScene
					&& scene is not ("Room_Colosseum_Bronze" or "Room_Colosseum_Silver" or "Room_Colosseum_Gold");
			}

			return scene is "GG_Workshop" or "GG_Atrium" or "GG_Atrium_Roof"
				|| (PlayerDataR.bossRushMode && scene == "Room_Colosseum_01");
		}
		bool shouldRemoveWinding() => shouldActivate() && instantSuperDash;
		void prepareDashBase() {
			dashBaseCaptured = false;
		}
		void applySpeedMultiplier() {
			if (!shouldActivate()) {
				return;
			}
			if (!dashBaseCaptured) {
				currentDashBaseSpeed = speed.Value;
				dashBaseCaptured = true;
			}

			float multiplier = Mathf.Max(0f, fastSuperDashSpeedMultiplier);
			speed.Value = currentDashBaseSpeed * multiplier;
		}

		FsmEvent skipEvent = FsmEvent.GetFsmEvent("WAIT");

		InvokeMethod wallChargeAction = new(() => {
			prepareDashBase();
			if (shouldRemoveWinding()) {
				fsm.SendEvent(skipEvent.Name);
			}
		});
		fsm.InsertAction("Wall Charge", wallChargeAction, 0);
		snapshot.addedActions.Add(("Wall Charge", wallChargeAction));

		InvokeMethod groundChargeAction = new(() => {
			prepareDashBase();
			if (shouldRemoveWinding()) {
				fsm.SendEvent(skipEvent.Name);
			}
		});
		fsm.InsertAction("Ground Charge", groundChargeAction, 0);
		snapshot.addedActions.Add(("Ground Charge", groundChargeAction));

		InvokeMethod dashStartResetAction = new(prepareDashBase);
		fsm.InsertAction("Dash Start", dashStartResetAction, 0);
		snapshot.addedActions.Add(("Dash Start", dashStartResetAction));

		InvokeMethod dashStartAction = new(applySpeedMultiplier);
		fsm.AddAction("Dash Start", dashStartAction);
		snapshot.addedActions.Add(("Dash Start", dashStartAction));

		InvokeMethod dashingSpeedAction = new(applySpeedMultiplier);
		fsm.AddAction("Dashing", dashingSpeedAction);
		snapshot.addedActions.Add(("Dashing", dashingSpeedAction));

		InvokePredicate dashingAction = new(shouldRemoveWinding) {
			trueEvent = skipEvent
		};
		fsm.AddAction("Dashing", dashingAction);
		snapshot.addedActions.Add(("Dashing", dashingAction));

		InvokePredicate airCancelAction = new(shouldRemoveWinding) {
			trueEvent = FsmEvent.Finished
		};
		fsm.AddAction("Air Cancel", airCancelAction);
		snapshot.addedActions.Add(("Air Cancel", airCancelAction));

		InvokePredicate hitWallAction = new(shouldRemoveWinding) {
			trueEvent = FsmEvent.Finished
		};
		fsm.AddAction("Hit Wall", hitWallAction);
		snapshot.addedActions.Add(("Hit Wall", hitWallAction));

		marker ??= fsm.gameObject.AddComponent<SuperDashPatchMarker>();
		marker.patchedFsmId = currentFsmId;
		patchedFsms[currentFsmId] = snapshot;
		return true;
	}

	internal static IEnumerable<Element> MenuElements() {
		_ = ModuleManager.TryGetModule(typeof(FastSuperDash), out Module? module);
		bool menuEnabled = module != null && module.Enabled;

		Element instantToggle = Blueprints.HorizontalBoolOption(
			"Settings/instantSuperDash".Localize(),
			"",
			b => {
				instantSuperDash = b;
				GodhomeQoL.SaveGlobalSettingsSafe();
			},
			() => instantSuperDash
		);

		Element everywhereToggle = Blueprints.HorizontalBoolOption(
			"Settings/fastSuperDashEverywhere".Localize(),
			"",
			b => {
				fastSuperDashEverywhere = b;
				GodhomeQoL.SaveGlobalSettingsSafe();
			},
			() => fastSuperDashEverywhere
		);

		CustomSlider speedSlider = new(
			"Settings/fastSuperDashSpeedMultiplier".Localize(),
			val => {
				fastSuperDashSpeedMultiplier = (float) Math.Round(val, 2);
			},
			() => (float) Math.Round(fastSuperDashSpeedMultiplier, 2),
			1f,
			10f,
			false
		);

		Element moduleToggle = Blueprints.HorizontalBoolOption(
			"Modules/FastSuperDash".Localize(),
			$"ToggleableLevel/{ToggleableLevel.ChangeScene}".Localize(),
			val => {
				if (module != null) {
					module.Enabled = val;
					GodhomeQoL.SaveGlobalSettingsSafe();
				}
			},
			() => module?.Enabled ?? menuEnabled
		);

		instantToggle.isVisible = true;
		everywhereToggle.isVisible = true;
		speedSlider.isVisible = true;

		moduleToggle.OnUpdate += (_, _) => { };
		instantToggle.OnUpdate += (_, _) => { };
		everywhereToggle.OnUpdate += (_, _) => { };
		speedSlider.OnUpdate += (_, _) => { };

		return new Element[] { moduleToggle, instantToggle, everywhereToggle, speedSlider };
	}

	internal static MenuScreen GetMenu(MenuScreen parent) {
		Menu m = new("Modules/FastSuperDash".Localize(), [..MenuElements()]);
		return m.GetMenuScreen(parent);
	}
}
