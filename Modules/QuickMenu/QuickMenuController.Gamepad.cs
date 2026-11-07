using InControl;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController
    {
        private const string DefaultQuickMenuControllerButton = "RightStickButton";
        private const string DefaultTeleportKitControllerButton = "LeftStickButton";

        private bool gamepadMode;
        private bool gamepadHeroControlTaken;
        private bool gamepadHeroHookActive;
        private bool gamepadReleasePending;
        private bool waitingForQuickMenuControllerRebind;
        private int quickMenuControllerRebindFrame;
        private GameObject? hoverEffectsRoot;
        private Text? quickMenuControllerButtonValue;
        private Text? teleportKitControllerButtonValue;
        private bool controllerRebindForTeleportKit;

        private void HandleQuickMenuGamepad(bool canToggle)
        {
            if (waitingForQuickMenuControllerRebind)
            {
                HandleQuickMenuControllerRebind();
                return;
            }

            bool anyVisible = IsAnyUiVisible();
            if (gamepadReleasePending)
            {
                gamepadReleasePending = false;
                if (!anyVisible)
                {
                    ReleaseGamepadMode();
                }
            }

            if (anyVisible)
            {
                GameObject? root = GetActiveUiRoot();
                if (root != null && root != hoverEffectsRoot)
                {
                    hoverEffectsRoot = root;
                    AttachHkHoverEffects(root);
                }
            }

            InputControlType toggleButton = GetQuickMenuControllerButton();
            if (canToggle
                && !IsAnyRebinding()
                && !EqualityComparer<InputControlType>.Default.Equals(toggleButton, default)
                && IsGamepadControlPressed(toggleButton))
            {
                if (anyVisible)
                {
                    ToggleMenu();
                }
                else
                {
                    gamepadMode = true;
                    virtualCursorPlaced = false;
                    ToggleMenu();
                    TakeHeroControlForGamepad();
                }

                return;
            }

            if (!gamepadMode || !anyVisible)
            {
                return;
            }

            UpdateVirtualCursor();

            if (!IsAnyRebinding() && IsControllerButtonPressed(InputControlType.Action2))
            {
                GamepadBack();
            }
        }

        private InputControlType GetQuickMenuControllerButton()
        {
            string raw = GodhomeQoL.GlobalSettings.QuickMenuControllerButton ?? DefaultQuickMenuControllerButton;
            return Enum.TryParse(raw, out InputControlType button) ? button : default;
        }

        private string GetQuickMenuControllerButtonLabel()
        {
            InputControlType button = GetQuickMenuControllerButton();
            return EqualityComparer<InputControlType>.Default.Equals(button, default)
                ? "QuickMenu/GamepadNone".Localize()
                : FormatControllerButton(button);
        }

        private static string FormatControllerButton(InputControlType button) => button switch
        {
            InputControlType.Action1 => "A",
            InputControlType.Action2 => "B",
            InputControlType.Action3 => "X",
            InputControlType.Action4 => "Y",
            InputControlType.LeftBumper => "LB",
            InputControlType.RightBumper => "RB",
            InputControlType.LeftTrigger => "LT",
            InputControlType.RightTrigger => "RT",
            InputControlType.LeftStickButton => "L3",
            InputControlType.RightStickButton => "R3",
            InputControlType.Back or InputControlType.Select or InputControlType.View or InputControlType.Share => "Select",
            InputControlType.Start or InputControlType.Menu or InputControlType.Options => "Start",
            InputControlType.DPadUp => "D-Pad Up",
            InputControlType.DPadDown => "D-Pad Down",
            InputControlType.DPadLeft => "D-Pad Left",
            InputControlType.DPadRight => "D-Pad Right",
            _ => button.ToString()
        };

        private void StartQuickMenuControllerRebind()
        {
            controllerRebindForTeleportKit = false;
            waitingForQuickMenuControllerRebind = true;
            quickMenuControllerRebindFrame = Time.frameCount;
            UpdateKeybindValue(quickMenuControllerButtonValue, "QuickMenu/GamepadPress".Localize());
        }

        private void StartTeleportKitControllerRebind()
        {
            controllerRebindForTeleportKit = true;
            waitingForQuickMenuControllerRebind = true;
            quickMenuControllerRebindFrame = Time.frameCount;
            UpdateKeybindValue(teleportKitControllerButtonValue, "QuickMenu/GamepadPress".Localize());
        }

        private string GetTeleportKitControllerButtonLabel()
        {
            string raw = GodhomeQoL.GlobalSettings.TeleportKitControllerButton ?? DefaultTeleportKitControllerButton;
            return Enum.TryParse(raw, out InputControlType button) && button != InputControlType.None
                ? FormatControllerButton(button)
                : "QuickMenu/GamepadNone".Localize();
        }

        private void HandleQuickMenuControllerRebind()
        {
            if (Time.frameCount == quickMenuControllerRebindFrame)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                FinishQuickMenuControllerRebind();
                return;
            }

            if (!TryGetPressedGamepadControl(out InputControlType button))
            {
                return;
            }

            if (controllerRebindForTeleportKit)
            {
                GodhomeQoL.GlobalSettings.TeleportKitControllerButton = button.ToString();
            }
            else
            {
                GodhomeQoL.GlobalSettings.QuickMenuControllerButton = button.ToString();
            }

            GodhomeQoL.SaveGlobalSettingsSafe();
            FinishQuickMenuControllerRebind();
        }

        private static readonly HashSet<InputControlType> GamepadBindableControls = new()
        {
            InputControlType.Action1,
            InputControlType.Action2,
            InputControlType.Action3,
            InputControlType.Action4,
            InputControlType.LeftBumper,
            InputControlType.RightBumper,
            InputControlType.LeftTrigger,
            InputControlType.RightTrigger,
            InputControlType.LeftStickButton,
            InputControlType.RightStickButton,
            InputControlType.DPadUp,
            InputControlType.DPadDown,
            InputControlType.DPadLeft,
            InputControlType.DPadRight,
            InputControlType.Back,
            InputControlType.Start,
            InputControlType.Select,
            InputControlType.Menu,
            InputControlType.Options,
            InputControlType.Share,
            InputControlType.View
        };

        private static bool IsGamepadControlPressed(InputControlType target)
        {
            foreach (InputDevice device in InputManager.Devices)
            {
                if (device == null)
                {
                    continue;
                }

                InputControl control = device.GetControl(target);
                if (control != null && control.WasPressed)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetPressedGamepadControl(out InputControlType button)
        {
            button = default;
            foreach (InputDevice device in InputManager.Devices)
            {
                if (device == null)
                {
                    continue;
                }

                foreach (InputControl control in device.Controls)
                {
                    if (control != null && control.WasPressed && GamepadBindableControls.Contains(control.Target))
                    {
                        button = control.Target;
                        return true;
                    }
                }
            }

            return false;
        }

        private void FinishQuickMenuControllerRebind()
        {
            waitingForQuickMenuControllerRebind = false;
            if (controllerRebindForTeleportKit)
            {
                UpdateKeybindValue(teleportKitControllerButtonValue, GetTeleportKitControllerButtonLabel());
            }
            else
            {
                UpdateKeybindValue(quickMenuControllerButtonValue, GetQuickMenuControllerButtonLabel());
            }
        }

        private void TakeHeroControlForGamepad()
        {
            if (!gamepadHeroHookActive)
            {
                ModHooks.HeroUpdateHook += BlockHeroInputForGamepad;
                gamepadHeroHookActive = true;
            }

            BlockHeroInputForGamepad();
        }

        private void BlockHeroInputForGamepad()
        {
            if (!gamepadMode)
            {
                return;
            }

            HeroController? hero = HeroController.instance;
            if (hero == null || !hero.acceptingInput)
            {
                return;
            }

            hero.IgnoreInput();
            gamepadHeroControlTaken = true;
        }

        private void ReleaseGamepadMode()
        {
            gamepadMode = false;
            hoverEffectsRoot = null;
            HideVirtualCursor();

            if (gamepadHeroHookActive)
            {
                ModHooks.HeroUpdateHook -= BlockHeroInputForGamepad;
                gamepadHeroHookActive = false;
            }

            if (!gamepadHeroControlTaken)
            {
                return;
            }

            gamepadHeroControlTaken = false;
            HeroController? hero = HeroController.instance;
            GameManager? manager = GameManager.instance;
            if (hero != null
                && !hero.acceptingInput
                && !hero.controlReqlinquished
                && !hero.cState.dead
                && manager != null
                && !manager.IsInSceneTransition)
            {
                hero.AcceptInput();
            }
        }

        private GameObject? GetActiveUiRoot()
        {
            GameObject? best = null;
            int bestOrder = int.MinValue;
            foreach (Transform child in transform)
            {
                if (!child.gameObject.activeInHierarchy || child.gameObject == statusRoot)
                {
                    continue;
                }

                Canvas? canvas = child.GetComponent<Canvas>();
                if (canvas == null || canvas.sortingOrder < bestOrder)
                {
                    continue;
                }

                best = child.gameObject;
                bestOrder = canvas.sortingOrder;
            }

            return best;
        }

        private static GameObject? FindActiveDialog(GameObject root)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(false))
            {
                if (node.name == "Dialog" && node.gameObject.activeInHierarchy)
                {
                    return node.gameObject;
                }
            }

            return null;
        }

        private static GameObject? FindFirstGamepadTarget(GameObject root)
        {
            foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(false))
            {
                if (selectable == null
                    || selectable is Scrollbar
                    || !selectable.gameObject.activeInHierarchy
                    || !selectable.IsInteractable()
                    || selectable.navigation.mode == Navigation.Mode.None)
                {
                    continue;
                }

                return selectable.gameObject;
            }

            return null;
        }

        private void GamepadBack()
        {
            GameObject? root = GetActiveUiRoot();
            if (root == null)
            {
                return;
            }

            GameObject? dialog = FindActiveDialog(root);
            if (dialog != null && ClickNamedButton(dialog, "NoRow"))
            {
                return;
            }

            if (root != quickRoot && ClickNamedButton(root, "BackRow"))
            {
                return;
            }

            ToggleMenu();
        }

        private static bool ClickNamedButton(GameObject root, string suffix)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(false))
            {
                if (button != null
                    && button.gameObject.activeInHierarchy
                    && button.IsInteractable()
                    && button.gameObject.name.EndsWith(suffix, StringComparison.Ordinal))
                {
                    button.onClick.Invoke();
                    return true;
                }
            }

            return false;
        }

        private static void AttachHkHoverEffects(GameObject root)
        {
            foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(true))
            {
                if (selectable == null
                    || selectable is Scrollbar
                    || selectable is Slider
                    || selectable is InputField
                    || selectable.GetComponent<RowHighlight>() != null
                    || selectable.GetComponent<RowHighlightTrigger>() != null
                    || selectable.GetComponent<HkPlateHover>() != null
                    || selectable.GetComponent<HkIconHover>() != null)
                {
                    continue;
                }

                selectable.gameObject.AddComponent<HkIconHover>();
            }
        }
    }
}
