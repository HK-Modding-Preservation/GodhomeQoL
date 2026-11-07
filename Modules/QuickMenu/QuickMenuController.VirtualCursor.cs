using InControl;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController
    {
        private const int VirtualCursorSortOrder = 10200;
        private const float VirtualCursorSpeed = 1.1f;
        private const float VirtualCursorDPadFactor = 0.35f;
        private const float VirtualCursorScrollSpeed = 900f;
        private const float VirtualCursorStickDeadZone = 0.15f;
        private const int VirtualCursorTextureSize = 32;

        private static readonly Vector2[] VirtualCursorShape =
        {
            new(1f, 1f),
            new(1f, 23f),
            new(6f, 18.5f),
            new(10f, 28f),
            new(15f, 26f),
            new(11f, 16.5f),
            new(18f, 16.5f)
        };

        private static Sprite? virtualCursorSprite;
        private static readonly List<RaycastResult> VirtualCursorHits = new();

        private GameObject? virtualCursorRoot;
        private RectTransform? virtualCursorRect;
        private Vector2 virtualCursorPosition;
        private bool virtualCursorPlaced;
        private GameObject? virtualCursorPanel;
        private PointerEventData? virtualCursorPointer;
        private readonly List<GameObject> virtualCursorHoverChain = new();
        private GameObject? virtualCursorPressTarget;
        private GameObject? virtualCursorDragTarget;

        private void UpdateVirtualCursor()
        {
            EventSystem? eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            EnsureVirtualCursor();
            GameObject? panel = GetActiveUiRoot();
            if (!virtualCursorPlaced || panel != virtualCursorPanel)
            {
                virtualCursorPanel = panel;
                virtualCursorPlaced = true;
                PlaceVirtualCursorOnFirstTarget(panel);
            }

            InputDevice device = InputManager.ActiveDevice;
            Vector2 move = ApplyStickCurve(device.LeftStick.Vector) + (device.DPad.Vector * VirtualCursorDPadFactor);
            Vector2 delta = move * Screen.height * VirtualCursorSpeed * Time.unscaledDeltaTime;
            virtualCursorPosition = new Vector2(
                Mathf.Clamp(virtualCursorPosition.x + delta.x, 0f, Screen.width - 1f),
                Mathf.Clamp(virtualCursorPosition.y + delta.y, 0f, Screen.height - 1f));

            if (virtualCursorRoot != null)
            {
                virtualCursorRoot.SetActive(true);
            }

            if (virtualCursorRect != null)
            {
                virtualCursorRect.position = virtualCursorPosition;
            }

            PointerEventData pointer = virtualCursorPointer ??= new PointerEventData(eventSystem);
            pointer.position = virtualCursorPosition;
            pointer.delta = delta;
            pointer.button = PointerEventData.InputButton.Left;

            VirtualCursorHits.Clear();
            eventSystem.RaycastAll(pointer, VirtualCursorHits);
            GameObject? hit = null;
            foreach (RaycastResult result in VirtualCursorHits)
            {
                if (result.gameObject != null)
                {
                    hit = result.gameObject;
                    pointer.pointerCurrentRaycast = result;
                    break;
                }
            }

            if (hit == null)
            {
                pointer.pointerCurrentRaycast = default;
            }

            UpdateVirtualCursorHover(hit, pointer);
            UpdateVirtualCursorPress(device, hit, pointer, delta);
            UpdateVirtualCursorScroll(device, hit, panel);
        }

        private static Vector2 ApplyStickCurve(Vector2 stick)
        {
            float magnitude = stick.magnitude;
            if (magnitude < VirtualCursorStickDeadZone)
            {
                return Vector2.zero;
            }

            float scaled = Mathf.InverseLerp(VirtualCursorStickDeadZone, 1f, Mathf.Min(magnitude, 1f));
            return stick / magnitude * (scaled * scaled);
        }

        private void PlaceVirtualCursorOnFirstTarget(GameObject? panel)
        {
            GameObject? target = panel != null ? FindFirstGamepadTarget(panel) : null;
            if (target != null && target.transform is RectTransform rect)
            {
                Vector3[] corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                virtualCursorPosition = new Vector2((corners[0].x + corners[2].x) * 0.5f, (corners[0].y + corners[2].y) * 0.5f);
                return;
            }

            virtualCursorPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        private void UpdateVirtualCursorHover(GameObject? hit, PointerEventData pointer)
        {
            List<GameObject> chain = new();
            for (Transform? node = hit != null ? hit.transform : null; node != null; node = node.parent)
            {
                chain.Add(node.gameObject);
            }

            foreach (GameObject previous in virtualCursorHoverChain)
            {
                if (previous != null && !chain.Contains(previous))
                {
                    ExecuteEvents.Execute(previous, pointer, ExecuteEvents.pointerExitHandler);
                }
            }

            foreach (GameObject current in chain)
            {
                if (!virtualCursorHoverChain.Contains(current))
                {
                    ExecuteEvents.Execute(current, pointer, ExecuteEvents.pointerEnterHandler);
                }
            }

            virtualCursorHoverChain.Clear();
            virtualCursorHoverChain.AddRange(chain);
            pointer.pointerEnter = hit;
        }

        private void UpdateVirtualCursorPress(InputDevice device, GameObject? hit, PointerEventData pointer, Vector2 delta)
        {
            InputControl confirm = device.Action1;
            if (confirm.WasPressed && hit != null)
            {
                pointer.pressPosition = pointer.position;
                pointer.pointerPressRaycast = pointer.pointerCurrentRaycast;
                pointer.eligibleForClick = true;
                pointer.useDragThreshold = true;
                pointer.rawPointerPress = hit;

                GameObject? pressed = ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler)
                    ?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit);
                pointer.pointerPress = pressed;
                virtualCursorPressTarget = pressed;

                virtualCursorDragTarget = ExecuteEvents.GetEventHandler<IDragHandler>(hit);
                pointer.pointerDrag = virtualCursorDragTarget;
                if (virtualCursorDragTarget != null)
                {
                    ExecuteEvents.Execute(virtualCursorDragTarget, pointer, ExecuteEvents.initializePotentialDrag);
                    ExecuteEvents.Execute(virtualCursorDragTarget, pointer, ExecuteEvents.beginDragHandler);
                    pointer.dragging = true;
                }

                return;
            }

            if (confirm.IsPressed && virtualCursorDragTarget != null && delta != Vector2.zero)
            {
                ExecuteEvents.Execute(virtualCursorDragTarget, pointer, ExecuteEvents.dragHandler);
            }

            if (!confirm.WasReleased)
            {
                return;
            }

            if (virtualCursorPressTarget != null)
            {
                ExecuteEvents.Execute(virtualCursorPressTarget, pointer, ExecuteEvents.pointerUpHandler);
                GameObject? clickTarget = hit != null ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit) : null;
                if (clickTarget != null && clickTarget == virtualCursorPressTarget)
                {
                    ExecuteEvents.Execute(clickTarget, pointer, ExecuteEvents.pointerClickHandler);
                }
            }

            if (virtualCursorDragTarget != null)
            {
                ExecuteEvents.Execute(virtualCursorDragTarget, pointer, ExecuteEvents.endDragHandler);
            }

            ResetVirtualCursorPress(pointer);
        }

        private void ResetVirtualCursorPress(PointerEventData? pointer)
        {
            virtualCursorPressTarget = null;
            virtualCursorDragTarget = null;
            if (pointer == null)
            {
                return;
            }

            pointer.pointerPress = null;
            pointer.rawPointerPress = null;
            pointer.pointerDrag = null;
            pointer.dragging = false;
            pointer.eligibleForClick = false;
        }

        private static void UpdateVirtualCursorScroll(InputDevice device, GameObject? hit, GameObject? panel)
        {
            float amount = device.RightStick.Vector.y;
            if (Mathf.Abs(amount) < VirtualCursorStickDeadZone)
            {
                return;
            }

            ScrollRect? scroll = hit != null ? hit.GetComponentInParent<ScrollRect>() : null;
            if (scroll == null && panel != null)
            {
                scroll = panel.GetComponentInChildren<ScrollRect>();
            }

            if (scroll == null || scroll.content == null || scroll.viewport == null)
            {
                return;
            }

            RectTransform content = scroll.content;
            float maxOffset = Mathf.Max(0f, content.rect.height - scroll.viewport.rect.height);
            Vector2 position = content.anchoredPosition;
            position.y = Mathf.Clamp(position.y - (amount * VirtualCursorScrollSpeed * Time.unscaledDeltaTime), 0f, maxOffset);
            content.anchoredPosition = position;
            scroll.velocity = Vector2.zero;
        }

        private void HideVirtualCursor()
        {
            if (virtualCursorPointer != null)
            {
                foreach (GameObject previous in virtualCursorHoverChain)
                {
                    if (previous != null)
                    {
                        ExecuteEvents.Execute(previous, virtualCursorPointer, ExecuteEvents.pointerExitHandler);
                    }
                }
            }

            virtualCursorHoverChain.Clear();
            ResetVirtualCursorPress(virtualCursorPointer);
            virtualCursorPanel = null;
            virtualCursorPlaced = false;
            if (virtualCursorRoot != null)
            {
                virtualCursorRoot.SetActive(false);
            }
        }

        private void EnsureVirtualCursor()
        {
            if (virtualCursorRoot != null)
            {
                return;
            }

            virtualCursorRoot = new GameObject("GamepadCursorCanvas");
            virtualCursorRoot.transform.SetParent(transform, false);
            Canvas canvas = virtualCursorRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = VirtualCursorSortOrder;
            CanvasScaler scaler = virtualCursorRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            GameObject pointer = new("Pointer");
            pointer.transform.SetParent(virtualCursorRoot.transform, false);
            virtualCursorRect = pointer.AddComponent<RectTransform>();
            virtualCursorRect.anchorMin = Vector2.zero;
            virtualCursorRect.anchorMax = Vector2.zero;
            virtualCursorRect.pivot = new Vector2(1f / VirtualCursorTextureSize, 1f - (1f / VirtualCursorTextureSize));
            virtualCursorRect.sizeDelta = new Vector2(VirtualCursorTextureSize, VirtualCursorTextureSize);
            Image image = pointer.AddComponent<Image>();
            image.sprite = GetVirtualCursorSprite();
            image.raycastTarget = false;
            virtualCursorRoot.SetActive(false);
        }

        private static Sprite GetVirtualCursorSprite()
        {
            if (virtualCursorSprite != null)
            {
                return virtualCursorSprite;
            }

            int size = VirtualCursorTextureSize;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontUnloadUnusedAsset
            };

            Color fill = new(1f, 0.93f, 0.78f, 1f);
            Color edge = new(0.12f, 0.07f, 0.02f, 1f);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = size - (y + 0.5f);
                    float distance = DistanceToShape(px, py);
                    bool inside = IsInsideShape(px, py);
                    Color color = new(0f, 0f, 0f, 0f);
                    if (inside)
                    {
                        color = distance < 1.1f ? edge : fill;
                    }
                    else if (distance < 1.2f)
                    {
                        color = new Color(edge.r, edge.g, edge.b, 1f - (distance / 1.2f));
                    }

                    pixels[(y * size) + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            virtualCursorSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            virtualCursorSprite.name = "GodhomeQoL_GamepadCursor";
            virtualCursorSprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return virtualCursorSprite;
        }

        private static bool IsInsideShape(float x, float y)
        {
            bool inside = false;
            Vector2[] shape = VirtualCursorShape;
            for (int i = 0, j = shape.Length - 1; i < shape.Length; j = i++)
            {
                if ((shape[i].y > y) != (shape[j].y > y)
                    && x < ((shape[j].x - shape[i].x) * (y - shape[i].y) / (shape[j].y - shape[i].y)) + shape[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static float DistanceToShape(float x, float y)
        {
            float best = float.MaxValue;
            Vector2 point = new(x, y);
            Vector2[] shape = VirtualCursorShape;
            for (int i = 0, j = shape.Length - 1; i < shape.Length; j = i++)
            {
                Vector2 a = shape[j];
                Vector2 b = shape[i];
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(point, a + (ab * t)));
            }

            return best;
        }
    }
}
