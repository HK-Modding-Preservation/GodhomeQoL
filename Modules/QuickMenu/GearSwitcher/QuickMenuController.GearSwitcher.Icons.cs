using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ToggleableBindings;
using ToggleableBindings.VanillaBindings;

namespace GodhomeQoL.Modules.Tools;

public sealed partial class QuickMenu : Module
{
    private sealed partial class QuickMenuController
    {
		private const float GearSwitcherIconScale = 0.85f;
		private const float GearSwitcherSmallIconSize = 37f;
		private const float GearSwitcherBigIconSize = 70f;

		private static Button CreateGearSwitcherIconButton(Transform parent, string name, float offsetX, float offsetY, Color glowColor, out RectTransform rectTransform, out Image image, out Outline outline)
		{
			GameObject gameObject = new GameObject(name);
			gameObject.transform.SetParent(parent, worldPositionStays: false);
			rectTransform = gameObject.AddComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			rectTransform.anchoredPosition = new Vector2(offsetX, offsetY);
			rectTransform.sizeDelta = new Vector2(44f, 44f);
			image = gameObject.AddComponent<Image>();
			image.preserveAspect = true;
			outline = gameObject.AddComponent<Outline>();
			outline.effectColor = glowColor;
			outline.effectDistance = new Vector2(2f, -2f);
			outline.enabled = false;
			Button button = gameObject.AddComponent<Button>();
			button.transition = Selectable.Transition.None;
			button.targetGraphic = image;
			return button;
		}

		private static void ApplyGearSwitcherIconState(Image icon, Outline? glow, bool active)
		{
			ApplyGearSwitcherIconState(icon, glow, active, GearSwitcherSpellActiveColor);
		}

		private static void ApplyGearSwitcherIconState(Image icon, Outline? glow, bool active, Color activeColor)
		{
			icon.color = (active ? activeColor : GearSwitcherSpellInactiveColor);
			if (glow != null)
			{
				glow.enabled = active;
			}
		}

		private static void ApplyGearSwitcherIcon(Image icon, Outline? glow, Sprite? sprite, bool active)
		{
			ApplyGearSwitcherIcon(icon, glow, sprite, active, GearSwitcherSpellActiveColor);
		}

		private static void ApplyGearSwitcherIcon(Image icon, Outline? glow, Sprite? sprite, bool active, Color activeColor)
		{
			if (sprite != null)
			{
				icon.sprite = sprite;
				icon.rectTransform.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height) * GearSwitcherIconScale;
			}
			else
			{
				icon.rectTransform.sizeDelta = new Vector2(44f, 44f) * GearSwitcherIconScale;
			}
			ApplyGearSwitcherIconState(icon, glow, active, activeColor);
		}

		private static void ApplyGearSwitcherRowScale(List<RectTransform> rects, float widthFactor = 1f, bool resetFirst = false)
		{
			if (rects.Count == 0)
			{
				return;
			}
			if (resetFirst)
			{
				foreach (RectTransform rect in rects)
				{
					rect.localScale = Vector3.one;
				}
			}
			float num = float.MaxValue;
			float num2 = float.MinValue;
			foreach (RectTransform rect2 in rects)
			{
				float num3 = rect2.sizeDelta.x * 0.5f * widthFactor;
				float x = rect2.anchoredPosition.x;
				num = Math.Min(num, x - num3);
				num2 = Math.Max(num2, x + num3);
			}
			float num4 = num2 - num;
			float num5 = 800f;
			if (num4 <= 0f)
			{
				return;
			}
			float num6 = ((num4 > num5) ? (num5 / num4) : 1f);
			if (num6 > 1f)
			{
				num6 = 1f;
			}
			foreach (RectTransform rect3 in rects)
			{
				float num7 = num6 * widthFactor;
				rect3.localScale = new Vector3(num7, num7, 1f);
			}
		}

		private void CreateGearSwitcherHpMaskIcon(Transform parent, float offsetX, int value)
		{
			Button button = CreateGearSwitcherIconButton(parent, $"HpMask_{value}", offsetX, 5f, GearSwitcherSpellGlowColor, out RectTransform rectTransform, out Image image, out Outline outline);
			button.onClick.AddListener(delegate
			{
				if (SetSelectedPresetMaxHealth(value))
				{
					GearSwitcher.ApplyStatsImmediate(GetSelectedPreset(), GearSwitcher.StatsPart.MaxHealth);
				}
				UpdateGearSwitcherHpMaskIcons();
			});
			gearSwitcherHpMaskIcons.Add(image);
			gearSwitcherHpMaskGlows.Add(outline);
			gearSwitcherHpMaskIconRects.Add(rectTransform);
		}

		private void CreateGearSwitcherSoulVesselIcon(Transform parent, float offsetX, int value)
		{
			Button button = CreateGearSwitcherIconButton(parent, $"SoulVessel_{value}", offsetX, 0f, GearSwitcherSpellGlowColor, out RectTransform rectTransform, out Image image, out Outline outline);
			button.onClick.AddListener(delegate
			{
				int soulVessels = GetSelectedPreset().SoulVessels;
				int selectedPresetSoulVessels = value;
				if (value == 1 && soulVessels == 1)
				{
					selectedPresetSoulVessels = 0;
				}
				SetSelectedPresetSoulVessels(selectedPresetSoulVessels);
				GearSwitcher.ApplyStatsImmediate(GetSelectedPreset(), GearSwitcher.StatsPart.SoulVessels);
				UpdateGearSwitcherSoulVesselIcons();
			});
			gearSwitcherSoulVesselIcons.Add(image);
			gearSwitcherSoulVesselGlows.Add(outline);
			gearSwitcherSoulVesselIconRects.Add(rectTransform);
		}

		private void CreateGearSwitcherCloakIcon(Transform parent, float offsetX)
		{
			Button button = CreateGearSwitcherIconButton(parent, "CloakIcon", offsetX, 0f, GearSwitcherSpellGlowColor, out RectTransform rectTransform, out Image image, out Outline outline);
			button.onClick.AddListener(delegate
			{
				int selectedCloakLevel = GetSelectedCloakLevel();
				int num = selectedCloakLevel + 1;
				if (num > 2)
				{
					num = 0;
				}
				ApplyCloakLevel(num);
				GearSwitcher.ApplyAbilitiesImmediate(GetSelectedPreset());
				UpdateGearSwitcherCloakRowIcons();
			});
			gearSwitcherCloakRowIconRects.Add(rectTransform);
			gearSwitcherCloakIcon = image;
			gearSwitcherCloakGlow = outline;
		}

		private void CreateGearSwitcherMoveAbilityIcon(Transform parent, string name, float offsetX, string abilityKey, out Image icon, out Outline glow)
		{
			Button button = CreateGearSwitcherIconButton(parent, name, offsetX, 0f, GearSwitcherBindingGlowColor, out RectTransform rectTransform, out Image image, out Outline outline);
			button.onClick.AddListener(delegate
			{
				bool selectedMoveAbility = GetSelectedMoveAbility(abilityKey);
				SetSelectedMoveAbility(abilityKey, !selectedMoveAbility);
				GearSwitcher.ApplyAbilitiesImmediate(GetSelectedPreset());
				UpdateGearSwitcherCloakRowIcons();
			});
			gearSwitcherCloakRowIconRects.Add(rectTransform);
			icon = image;
			glow = outline;
		}

		private void CreateGearSwitcherBindingIcon(Transform parent, string name, float offsetX, string bindingKey, out Image icon, out Outline glow)
		{
			Button button = CreateGearSwitcherIconButton(parent, name, offsetX, 0f, GearSwitcherSpellGlowColor, out RectTransform rectTransform, out Image image, out Outline outline);
			button.onClick.AddListener(delegate
			{
				bool selectedBinding = GetSelectedBinding(bindingKey);
				SetSelectedBinding(bindingKey, !selectedBinding);
				GearSwitcher.ApplyBindingsImmediate(GetSelectedPreset());
				UpdateGearSwitcherBindingsRowIcons();
			});
			gearSwitcherBindingsRowIconRects.Add(rectTransform);
			icon = image;
			glow = outline;
		}

		private void CreateGearSwitcherDreamNailIcon(Transform parent, float offsetX)
		{
			Button button = CreateGearSwitcherIconButton(parent, "DreamNailIcon", offsetX, 0f, GearSwitcherSpellGlowColor, out RectTransform rectTransform, out Image image, out Outline outline);
			button.onClick.AddListener(delegate
			{
				int selectedDreamNailIconLevel = GetSelectedDreamNailIconLevel();
				int num = selectedDreamNailIconLevel + 1;
				if (num > 2)
				{
					num = 0;
				}
				ApplyDreamNailIconLevel(num);
				GearSwitcher.ApplyDreamNailImmediate(GetSelectedPreset());
				UpdateGearSwitcherCloakRowIcons();
			});
			gearSwitcherCloakRowIconRects.Add(rectTransform);
			gearSwitcherDreamNailIcon = image;
			gearSwitcherDreamNailGlow = outline;
		}

		private void CreateGearSwitcherDreamgateIcon(Transform parent, float offsetX)
		{
			Button button = CreateGearSwitcherIconButton(parent, "DreamgateIcon", offsetX, 0f, GearSwitcherSpellGlowColor, out RectTransform rectTransform, out Image image, out Outline outline);
			button.onClick.AddListener(delegate
			{
				bool selectedDreamgateEnabled = GetSelectedDreamgateEnabled();
				SetSelectedDreamgateEnabled(!selectedDreamgateEnabled);
				GearSwitcher.ApplyDreamNailImmediate(GetSelectedPreset());
				UpdateGearSwitcherCloakRowIcons();
			});
			gearSwitcherCloakRowIconRects.Add(rectTransform);
			gearSwitcherDreamgateIcon = image;
			gearSwitcherDreamgateGlow = outline;
		}

		private void CreateGearSwitcherNailArtIcon(Transform parent, string name, float offsetX, string artKey, Action updateAction, out Image icon, out Outline glow)
		{
			Button button = CreateGearSwitcherIconButton(parent, name, offsetX, 0f, GearSwitcherSpellGlowColor, out _, out Image image, out Outline outline);
			button.onClick.AddListener(delegate
			{
				bool selectedNailArt = GetSelectedNailArt(artKey);
				SetSelectedNailArt(artKey, !selectedNailArt);
				GearSwitcher.ApplyNailArtsImmediate(GetSelectedPreset());
				updateAction();
			});
			icon = image;
			glow = outline;
		}

		private void CreateGearSwitcherSpellIcon(Transform parent, string name, float offsetX, string spellKey, Action updateAction, out Image icon, out Outline glow)
		{
			Button button = CreateGearSwitcherIconButton(parent, name, offsetX, 0f, GearSwitcherSpellGlowColor, out _, out Image image, out Outline outline);
			button.onClick.AddListener(delegate
			{
				int num = Mathf.Clamp(GetSelectedSpellLevel(spellKey), 0, 2);
				int num2 = num + 1;
				if (num2 > 2)
				{
					num2 = 0;
				}
				SetSelectedSpellLevel(spellKey, num2);
				GearSwitcher.ApplySpellsImmediate(GetSelectedPreset());
				updateAction();
			});
			icon = image;
			glow = outline;
		}

		private void UpdateGearSwitcherFireballIcon()
		{
			if (gearSwitcherFireballIcon == null)
			{
				return;
			}
			vengefulSpiritSprite ??= LoadCollectorIconSprite("Vengeful Spirit.png", "VengefulSpirit");
			shadeSoulSprite ??= LoadCollectorIconSprite("Shade Soul.png", "ShadeSoul");
			int num = Mathf.Clamp(GetSelectedSpellLevel("fireballLevel"), 0, 2);
			ApplyGearSwitcherIcon(gearSwitcherFireballIcon, gearSwitcherFireballGlow, (num >= 2) ? shadeSoulSprite : vengefulSpiritSprite, num > 0);
		}

		private void UpdateGearSwitcherQuakeIcon()
		{
			if (gearSwitcherQuakeIcon == null)
			{
				return;
			}
			desolateDiveSprite ??= LoadCollectorIconSprite("Desolate Dive.png", "DesolateDive");
			descendingDarkSprite ??= LoadCollectorIconSprite("Descending Dark.png", "DescendingDark");
			int num = Mathf.Clamp(GetSelectedSpellLevel("quakeLevel"), 0, 2);
			ApplyGearSwitcherIcon(gearSwitcherQuakeIcon, gearSwitcherQuakeGlow, (num >= 2) ? descendingDarkSprite : desolateDiveSprite, num > 0);
		}

		private void UpdateGearSwitcherScreamIcon()
		{
			if (gearSwitcherScreamIcon == null)
			{
				return;
			}
			howlingWraithsSprite ??= LoadCollectorIconSprite("Howling Wraiths.png", "HowlingWraiths");
			abyssShriekSprite ??= LoadCollectorIconSprite("Abyss Shriek.png", "AbyssShriek");
			int num = Mathf.Clamp(GetSelectedSpellLevel("screamLevel"), 0, 2);
			ApplyGearSwitcherIcon(gearSwitcherScreamIcon, gearSwitcherScreamGlow, (num >= 2) ? abyssShriekSprite : howlingWraithsSprite, num > 0);
		}

		private void UpdateGearSwitcherCycloneIcon()
		{
			if (gearSwitcherCycloneIcon == null)
			{
				return;
			}
			cycloneSlashSprite ??= LoadCollectorIconSprite("Cyclone Slash.png", "CycloneSlash");
			ApplyGearSwitcherIcon(gearSwitcherCycloneIcon, gearSwitcherCycloneGlow, cycloneSlashSprite, GetSelectedNailArt("hasCyclone"));
		}

		private void UpdateGearSwitcherDashSlashIcon()
		{
			if (gearSwitcherDashSlashIcon == null)
			{
				return;
			}
			dashSlashSprite ??= LoadCollectorIconSprite("Dash Slash.png", "DashSlash");
			ApplyGearSwitcherIcon(gearSwitcherDashSlashIcon, gearSwitcherDashSlashGlow, dashSlashSprite, GetSelectedNailArt("hasUpwardSlash"));
		}

		private void UpdateGearSwitcherGreatSlashIcon()
		{
			if (gearSwitcherGreatSlashIcon == null)
			{
				return;
			}
			greatSlashSprite ??= LoadCollectorIconSprite("Great Slash.png", "GreatSlash");
			ApplyGearSwitcherIcon(gearSwitcherGreatSlashIcon, gearSwitcherGreatSlashGlow, greatSlashSprite, GetSelectedNailArt("hasDashSlash"));
		}

		private void UpdateGearSwitcherCloakRowIcons()
		{
			UpdateGearSwitcherCloakIcon();
			UpdateGearSwitcherMantisClawIcon();
			UpdateGearSwitcherMonarchWingsIcon();
			UpdateGearSwitcherCrystalHeartIcon();
			UpdateGearSwitcherIsmasTearIcon();
			UpdateGearSwitcherDreamNailIcon();
			UpdateGearSwitcherDreamgateIcon();
			UpdateGearSwitcherCloakRowScale();
		}

		private void UpdateGearSwitcherCloakIcon()
		{
			if (gearSwitcherCloakIcon == null)
			{
				return;
			}
			mothwingCloakSprite ??= LoadCollectorIconSprite("Mothwing Cloak.png", "MothwingCloak");
			shadeCloakSprite ??= LoadCollectorIconSprite("Shade Cloak.png", "ShadeCloak");
			int selectedCloakLevel = GetSelectedCloakLevel();
			ApplyGearSwitcherIcon(gearSwitcherCloakIcon, gearSwitcherCloakGlow, (selectedCloakLevel >= 2) ? shadeCloakSprite : mothwingCloakSprite, selectedCloakLevel > 0);
		}

		private void UpdateGearSwitcherMantisClawIcon()
		{
			if (gearSwitcherMantisClawIcon == null)
			{
				return;
			}
			mantisClawSprite ??= LoadCollectorIconSprite("Mantis Claw.png", "MantisClaw");
			ApplyGearSwitcherIcon(gearSwitcherMantisClawIcon, gearSwitcherMantisClawGlow, mantisClawSprite, GetSelectedMoveAbility("Walljump"));
		}

		private void UpdateGearSwitcherMonarchWingsIcon()
		{
			if (gearSwitcherMonarchWingsIcon == null)
			{
				return;
			}
			monarchWingsSprite ??= LoadCollectorIconSprite("Monarch Wings.png", "MonarchWings");
			ApplyGearSwitcherIcon(gearSwitcherMonarchWingsIcon, gearSwitcherMonarchWingsGlow, monarchWingsSprite, GetSelectedMoveAbility("DoubleJump"));
		}

		private void UpdateGearSwitcherCrystalHeartIcon()
		{
			if (gearSwitcherCrystalHeartIcon == null)
			{
				return;
			}
			crystalHeartSprite ??= LoadCollectorIconSprite("Crystal Heart.png", "CrystalHeart");
			ApplyGearSwitcherIcon(gearSwitcherCrystalHeartIcon, gearSwitcherCrystalHeartGlow, crystalHeartSprite, GetSelectedMoveAbility("SuperDash"));
		}

		private void UpdateGearSwitcherIsmasTearIcon()
		{
			if (gearSwitcherIsmasTearIcon == null)
			{
				return;
			}
			ismasTearSprite ??= LoadCollectorIconSprite("Isma's Tear.png", "IsmasTear");
			ApplyGearSwitcherIcon(gearSwitcherIsmasTearIcon, gearSwitcherIsmasTearGlow, ismasTearSprite, GetSelectedMoveAbility("AcidArmour"));
		}

		private void UpdateGearSwitcherDreamNailIcon()
		{
			if (gearSwitcherDreamNailIcon == null)
			{
				return;
			}
			dreamNailSprite ??= LoadCollectorIconSprite("Dream Nail.png", "DreamNail");
			awokenDreamNailSprite ??= LoadCollectorIconSprite("Awoken Dream Nail.png", "AwokenDreamNail");
			int selectedDreamNailIconLevel = GetSelectedDreamNailIconLevel();
			ApplyGearSwitcherIcon(gearSwitcherDreamNailIcon, gearSwitcherDreamNailGlow, (selectedDreamNailIconLevel >= 2) ? awokenDreamNailSprite : dreamNailSprite, selectedDreamNailIconLevel > 0);
		}

		private void UpdateGearSwitcherDreamgateIcon()
		{
			if (gearSwitcherDreamgateIcon == null)
			{
				return;
			}
			dreamgateSprite ??= LoadCollectorIconSprite("Dreamgate.png", "Dreamgate");
			ApplyGearSwitcherIcon(gearSwitcherDreamgateIcon, gearSwitcherDreamgateGlow, dreamgateSprite, GetSelectedDreamgateEnabled());
		}

		private void UpdateGearSwitcherBindingsRowIcons()
		{
			UpdateGearSwitcherBindingNailIcon();
			UpdateGearSwitcherBindingShellIcon();
			UpdateGearSwitcherBindingCharmsIcon();
			UpdateGearSwitcherBindingSoulIcon();
			UpdateGearSwitcherBindingsRowScale();
		}

		private void UpdateGearSwitcherHpMaskIcons()
		{
			if (gearSwitcherHpMaskIcons.Count == 0)
			{
				return;
			}
			hpMaskSprite ??= LoadCollectorIconSprite("HPMask.png", "HPMask");
			int maxHealth = GetEffectiveSelectedPresetMaxHealth();
			for (int i = 0; i < gearSwitcherHpMaskIcons.Count; i++)
			{
				Image image = gearSwitcherHpMaskIcons[i];
				if (hpMaskSprite != null)
				{
					image.sprite = hpMaskSprite;
				}
				image.rectTransform.sizeDelta = new Vector2(GearSwitcherSmallIconSize, GearSwitcherSmallIconSize);
				ApplyGearSwitcherIconState(image, gearSwitcherHpMaskGlows[i], i < maxHealth);
			}
			UpdateGearSwitcherHpMaskRowScale();
		}

		private void UpdateGearSwitcherHpMaskRowScale()
		{
			ApplyGearSwitcherRowScale(gearSwitcherHpMaskIconRects);
		}

		private void UpdateGearSwitcherSoulVesselIcons()
		{
			if (gearSwitcherSoulVesselIcons.Count == 0)
			{
				return;
			}
			soulVesselSprite ??= LoadCollectorIconSprite("Soul Vessel.png", "SoulVessel");
			int num = Math.Max(0, Math.Min(3, GetSelectedPreset().SoulVessels));
			for (int i = 0; i < gearSwitcherSoulVesselIcons.Count; i++)
			{
				Image image = gearSwitcherSoulVesselIcons[i];
				if (soulVesselSprite != null)
				{
					image.sprite = soulVesselSprite;
				}
				image.rectTransform.sizeDelta = new Vector2(GearSwitcherSmallIconSize, GearSwitcherSmallIconSize);
				ApplyGearSwitcherIconState(image, gearSwitcherSoulVesselGlows[i], i < num);
			}
			UpdateGearSwitcherSoulVesselRowScale();
		}

		private void UpdateGearSwitcherSoulVesselRowScale()
		{
			ApplyGearSwitcherRowScale(gearSwitcherSoulVesselIconRects);
		}

		private void UpdateGearSwitcherNaillessIcon()
		{
			if (gearSwitcherNaillessIcon == null)
			{
				return;
			}
			naillessSprite ??= LoadCollectorIconSprite("Nailless.png", "Nailless");
			if (naillessSprite != null)
			{
				gearSwitcherNaillessIcon.sprite = naillessSprite;
			}
			gearSwitcherNaillessIcon.rectTransform.sizeDelta = new Vector2(GearSwitcherBigIconSize, GearSwitcherBigIconSize);
			ApplyGearSwitcherIconState(gearSwitcherNaillessIcon, gearSwitcherNaillessGlow, GetSelectedNailless());
		}

		private void UpdateGearSwitcherOvercharmedIcon()
		{
			if (gearSwitcherOvercharmedIcon == null)
			{
				return;
			}
			overcharmedSprite ??= LoadCollectorIconSprite("OVERCHARMED.png", "Overcharmed");
			if (overcharmedSprite != null)
			{
				gearSwitcherOvercharmedIcon.sprite = overcharmedSprite;
			}
			gearSwitcherOvercharmedIcon.rectTransform.sizeDelta = new Vector2(GearSwitcherBigIconSize, GearSwitcherBigIconSize);
			ApplyGearSwitcherIconState(gearSwitcherOvercharmedIcon, gearSwitcherOvercharmedGlow, GetSelectedOvercharmed());
		}

		private void UpdateGearSwitcherCharmPromptIcon()
		{
			if (!(gearSwitcherCharmPromptIcon == null))
			{
				if (charmsPromptSprite is null)
				{
					charmsPromptSprite = LoadCollectorIconSprite("Charms Prompt.png", "CharmsPrompt");
				}
				if (charmsPromptSprite != null)
				{
					gearSwitcherCharmPromptIcon.sprite = charmsPromptSprite;
					float num = GearSwitcherBigIconSize / charmsPromptSprite.rect.height;
					float x = charmsPromptSprite.rect.width * num;
					gearSwitcherCharmPromptIcon.rectTransform.sizeDelta = new Vector2(x, GearSwitcherBigIconSize);
				}
				else
				{
					gearSwitcherCharmPromptIcon.rectTransform.sizeDelta = new Vector2(GearSwitcherBigIconSize, GearSwitcherBigIconSize);
				}
				gearSwitcherCharmPromptIcon.color = GearSwitcherSpellActiveColor;
			}
		}

		private void UpdateGearSwitcherBindingNailIcon()
		{
			if (gearSwitcherBindingNailIcon == null)
			{
				return;
			}
			nailBindingSprite ??= LoadCollectorIconSprite("Nail Binding.png", "NailBinding") ?? GetBindingDefaultSprite<NailBinding>();
			nailBindingSelectedSprite ??= GetBindingSelectedSprite<NailBinding>();
			bool selectedBinding = GetSelectedBinding("NailBinding");
			ApplyGearSwitcherIcon(gearSwitcherBindingNailIcon, gearSwitcherBindingNailGlow, selectedBinding ? (nailBindingSelectedSprite ?? nailBindingSprite) : nailBindingSprite, selectedBinding, GearSwitcherBindingActiveColor);
		}

		private void UpdateGearSwitcherBindingShellIcon()
		{
			if (gearSwitcherBindingShellIcon == null)
			{
				return;
			}
			shellBindingSprite ??= LoadCollectorIconSprite("Shell Binding.png", "ShellBinding") ?? GetBindingDefaultSprite<ShellBinding>();
			shellBindingSelectedSprite ??= GetBindingSelectedSprite<ShellBinding>();
			bool selectedBinding = GetSelectedBinding("ShellBinding");
			ApplyGearSwitcherIcon(gearSwitcherBindingShellIcon, gearSwitcherBindingShellGlow, selectedBinding ? (shellBindingSelectedSprite ?? shellBindingSprite) : shellBindingSprite, selectedBinding, GearSwitcherBindingActiveColor);
		}

		private void UpdateGearSwitcherBindingCharmsIcon()
		{
			if (gearSwitcherBindingCharmsIcon == null)
			{
				return;
			}
			charmsBindingSprite ??= LoadCollectorIconSprite("Charms Binding.png", "CharmsBinding") ?? GetBindingDefaultSprite<CharmsBinding>();
			charmsBindingSelectedSprite ??= GetBindingSelectedSprite<CharmsBinding>();
			bool selectedBinding = GetSelectedBinding("CharmsBinding");
			ApplyGearSwitcherIcon(gearSwitcherBindingCharmsIcon, gearSwitcherBindingCharmsGlow, selectedBinding ? (charmsBindingSelectedSprite ?? charmsBindingSprite) : charmsBindingSprite, selectedBinding, GearSwitcherBindingActiveColor);
		}

		private void UpdateGearSwitcherBindingSoulIcon()
		{
			if (gearSwitcherBindingSoulIcon == null)
			{
				return;
			}
			soulBindingSprite ??= LoadCollectorIconSprite("Soul Binding.png", "SoulBinding") ?? GetBindingDefaultSprite<SoulBinding>();
			soulBindingSelectedSprite ??= GetBindingSelectedSprite<SoulBinding>();
			bool selectedBinding = GetSelectedBinding("SoulBinding");
			ApplyGearSwitcherIcon(gearSwitcherBindingSoulIcon, gearSwitcherBindingSoulGlow, selectedBinding ? (soulBindingSelectedSprite ?? soulBindingSprite) : soulBindingSprite, selectedBinding, GearSwitcherBindingActiveColor);
		}

		private void UpdateGearSwitcherBindingsRowScale()
		{
			ApplyGearSwitcherRowScale(gearSwitcherBindingsRowIconRects, 0.7f);
		}

		private void UpdateGearSwitcherCloakRowScale()
		{
			ApplyGearSwitcherRowScale(gearSwitcherCloakRowIconRects, resetFirst: true);
		}

		private int GetSelectedDreamNailIconLevel()
		{
			int num = Math.Max(0, Math.Min(3, GetSelectedPreset().DreamNailLevel));
			if (num >= 3)
			{
				return 2;
			}
			return (num >= 1) ? 1 : 0;
		}

		private void ApplyDreamNailIconLevel(int iconLevel)
		{
			iconLevel = Math.Max(0, Math.Min(2, iconLevel));
			int dreamNailLevel = GetSelectedPreset().DreamNailLevel;
			bool flag = dreamNailLevel >= 2;
			switch (iconLevel)
			{
			case 0:
				SetSelectedPresetDreamNail(0);
				break;
			case 1:
				SetSelectedPresetDreamNail((!flag) ? 1 : 2);
				break;
			default:
				SetSelectedPresetDreamNail(3);
				break;
			}
		}
    }
}
