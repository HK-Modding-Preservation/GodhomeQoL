namespace GodhomeQoL.Modules.Misc;

internal sealed class InspectHelper : Module {
	public override bool Hidden => true;
	public override bool AlwaysEnabled => true;

	private protected override void Load() =>
		ModHooks.FinishedLoadingModsHook += CreateGameObject;

	private protected override void Unload() =>
		ModHooks.FinishedLoadingModsHook -= CreateGameObject;

	private static void CreateGameObject() {
		if (ModHooks.GetMod("Unity Explorer", true) == null) {
			return;
		}

		_ = GameObjectUtil.CreateHolder<Inspector>($"{nameof(GodhomeQoL)} Inspect Helper");
		Log("Creating Inspect Helper GameObject");
	}

	private sealed class Test {
#if DEBUG
#endif
	}

	private sealed class Inspector : MonoBehaviour {
		public Test test;
		public static GodhomeQoL Instance => GodhomeQoL.UnsafeInstance;
		public static bool Active => GodhomeQoL.Active;

		public static Dictionary<string, Module> Modules => ModuleManager.Modules;

		public static GlobalSettings GlobalSettings => Setting.Global;
		public static LocalSettings LocalSettings => Setting.Local;

		public static Dict Dict => L11nUtil.dict;

		void Awake() => test = new();

		public static string Localize(string key) => L11nUtil.Localize(key);

		public static void KillAllSafe() {
			global::GodhomeQoL.Modules.Tools.ShowHPOnDeath.MarkCheatKill();
			FindObjectsOfType<HealthManager>()
				.Filter(EnemyDetector.IsValidEnemy)
				.Reject(hm => hm.IsInvincible)
				.ForEach(hm => hm.Die(null, AttackTypes.RuinsWater, true));
		}

		public static void SkipBoss(int count = 1) =>
			GlobalCoroutineExecutor.Start(SkipBossCoroutine(count));

		private static IEnumerator SkipBossCoroutine(int count) {
			if (BossSceneController.Instance is not BossSceneController controller) {
				yield break;
			}

			global::GodhomeQoL.Modules.Tools.ShowHPOnDeath.MarkCheatKill();
			controller.bossesDeadWaitTime = 0f;
			controller.EndBossScene();

			if (count > 1) {
				bool flag = false;
				GameManager? manager = GameManager.instance;
				if (manager == null)
				{
					yield break;
				}

				void HandleSceneEntered()
				{
					flag = true;
				}

				manager.OnFinishedEnteringScene += HandleSceneEntered;
				yield return new WaitUntil(() => flag || GameManager.instance == null);
				manager.OnFinishedEnteringScene -= HandleSceneEntered;

				_ = GlobalCoroutineExecutor.Start(SkipBossCoroutine(count - 1));
			}
		}
	}
}
