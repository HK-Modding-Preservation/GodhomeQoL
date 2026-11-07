using Modding.Menu;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController
    {
        private const int HkTitleMaxSize = 46;
        private const int HkTitleMinSize = 22;
        private const float HkTitleFleurScale = 0.42f;
        private const float HkTitleFleurOffset = 14f;
        private const float HkCursorScale = 0.36f;
        private const float HkCursorOffset = 34f;
        private const int HkSoftTextureSize = 64;
        private const float HkSoftFade = 20f;
        private const float HkSoftBorder = 24f;

        private static readonly Color HkDialogColor = new(0.02f, 0.02f, 0.02f, 0.94f);
        private static readonly Color HkPanelColor = new(0f, 0f, 0f, 0.72f);
        private static readonly Color HkRowHighlightColor = new(1f, 1f, 1f, 0.05f);
        private static readonly Color HkDisabledTextColor = new(0.5f, 0.5f, 0.5f, 1f);
        private static readonly Color HkQuickEntryHoverColor = new(0.16f, 0.15f, 0.13f, 0.8f);
        private static readonly Color HkQuickEntryOnColor = new(0.24f, 0.16f, 0.06f, 0.92f);
        private static readonly Color HkQuickEntryOnHoverColor = new(0.32f, 0.22f, 0.09f, 0.95f);
        private static readonly Color HkQuickEntryOffHoverColor = new(0.16f, 0.11f, 0.05f, 0.9f);
        private static readonly Color HkStateGlowOnColor = new(1f, 0.78f, 0.35f, 0.55f);
        private static readonly Color HkFrameNeutralColor = new(0.902f, 0.749f, 0.384f, 0.45f);
        private static readonly Color HkFrameOnColor = new(0.902f, 0.749f, 0.384f, 0.8f);
        private static readonly Color HkFrameOffColor = new(0.902f, 0.749f, 0.384f, 0.28f);
        private static readonly Color HkQuickEntryTextColor = new(0.953f, 0.894f, 0.753f, 1f);

        private const int HkDotTextureSize = 32;
        private const int HkChamferTextureSize = 32;
        private const float HkChamferCut = 7f;
        private const float HkChamferBorder = 10f;
        private const int HkLampTextureWidth = 60;
        private const int HkLampTextureHeight = 68;
        private const float HkLampWidth = 16f;
        private const float HkLampHeight = 18f;
        private const float HkStateGlowSize = 34f;
        private const float HkStateDotRight = 16f;
        private const float HkQuickEntryTextLeft = 16f;
        private const float HkQuickEntryTextRight = 30f;

        private static Sprite? hkGlowSprite;
        private static Sprite? hkChamferFillSprite;
        private static Sprite? hkChamferLineSprite;
        private static Sprite? hkLampOnSprite;
        private static Sprite? hkLampOffSprite;
        private static Sprite? hkFrameSprite;

        private const int HkFrameTextureSize = 64;
        private const float HkFrameRadius = 12f;
        private const float HkFrameBorder = 16f;
        private static readonly Color HkGroupFillColor = new(0f, 0f, 0f, 0.38f);
        private static readonly Color HkGroupLineColor = new(0.88f, 0.84f, 0.72f, 0.6f);

        private static Sprite? hkSoftSprite;
        private static GameObject? hkCursorLeft;
        private static GameObject? hkCursorRight;
        private static RowHighlight? hkCursorOwner;

        internal static Font GetHkTitleFont() => MenuResources.TrajanBold ?? GetMenuFont();

        private static Font GetHkButtonFont() => MenuResources.TrajanRegular ?? GetMenuFont();

        internal static Sprite GetHkSoftSprite()
        {
            if (hkSoftSprite != null)
            {
                return hkSoftSprite;
            }

            int size = HkSoftTextureSize;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontUnloadUnusedAsset
            };

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Min(x + 0.5f, size - x - 0.5f);
                    float dy = Mathf.Min(y + 0.5f, size - y - 0.5f);
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(dx, dy) / HkSoftFade));
                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);

            hkSoftSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(HkSoftBorder, HkSoftBorder, HkSoftBorder, HkSoftBorder));
            hkSoftSprite.name = "GodhomeQoL_HkSoftPlate";
            hkSoftSprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return hkSoftSprite;
        }

        private static void ApplyHkSoftPlate(Image image, Color color)
        {
            image.sprite = GetHkSoftSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
        }

        private static void StyleHkPanel(Image panelImage)
        {
            ApplyHkSoftPlate(panelImage, HkPanelColor);
        }

        private static void StyleHkDialog(Image dialogImage)
        {
            ApplyHkSoftPlate(dialogImage, HkDialogColor);
        }

        internal static void StyleHkTitle(Text title)
        {
            title.font = GetHkTitleFont();
            int maxSize = Mathf.Min(title.fontSize, HkTitleMaxSize);
            title.fontSize = maxSize;
            title.resizeTextForBestFit = true;
            title.resizeTextMaxSize = maxSize;
            title.resizeTextMinSize = Mathf.Min(HkTitleMinSize, maxSize);
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.verticalOverflow = VerticalWrapMode.Truncate;
            AddHkTitleFleur(title.rectTransform);
        }

        private static void StyleHkButtonText(Text label)
        {
            label.font = GetHkButtonFont();
            int maxSize = Mathf.Max(14, label.fontSize - 2);
            label.fontSize = maxSize;
            label.resizeTextForBestFit = true;
            label.resizeTextMaxSize = maxSize;
            label.resizeTextMinSize = Mathf.Min(14, maxSize);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static void AddHkTitleFleur(RectTransform titleRect)
        {
            RuntimeAnimatorController? controller = MenuResources.MenuTopFleurAnimator;
            if (controller == null)
            {
                return;
            }

            GameObject fleur = new("TopFleur");
            fleur.transform.SetParent(titleRect, false);
            RectTransform rect = fleur.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -HkTitleFleurOffset);
            rect.sizeDelta = new Vector2(1087f, 98f);
            rect.localScale = new Vector3(HkTitleFleurScale, HkTitleFleurScale, 1f);

            Image image = fleur.AddComponent<Image>();
            image.raycastTarget = false;

            Animator animator = fleur.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.applyRootMotion = false;

            fleur.AddComponent<HkMenuAnimator>().Show(true);
        }

        private static void StyleHkScrollbar(Image track, Image handle, RectTransform handleRect)
        {
            Sprite? line = MenuResources.ScrollbarBackgroundSprite;
            if (line != null)
            {
                track.sprite = line;
                track.type = Image.Type.Simple;
                track.color = new Color(1f, 1f, 1f, 0.85f);
            }

            Sprite? fleurSprite = MenuResources.ScrollbarHandleSprite;
            if (fleurSprite == null)
            {
                return;
            }

            handle.color = new Color(1f, 1f, 1f, 0f);

            Scrollbar? scrollbar = track.GetComponent<Scrollbar>();
            GameObject fleur = new("TopFleur");
            fleur.transform.SetParent(scrollbar != null ? track.rectTransform : handleRect, false);
            RectTransform rect = fleur.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0.8f, 0f);
            rect.sizeDelta = new Vector2(37.8f, 68.5f);
            rect.localScale = new Vector3(0.9f, 0.9f, 1f);

            Image image = fleur.AddComponent<Image>();
            image.sprite = fleurSprite;
            image.raycastTarget = false;

            if (scrollbar != null)
            {
                fleur.AddComponent<HkScrollFleur>().Scrollbar = scrollbar;
            }
        }

        private sealed class HkScrollFleur : MonoBehaviour
        {
            internal Scrollbar? Scrollbar { get; set; }

            private void LateUpdate()
            {
                if (Scrollbar == null || transform is not RectTransform rect)
                {
                    return;
                }

                float value = Mathf.Clamp01(Scrollbar.value);
                rect.anchorMin = new Vector2(0.5f, value);
                rect.anchorMax = new Vector2(0.5f, value);
            }
        }

        private static void StyleHkSlider(Image background, RectTransform fillAreaRect, Image fill, RectTransform handleRect, Image handle)
        {
            background.color = new Color(1f, 1f, 1f, 0f);

            GameObject track = new("Track");
            track.transform.SetParent(background.transform, false);
            track.transform.SetAsFirstSibling();
            RectTransform trackRect = track.AddComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 0.5f);
            trackRect.anchorMax = new Vector2(1f, 0.5f);
            trackRect.offsetMin = new Vector2(10f, -2f);
            trackRect.offsetMax = new Vector2(-10f, 2f);
            Image trackImage = track.AddComponent<Image>();
            trackImage.color = new Color(1f, 1f, 1f, 0.25f);
            trackImage.raycastTarget = false;

            fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRect.offsetMin = new Vector2(10f, -2f);
            fillAreaRect.offsetMax = new Vector2(-10f, 2f);
            fill.color = new Color(1f, 1f, 1f, 0.85f);

            Sprite? fleurSprite = MenuResources.ScrollbarHandleSprite;
            if (fleurSprite != null)
            {
                handle.sprite = fleurSprite;
                handle.preserveAspect = true;
                handle.color = Color.white;
                handleRect.sizeDelta = new Vector2(22f, 40f);
            }
        }

        private static void StyleHkStatusText(Text text)
        {
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }

        private static void AddHkPlateHover(GameObject target, Image plate, Color normal)
        {
            HkPlateHover hover = target.AddComponent<HkPlateHover>();
            hover.Plate = plate;
            hover.Normal = normal;
            hover.Hover = HkQuickEntryHoverColor;
        }

        private static Sprite CreateHkRadialSprite(string name, bool solid)
        {
            int size = HkDotTextureSize;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontUnloadUnusedAsset
            };

            Color[] pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Mathf.Sqrt(((x - center) * (x - center)) + ((y - center) * (y - center))) / (size * 0.5f);
                    float alpha = solid
                        ? Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.72f, 1f, distance))
                        : Mathf.Pow(Mathf.Clamp01(1f - distance), 2f);
                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sprite;
        }

        private static Sprite GetHkGlowSprite() => hkGlowSprite != null ? hkGlowSprite : hkGlowSprite = CreateHkRadialSprite("GodhomeQoL_HkGlow", false);

        internal static Sprite GetHkFrameSprite()
        {
            if (hkFrameSprite != null)
            {
                return hkFrameSprite;
            }

            int size = HkFrameTextureSize;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontUnloadUnusedAsset
            };

            Color[] pixels = new Color[size * size];
            float center = size * 0.5f;
            float halfExtent = center - 1.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - center) - (halfExtent - HkFrameRadius);
                    float qy = Mathf.Abs(y + 0.5f - center) - (halfExtent - HkFrameRadius);
                    float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                    float distance = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - HkFrameRadius;
                    float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1.6f, Mathf.Abs(distance + 1f)));
                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            hkFrameSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(HkFrameBorder, HkFrameBorder, HkFrameBorder, HkFrameBorder));
            hkFrameSprite.name = "GodhomeQoL_HkFrame";
            hkFrameSprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return hkFrameSprite;
        }

        private static void AddHkGroupFrame(Transform row, float width, float height, float centerY)
        {
            GameObject frame = new("GroupFrame");
            frame.transform.SetParent(row, false);
            frame.transform.SetAsFirstSibling();
            RectTransform rect = frame.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, centerY);
            rect.sizeDelta = new Vector2(width, height);

            Image fill = frame.AddComponent<Image>();
            ApplyHkSoftPlate(fill, HkGroupFillColor);
            fill.raycastTarget = false;

            GameObject line = new("Outline");
            line.transform.SetParent(frame.transform, false);
            RectTransform lineRect = line.AddComponent<RectTransform>();
            lineRect.anchorMin = Vector2.zero;
            lineRect.anchorMax = Vector2.one;
            lineRect.offsetMin = Vector2.zero;
            lineRect.offsetMax = Vector2.zero;

            Image outline = line.AddComponent<Image>();
            outline.sprite = GetHkFrameSprite();
            outline.type = Image.Type.Sliced;
            outline.color = HkGroupLineColor;
            outline.raycastTarget = false;

            frame.AddComponent<HkGroupFrameHover>().Init(fill, outline);
        }

        private static void StyleHkQuickEntryText(Text text)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMaxSize = text.fontSize;
            text.resizeTextMinSize = Mathf.Min(15, text.fontSize);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static void CreateHkQuickEntryStateDot(Transform button)
        {
            GameObject glow = CreateHkStateImage(button, "StateGlow", GetHkGlowSprite(), new Vector2(HkStateGlowSize, HkStateGlowSize));
            glow.SetActive(false);
            GameObject dot = CreateHkStateImage(button, "StateDot", GetHkLampSprite(false), new Vector2(HkLampWidth, HkLampHeight));
            dot.SetActive(false);
        }

        private static GameObject CreateHkStateImage(Transform parent, string name, Sprite sprite, Vector2 size)
        {
            GameObject obj = new(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-HkStateDotRight, 0f);
            rect.sizeDelta = size;
            Image image = obj.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return obj;
        }

        private static void ApplyHkChamferPlate(Image image, Color color)
        {
            image.sprite = GetHkChamferSprite(false);
            image.type = Image.Type.Sliced;
            image.color = color;
        }

        private static void CreateHkQuickEntryFrame(Transform button)
        {
            GameObject frame = new("Frame");
            frame.transform.SetParent(button, false);
            RectTransform rect = frame.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = frame.AddComponent<Image>();
            image.sprite = GetHkChamferSprite(true);
            image.type = Image.Type.Sliced;
            image.color = HkFrameNeutralColor;
            image.raycastTarget = false;
        }

        internal static Sprite GetHkChamferSprite(bool outline)
        {
            Sprite? cached = outline ? hkChamferLineSprite : hkChamferFillSprite;
            if (cached != null)
            {
                return cached;
            }

            int size = HkChamferTextureSize;
            Texture2D texture = NewHkTexture(size, size);
            Color[] pixels = new Color[size * size];
            float half = size * 0.5f;
            float extent = half - 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = Mathf.Abs(x + 0.5f - half);
                    float py = Mathf.Abs(y + 0.5f - half);
                    float box = Mathf.Max(px - extent, py - extent);
                    float cut = (px + py - ((2f * extent) - HkChamferCut)) * 0.70710678f;
                    float distance = Mathf.Max(box, cut);
                    float alpha = outline
                        ? Mathf.Clamp01(1f - Mathf.Abs(distance + 0.5f) + 0.5f) * Mathf.Clamp01(0.5f - distance)
                        : Mathf.Clamp01(0.5f - distance);
                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(HkChamferBorder, HkChamferBorder, HkChamferBorder, HkChamferBorder));
            sprite.name = outline ? "GodhomeQoL_HkChamferLine" : "GodhomeQoL_HkChamferFill";
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            if (outline)
            {
                hkChamferLineSprite = sprite;
            }
            else
            {
                hkChamferFillSprite = sprite;
            }

            return sprite;
        }

        private static Sprite GetHkLampSprite(bool on)
        {
            Sprite? cached = on ? hkLampOnSprite : hkLampOffSprite;
            if (cached != null)
            {
                return cached;
            }

            int width = HkLampTextureWidth;
            int height = HkLampTextureHeight;
            Texture2D texture = NewHkTexture(width, height);
            Color[] pixels = new Color[width * height];
            float scale = width / 30f;
            float apothem = 14.2f * scale;
            float rim = 1.4f * scale;
            float innerApothem = apothem * 0.46f;
            float innerLine = 0.9f * scale;
            Vector2 center = new(width * 0.5f, height * 0.5f);
            Vector2 light = new(width * 0.5f, height * 0.65f);
            float gradientRadius = 0.8f * Mathf.Sqrt((width * 0.5f * width * 0.5f) + (height * 0.65f * height * 0.65f));

            Color fillTop = on ? HexColor(0xffe9a8) : HexColor(0x3f260b);
            Color fillBottom = on ? HexColor(0xe6a92c) : HexColor(0x1a0f05);
            Color rimTop = on ? HexColor(0xf6c75a) : HexColor(0xe6bf62, 0.9f);
            Color rimBottom = on ? HexColor(0xc98e2a) : HexColor(0xe6bf62, 0.9f);
            Color innerColor = on ? HexColor(0x9a5e14, 0.75f) : HexColor(0xe6bf62, 0.35f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new(x + 0.5f, y + 0.5f);
                    float distance = HexagonDistance(p - center, apothem);
                    float coverage = Mathf.Clamp01(0.5f - distance);
                    if (coverage <= 0f)
                    {
                        pixels[(y * width) + x] = new Color(0f, 0f, 0f, 0f);
                        continue;
                    }

                    float t = Mathf.Clamp01((p - light).magnitude / gradientRadius);
                    Color color = Color.Lerp(fillTop, fillBottom, t);

                    float innerDistance = Mathf.Abs(HexagonDistance(p - center, innerApothem));
                    float innerAmount = Mathf.Clamp01(innerLine - innerDistance + 0.5f) * innerColor.a;
                    color = Color.Lerp(color, new Color(innerColor.r, innerColor.g, innerColor.b, 1f), innerAmount);

                    float rimAmount = Mathf.Clamp01(distance + rim + 0.5f);
                    Color rimColor = Color.Lerp(rimBottom, rimTop, (float)y / height);
                    color = Color.Lerp(color, new Color(rimColor.r, rimColor.g, rimColor.b, 1f), rimAmount * rimColor.a);

                    color.a = coverage;
                    pixels[(y * width) + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = on ? "GodhomeQoL_HkLampOn" : "GodhomeQoL_HkLampOff";
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            if (on)
            {
                hkLampOnSprite = sprite;
            }
            else
            {
                hkLampOffSprite = sprite;
            }

            return sprite;
        }

        private static float HexagonDistance(Vector2 point, float apothem)
        {
            float x = Mathf.Abs(point.y);
            float y = Mathf.Abs(point.x);
            const float kx = -0.866025404f;
            const float ky = 0.5f;
            const float kz = 0.577350269f;
            float dot = Mathf.Min((kx * x) + (ky * y), 0f);
            x -= 2f * dot * kx;
            y -= 2f * dot * ky;
            float clampedX = Mathf.Clamp(x, -kz * apothem, kz * apothem);
            float dx = x - clampedX;
            float dy = y - apothem;
            return Mathf.Sqrt((dx * dx) + (dy * dy)) * Mathf.Sign(dy);
        }

        private static Texture2D NewHkTexture(int width, int height) => new(width, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontUnloadUnusedAsset
        };

        private static Color HexColor(int rgb, float alpha = 1f) => new(
            ((rgb >> 16) & 0xff) / 255f,
            ((rgb >> 8) & 0xff) / 255f,
            (rgb & 0xff) / 255f,
            alpha);

        private static void ApplyHkQuickEntryState(QuickMenuEntry entry, Image plate, bool enabled)
        {
            Color plateColor = enabled ? HkQuickEntryOnColor : QuickMenuBaseColor;
            plate.color = plateColor;

            HkPlateHover? hover = entry.Button.GetComponent<HkPlateHover>();
            if (hover != null)
            {
                hover.Normal = plateColor;
                hover.Hover = enabled ? HkQuickEntryOnHoverColor : HkQuickEntryOffHoverColor;
            }

            Transform? frame = entry.Button.transform.Find("Frame");
            if (frame != null && frame.TryGetComponent(out Image frameImage))
            {
                frameImage.color = enabled ? HkFrameOnColor : HkFrameOffColor;
            }

            Transform buttonTransform = entry.Button.transform;
            Transform? glow = buttonTransform.Find("StateGlow");
            if (glow != null)
            {
                glow.gameObject.SetActive(enabled);
                if (glow.TryGetComponent(out Image glowImage))
                {
                    glowImage.color = HkStateGlowOnColor;
                }
            }

            Transform? dot = buttonTransform.Find("StateDot");
            if (dot != null)
            {
                dot.gameObject.SetActive(true);
                dot.SetAsLastSibling();
                if (dot.TryGetComponent(out Image dotImage))
                {
                    dotImage.sprite = GetHkLampSprite(enabled);
                    dotImage.color = Color.white;
                }
            }

            entry.Label.color = enabled ? HkQuickEntryTextColor : HkDisabledTextColor;
            RectTransform textRect = entry.Label.rectTransform;
            textRect.offsetMin = new Vector2(HkQuickEntryTextLeft, 0f);
            textRect.offsetMax = new Vector2(-HkQuickEntryTextRight, 0f);
        }

        private static void ShowHkCursors(RowHighlight owner)
        {
            if (owner == null || owner.transform is not RectTransform target)
            {
                return;
            }

            if (!EnsureHkCursors(target))
            {
                return;
            }

            bool moved = !ReferenceEquals(hkCursorOwner, owner);
            hkCursorOwner = owner;
            PlaceHkCursor(hkCursorLeft!, target, 0f, -HkCursorOffset, moved);
            PlaceHkCursor(hkCursorRight!, target, 1f, HkCursorOffset, moved);
        }

        private static void HideHkCursors(RowHighlight owner)
        {
            if (!ReferenceEquals(hkCursorOwner, owner))
            {
                return;
            }

            hkCursorOwner = null;
            if (hkCursorLeft != null)
            {
                hkCursorLeft.GetComponent<HkMenuAnimator>()?.Hide();
            }

            if (hkCursorRight != null)
            {
                hkCursorRight.GetComponent<HkMenuAnimator>()?.Hide();
            }
        }

        private static bool EnsureHkCursors(Transform parent)
        {
            RuntimeAnimatorController? controller = MenuResources.MenuCursorAnimator;
            if (controller == null)
            {
                return false;
            }

            if (hkCursorLeft == null)
            {
                hkCursorLeft = CreateHkCursor("HkCursorLeft", controller, parent, 1f);
            }

            if (hkCursorRight == null)
            {
                hkCursorRight = CreateHkCursor("HkCursorRight", controller, parent, -1f);
            }

            return true;
        }

        private static GameObject CreateHkCursor(string name, RuntimeAnimatorController controller, Transform parent, float side)
        {
            GameObject cursor = new(name);
            cursor.transform.SetParent(parent, false);
            RectTransform rect = cursor.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(164f, 119f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = new Vector3(side * HkCursorScale, HkCursorScale, 1f);

            Image image = cursor.AddComponent<Image>();
            image.raycastTarget = false;

            Animator animator = cursor.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.applyRootMotion = false;

            cursor.AddComponent<HkMenuAnimator>();
            return cursor;
        }

        private static void PlaceHkCursor(GameObject cursor, RectTransform target, float anchorX, float x, bool restart)
        {
            RectTransform rect = (RectTransform)cursor.transform;
            if (rect.parent != target)
            {
                rect.SetParent(target, false);
            }

            rect.anchorMin = new Vector2(anchorX, 0.5f);
            rect.anchorMax = new Vector2(anchorX, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.SetAsLastSibling();
            cursor.GetComponent<HkMenuAnimator>()?.Show(restart);
        }

        private sealed class HkMenuAnimator : MonoBehaviour
        {
            private Animator? animator;
            private bool visible;
            private bool pending;
            private bool restart;

            internal void Show(bool restartAnimation)
            {
                visible = true;
                pending = true;
                restart |= restartAnimation;
            }

            internal void Hide()
            {
                if (!visible)
                {
                    return;
                }

                visible = false;
                restart = false;
                pending = isActiveAndEnabled;
            }

            private void Awake()
            {
                animator = GetComponent<Animator>();
            }

            private void OnEnable()
            {
                if (visible)
                {
                    pending = true;
                    restart = true;
                }
            }

            private void Update()
            {
                if (!pending || animator == null || animator.runtimeAnimatorController == null)
                {
                    return;
                }

                pending = false;
                if (restart)
                {
                    restart = false;
                    animator.Rebind();
                }

                animator.ResetTrigger(visible ? "hide" : "show");
                animator.SetTrigger(visible ? "show" : "hide");
            }
        }

        private sealed class HkPlateHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
        {
            private bool hovered;
            private bool selected;

            internal Image? Plate { get; set; }

            internal Color Normal { get; set; }

            internal Color Hover { get; set; }

            public void OnPointerEnter(PointerEventData eventData)
            {
                hovered = true;
                Refresh();
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                hovered = false;
                Refresh();
            }

            public void OnSelect(BaseEventData eventData)
            {
                selected = true;
                Refresh();
            }

            public void OnDeselect(BaseEventData eventData)
            {
                selected = false;
                Refresh();
            }

            private void OnDisable()
            {
                hovered = false;
                selected = false;
                Refresh();
            }

            private void Refresh()
            {
                if (Plate != null)
                {
                    Plate.color = hovered || selected ? Hover : Normal;
                }
            }
        }

        private sealed class HkGroupFrameHover : MonoBehaviour
        {
            private const float FadeSpeed = 7f;
            private static readonly Color LineActiveColor = new(0.98f, 0.86f, 0.55f, 1f);
            private static readonly Color FillActiveColor = new(0.2f, 0.14f, 0.05f, 0.55f);

            private Image? fill;
            private Image? line;
            private Color fillBase;
            private Color lineBase;
            private Transform? extraRow;
            private bool extraSearched;
            private float level;

            internal void Init(Image fillImage, Image lineImage)
            {
                fill = fillImage;
                line = lineImage;
                fillBase = fillImage.color;
                lineBase = lineImage.color;
            }

            private void OnDisable()
            {
                level = 0f;
                Apply();
            }

            private void Update()
            {
                float target = IsActive() ? 1f : 0f;
                if (Mathf.Approximately(level, target))
                {
                    return;
                }

                level = Mathf.MoveTowards(level, target, Time.unscaledDeltaTime * FadeSpeed);
                Apply();
            }

            private void Apply()
            {
                if (fill != null)
                {
                    fill.color = Color.Lerp(fillBase, FillActiveColor, level);
                }

                if (line != null)
                {
                    line.color = Color.Lerp(lineBase, LineActiveColor, level);
                }
            }

            private bool IsActive()
            {
                if (transform is RectTransform rect
                    && RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, null))
                {
                    return true;
                }

                GameObject? selected = EventSystem.current?.currentSelectedGameObject;
                if (selected == null)
                {
                    return false;
                }

                Transform? row = transform.parent;
                if (row != null && selected.transform.IsChildOf(row))
                {
                    return true;
                }

                Transform? extra = GetExtraRow();
                return extra != null && selected.transform.IsChildOf(extra);
            }

            private Transform? GetExtraRow()
            {
                if (!extraSearched)
                {
                    extraSearched = true;
                    Transform? row = transform.parent;
                    if (row != null && row.name == "GearSwitcherHpMaskRow")
                    {
                        extraRow = row.parent?.Find("GearSwitcherSoulVesselRow");
                    }
                }

                return extraRow;
            }
        }

        private sealed class HkIconHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
        {
            private const float FadeSpeed = 9f;
            private const float RingPadding = 10f;
            private static readonly Color GlowColor = new(1f, 0.8f, 0.4f, 0.6f);
            private static readonly Color RingColor = new(1f, 0.86f, 0.45f, 1f);

            private bool hovered;
            private bool selected;
            private float level;
            private GameObject? glow;
            private Image? glowImage;
            private GameObject? ring;
            private Image? ringImage;
            private RectTransform? groupFrame;
            private bool frameSearched;
            private const float FrameInset = 5f;

            public void OnPointerEnter(PointerEventData eventData)
            {
                hovered = true;
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                hovered = false;
            }

            public void OnSelect(BaseEventData eventData)
            {
                selected = true;
            }

            public void OnDeselect(BaseEventData eventData)
            {
                selected = false;
            }

            private void OnDisable()
            {
                hovered = false;
                selected = false;
                level = 0f;
                ApplyGlow();
            }

            private void Update()
            {
                float target = hovered || selected ? 1f : 0f;
                if (Mathf.Approximately(level, target))
                {
                    return;
                }

                if (target > 0f)
                {
                    EnsureGlow();
                }

                level = Mathf.MoveTowards(level, target, Time.unscaledDeltaTime * FadeSpeed);
                ApplyGlow();
            }

            private void ApplyGlow()
            {
                bool visible = level > 0.001f;
                if (visible)
                {
                    UpdateLayout();
                }

                if (glow != null && glowImage != null)
                {
                    glow.SetActive(visible);
                    glowImage.color = new Color(GlowColor.r, GlowColor.g, GlowColor.b, GlowColor.a * level);
                }

                if (ring != null && ringImage != null)
                {
                    ring.SetActive(visible);
                    ringImage.color = new Color(RingColor.r, RingColor.g, RingColor.b, RingColor.a * level);
                }
            }

            private void UpdateLayout()
            {
                if (transform is not RectTransform target)
                {
                    return;
                }

                Vector2 size = Vector2.Scale(target.rect.size, new Vector2(Mathf.Abs(target.localScale.x), Mathf.Abs(target.localScale.y)));
                Vector2 center = target.anchoredPosition + new Vector2((0.5f - target.pivot.x) * size.x, (0.5f - target.pivot.y) * size.y);
                float side = Mathf.Max(size.x, size.y);

                if (glow != null && glow.transform is RectTransform glowRect)
                {
                    glowRect.anchorMin = target.anchorMin;
                    glowRect.anchorMax = target.anchorMax;
                    glowRect.anchoredPosition = center;
                    glowRect.sizeDelta = new Vector2(side * 1.6f, side * 1.6f);
                }

                if (ring != null && ring.transform is RectTransform ringRect && target.parent is RectTransform parent)
                {
                    GetLocalBounds(target, parent, out Vector2 min, out Vector2 max);
                    min -= new Vector2(RingPadding, RingPadding);
                    max += new Vector2(RingPadding, RingPadding);

                    RectTransform? frame = FindGroupFrame();
                    if (frame != null)
                    {
                        GetLocalBounds(frame, parent, out Vector2 frameMin, out Vector2 frameMax);
                        min = Vector2.Max(min, frameMin + new Vector2(FrameInset, FrameInset));
                        max = Vector2.Min(max, frameMax - new Vector2(FrameInset, FrameInset));
                    }

                    ringRect.anchorMin = new Vector2(0.5f, 0.5f);
                    ringRect.anchorMax = new Vector2(0.5f, 0.5f);
                    ringRect.anchoredPosition = ((min + max) * 0.5f) - parent.rect.center;
                    ringRect.sizeDelta = Vector2.Max(max - min, Vector2.zero);
                }
            }

            private static void GetLocalBounds(RectTransform rect, RectTransform space, out Vector2 min, out Vector2 max)
            {
                Vector3[] corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                min = new Vector2(float.MaxValue, float.MaxValue);
                max = new Vector2(float.MinValue, float.MinValue);
                foreach (Vector3 corner in corners)
                {
                    Vector2 local = space.InverseTransformPoint(corner);
                    min = Vector2.Min(min, local);
                    max = Vector2.Max(max, local);
                }
            }

            private RectTransform? FindGroupFrame()
            {
                if (frameSearched)
                {
                    return groupFrame;
                }

                frameSearched = true;
                Transform? row = transform.parent;
                Transform? frame = row?.Find("GroupFrame");
                if (frame == null && row != null && row.name == "GearSwitcherSoulVesselRow")
                {
                    frame = row.parent?.Find("GearSwitcherHpMaskRow/GroupFrame");
                }

                groupFrame = frame as RectTransform;
                return groupFrame;
            }

            private void EnsureGlow()
            {
                if (glow != null || transform is not RectTransform target || target.parent == null)
                {
                    return;
                }

                glow = CreateSibling(target, "HoverGlow", target.GetSiblingIndex());
                glowImage = glow.AddComponent<Image>();
                glowImage.sprite = GetHkGlowSprite();
                glowImage.color = new Color(GlowColor.r, GlowColor.g, GlowColor.b, 0f);
                glowImage.raycastTarget = false;
                glow.SetActive(false);

                ring = CreateSibling(target, "SelectRing", target.GetSiblingIndex() + 1);
                ringImage = ring.AddComponent<Image>();
                ringImage.sprite = GetHkFrameSprite();
                ringImage.type = Image.Type.Sliced;
                ringImage.color = new Color(RingColor.r, RingColor.g, RingColor.b, 0f);
                ringImage.raycastTarget = false;
                ring.SetActive(false);
            }

            private static GameObject CreateSibling(RectTransform target, string name, int siblingIndex)
            {
                GameObject sibling = new(name);
                sibling.transform.SetParent(target.parent, false);
                sibling.transform.SetSiblingIndex(siblingIndex);
                RectTransform rect = sibling.AddComponent<RectTransform>();
                rect.pivot = new Vector2(0.5f, 0.5f);
                return sibling;
            }
        }
    }
}
