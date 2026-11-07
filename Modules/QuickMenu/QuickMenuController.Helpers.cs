using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController
    {
        internal static Font GetMenuFont()
        {
            return Modding.Menu.MenuResources.Perpetua ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static Sprite? LoadQuickHandleSprite()
        {
            try
            {
                Assembly asm = typeof(QuickMenu).Assembly;
                string? resourceName = asm
                    .GetManifestResourceNames()
                    .FirstOrDefault(name => name.EndsWith("Pin_Knight.png", StringComparison.OrdinalIgnoreCase));

                if (resourceName != null)
                {
                    using Stream? stream = asm.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        return LoadSpriteFromStream(stream, "Pin_Knight");
                    }
                }

                string dir = Path.GetDirectoryName(asm.Location)
                    ?? AppDomain.CurrentDomain.BaseDirectory
                    ?? Environment.CurrentDirectory;
                string path = Path.Combine(dir, "Pin_Knight.png");

                if (File.Exists(path))
                {
                    using FileStream stream = File.OpenRead(path);
                    return LoadSpriteFromStream(stream, "Pin_Knight");
                }
            }
            catch (Exception e)
            {
                LogDebug($"QuickMenu: failed to load Pin_Knight sprite - {e.Message}");
            }

            return null;
        }

        private static Sprite? LoadCollectorIconSprite(string fileName, string name)
        {
            try
            {
                Assembly asm = typeof(QuickMenu).Assembly;
                string? resourceName = asm
                    .GetManifestResourceNames()
                    .FirstOrDefault(resource => resource.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

                if (resourceName != null)
                {
                    using Stream? stream = asm.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        return LoadSpriteFromStream(stream, name);
                    }
                }

                string dir = Path.GetDirectoryName(asm.Location)
                    ?? AppDomain.CurrentDomain.BaseDirectory
                    ?? Environment.CurrentDirectory;
                string path = Path.Combine(dir, fileName);

                if (File.Exists(path))
                {
                    using FileStream stream = File.OpenRead(path);
                    return LoadSpriteFromStream(stream, name);
                }
            }
            catch (Exception e)
            {
                LogDebug($"QuickMenu: failed to load {fileName} - {e.Message}");
            }

            return null;
        }

        private static Sprite? LoadBossManipulateIconSprite(string fileName, string name)
        {
            try
            {
                Assembly asm = typeof(QuickMenu).Assembly;
                string? resourceName = asm
                    .GetManifestResourceNames()
                    .FirstOrDefault(resource => resource.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

                if (resourceName != null)
                {
                    using Stream? stream = asm.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        return LoadSpriteFromStream(stream, name);
                    }
                }

                string dir = Path.GetDirectoryName(asm.Location)
                    ?? AppDomain.CurrentDomain.BaseDirectory
                    ?? Environment.CurrentDirectory;

                string[] candidates =
                {
                    Path.Combine(dir, "Boss Manipulate Images", fileName),
                    Path.Combine(dir, "Resources", "Boss Manipulate Images", fileName),
                    Path.Combine(dir, "Resources", fileName),
                    Path.Combine(dir, fileName),
                };

                foreach (string path in candidates)
                {
                    if (!File.Exists(path))
                    {
                        continue;
                    }

                    using FileStream stream = File.OpenRead(path);
                    return LoadSpriteFromStream(stream, name);
                }
            }
            catch (Exception e)
            {
                LogDebug($"QuickMenu: failed to load Boss Manipulate icon {fileName} - {e.Message}");
            }

            return null;
        }

        private static Sprite? LoadSpriteFromStream(Stream stream, string name)
        {
            using MemoryStream ms = new();
            stream.CopyTo(ms);
            byte[] data = ms.ToArray();

            Texture2D texture = new(2, 2, TextureFormat.ARGB32, false);
            if (!texture.LoadImage(data))
            {
                return null;
            }

            texture.name = name;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.Apply();

            Rect rect = new(0f, 0f, texture.width, texture.height);
            return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
        }

        private static GameObject CreateQuickPanel(Transform parent)
        {
            GameObject panel = new GameObject("QuickMenuPanel");
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -20f);
            rect.sizeDelta = new Vector2(QuickPanelWidth, QuickPanelHeight);

            return panel;
        }

        private static GameObject CreateQuickPanelBackplate(Transform parent)
        {
            GameObject backplate = new("QuickMenuBackplate");
            backplate.transform.SetParent(parent, false);

            RectTransform rect = backplate.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(QuickPanelWidth, QuickPanelHeight);

            Image image = backplate.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, QuickPanelBackplateAlpha);
            image.raycastTarget = false;

            return backplate;
        }

        private static RectTransform CreateQuickPanelContent(Transform parent)
        {
            GameObject content = new("QuickMenuContent");
            content.transform.SetParent(parent, false);

            RectTransform rect = content.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            return rect;
        }
    }
}
