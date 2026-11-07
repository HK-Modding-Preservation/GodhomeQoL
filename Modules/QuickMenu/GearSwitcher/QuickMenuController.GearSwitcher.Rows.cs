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
		private const float GearSwitcherGroupFrameHeight = 72f;
		private const float GearSwitcherFirstGroupSpacing = 72f;
		private const float GearSwitcherGroupSpacing = 82f;
		private const float GearSwitcherHpSoulSpacing = 54f;
		private const float GearSwitcherAfterGroupsSpacing = 70f;
		private const float GearSwitcherPromptSpacing = 98f;
		private const float GearSwitcherBigIconFrameHeight = 80f;
		private const float GearSwitcherBigIconFrameY = -24f;

		private void CreateGearSwitcherSpellsRow(Transform parent, float y)
		{
			GameObject gameObject = CreateRow(parent, "GearSwitcherSpellsRow", y, new Vector2(820f, 44f));
			Image image = gameObject.AddComponent<Image>();
			image.color = new Color(0f, 0f, 0f, 0f);
			AddHkGroupFrame(gameObject.transform, 270f, GearSwitcherGroupFrameHeight, 0f);
			CreateGearSwitcherSpellIcon(gameObject.transform, "FireballIcon", -90f, "fireballLevel", UpdateGearSwitcherFireballIcon, out gearSwitcherFireballIcon, out gearSwitcherFireballGlow);
			CreateGearSwitcherSpellIcon(gameObject.transform, "QuakeIcon", 0f, "quakeLevel", UpdateGearSwitcherQuakeIcon, out gearSwitcherQuakeIcon, out gearSwitcherQuakeGlow);
			CreateGearSwitcherSpellIcon(gameObject.transform, "ScreamIcon", 90f, "screamLevel", UpdateGearSwitcherScreamIcon, out gearSwitcherScreamIcon, out gearSwitcherScreamGlow);
			UpdateGearSwitcherFireballIcon();
			UpdateGearSwitcherQuakeIcon();
			UpdateGearSwitcherScreamIcon();
		}

		private void CreateGearSwitcherNailArtsRow(Transform parent, float y)
		{
			GameObject gameObject = CreateRow(parent, "GearSwitcherNailArtsRow", y, new Vector2(820f, 44f));
			Image image = gameObject.AddComponent<Image>();
			image.color = new Color(0f, 0f, 0f, 0f);
			AddHkGroupFrame(gameObject.transform, 270f, GearSwitcherGroupFrameHeight, 0f);
			CreateGearSwitcherNailArtIcon(gameObject.transform, "CycloneSlashIcon", -90f, "hasCyclone", UpdateGearSwitcherCycloneIcon, out gearSwitcherCycloneIcon, out gearSwitcherCycloneGlow);
			CreateGearSwitcherNailArtIcon(gameObject.transform, "DashSlashIcon", 0f, "hasUpwardSlash", UpdateGearSwitcherDashSlashIcon, out gearSwitcherDashSlashIcon, out gearSwitcherDashSlashGlow);
			CreateGearSwitcherNailArtIcon(gameObject.transform, "GreatSlashIcon", 90f, "hasDashSlash", UpdateGearSwitcherGreatSlashIcon, out gearSwitcherGreatSlashIcon, out gearSwitcherGreatSlashGlow);
			UpdateGearSwitcherCycloneIcon();
			UpdateGearSwitcherDashSlashIcon();
			UpdateGearSwitcherGreatSlashIcon();
		}

		private void CreateGearSwitcherCloakRow(Transform parent, float y)
		{
			GameObject gameObject = CreateRow(parent, "GearSwitcherCloakRow", y, new Vector2(820f, 44f));
			Image image = gameObject.AddComponent<Image>();
			image.color = new Color(0f, 0f, 0f, 0f);
			AddHkGroupFrame(gameObject.transform, 610f, GearSwitcherGroupFrameHeight, 0f);
			gearSwitcherCloakRowIconRects.Clear();
			CreateGearSwitcherCloakIcon(gameObject.transform, -264f);
			CreateGearSwitcherMoveAbilityIcon(gameObject.transform, "MantisClawIcon", -176f, "Walljump", out gearSwitcherMantisClawIcon, out gearSwitcherMantisClawGlow);
			CreateGearSwitcherMoveAbilityIcon(gameObject.transform, "MonarchWingsIcon", -88f, "DoubleJump", out gearSwitcherMonarchWingsIcon, out gearSwitcherMonarchWingsGlow);
			CreateGearSwitcherMoveAbilityIcon(gameObject.transform, "CrystalHeartIcon", 0f, "SuperDash", out gearSwitcherCrystalHeartIcon, out gearSwitcherCrystalHeartGlow);
			CreateGearSwitcherMoveAbilityIcon(gameObject.transform, "IsmasTearIcon", 88f, "AcidArmour", out gearSwitcherIsmasTearIcon, out gearSwitcherIsmasTearGlow);
			CreateGearSwitcherDreamNailIcon(gameObject.transform, 176f);
			CreateGearSwitcherDreamgateIcon(gameObject.transform, 264f);
			UpdateGearSwitcherCloakRowIcons();
		}

		private void CreateGearSwitcherBindingsRow(Transform parent, float y)
		{
			GameObject gameObject = CreateRow(parent, "GearSwitcherBindingsRow", y, new Vector2(820f, 44f));
			Image image = gameObject.AddComponent<Image>();
			image.color = new Color(0f, 0f, 0f, 0f);
			AddHkGroupFrame(gameObject.transform, 340f, GearSwitcherGroupFrameHeight, 0f);
			gearSwitcherBindingsRowIconRects.Clear();
			CreateGearSwitcherBindingIcon(gameObject.transform, "BindingNailIcon", -132f, "NailBinding", out gearSwitcherBindingNailIcon, out gearSwitcherBindingNailGlow);
			CreateGearSwitcherBindingIcon(gameObject.transform, "BindingShellIcon", -44f, "ShellBinding", out gearSwitcherBindingShellIcon, out gearSwitcherBindingShellGlow);
			CreateGearSwitcherBindingIcon(gameObject.transform, "BindingCharmsIcon", 44f, "CharmsBinding", out gearSwitcherBindingCharmsIcon, out gearSwitcherBindingCharmsGlow);
			CreateGearSwitcherBindingIcon(gameObject.transform, "BindingSoulIcon", 132f, "SoulBinding", out gearSwitcherBindingSoulIcon, out gearSwitcherBindingSoulGlow);
			UpdateGearSwitcherBindingsRowIcons();
		}

		private void CreateGearSwitcherHpMaskRow(Transform parent, float y)
		{
			GameObject gameObject = CreateRow(parent, "GearSwitcherHpMaskRow", y, new Vector2(820f, 44f));
			Image image = gameObject.AddComponent<Image>();
			image.color = new Color(0f, 0f, 0f, 0f);
			AddHkGroupFrame(gameObject.transform, 600f, GearSwitcherGroupFrameHeight + GearSwitcherHpSoulSpacing, -GearSwitcherHpSoulSpacing * 0.5f);
			gearSwitcherHpMaskIcons.Clear();
			gearSwitcherHpMaskGlows.Clear();
			gearSwitcherHpMaskIconRects.Clear();
			for (int i = 0; i < 9; i++)
			{
				CreateGearSwitcherHpMaskIcon(gameObject.transform, (float)(i - 4) * 62f, i + 1);
			}
			UpdateGearSwitcherHpMaskIcons();
		}

		private void CreateGearSwitcherSoulVesselRow(Transform parent, float y)
		{
			GameObject gameObject = CreateRow(parent, "GearSwitcherSoulVesselRow", y, new Vector2(820f, 44f));
			Image image = gameObject.AddComponent<Image>();
			image.color = new Color(0f, 0f, 0f, 0f);
			gearSwitcherSoulVesselIcons.Clear();
			gearSwitcherSoulVesselGlows.Clear();
			gearSwitcherSoulVesselIconRects.Clear();
			for (int i = 0; i < 3; i++)
			{
				CreateGearSwitcherSoulVesselIcon(gameObject.transform, (float)(i - 1) * 78f, i + 1);
			}
			UpdateGearSwitcherSoulVesselIcons();
		}

		private void CreateGearSwitcherNaillessRow(Transform parent, float y)
		{
			GameObject gameObject = CreateRow(parent, "GearSwitcherNaillessRow", y, new Vector2(820f, 44f));
			Image image = gameObject.AddComponent<Image>();
			image.color = new Color(0f, 0f, 0f, 0f);
			AddHkGroupFrame(gameObject.transform, 220f, GearSwitcherBigIconFrameHeight, GearSwitcherBigIconFrameY);
			GameObject gameObject2 = new GameObject("NaillessIcon");
			gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
			RectTransform rectTransform = gameObject2.AddComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			rectTransform.anchoredPosition = new Vector2(-50f, -24f);
			rectTransform.sizeDelta = new Vector2(GearSwitcherBigIconSize, GearSwitcherBigIconSize);
			Image image2 = gameObject2.AddComponent<Image>();
			image2.preserveAspect = true;
			Outline outline = gameObject2.AddComponent<Outline>();
			outline.effectColor = GearSwitcherSpellGlowColor;
			outline.effectDistance = new Vector2(2f, -2f);
			outline.enabled = false;
			Button button = gameObject2.AddComponent<Button>();
			button.transition = Selectable.Transition.None;
			button.targetGraphic = image2;
			button.onClick.AddListener(delegate
			{
				bool selectedNailless = GetSelectedNailless();
				SetSelectedNailless(!selectedNailless);
				GearSwitcher.ApplyNailInputImmediate(GetSelectedPreset());
				UpdateGearSwitcherNaillessIcon();
			});
			gearSwitcherNaillessIcon = image2;
			gearSwitcherNaillessGlow = outline;
			UpdateGearSwitcherNaillessIcon();
			GameObject gameObject3 = new GameObject("OvercharmedIcon");
			gameObject3.transform.SetParent(gameObject.transform, worldPositionStays: false);
			RectTransform rectTransform2 = gameObject3.AddComponent<RectTransform>();
			rectTransform2.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform2.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform2.pivot = new Vector2(0.5f, 0.5f);
			rectTransform2.anchoredPosition = new Vector2(50f, -24f);
			rectTransform2.sizeDelta = new Vector2(GearSwitcherBigIconSize, GearSwitcherBigIconSize);
			Image image3 = gameObject3.AddComponent<Image>();
			image3.preserveAspect = true;
			Outline outline2 = gameObject3.AddComponent<Outline>();
			outline2.effectColor = GearSwitcherSpellGlowColor;
			outline2.effectDistance = new Vector2(2f, -2f);
			outline2.enabled = false;
			Button button2 = gameObject3.AddComponent<Button>();
			button2.transition = Selectable.Transition.None;
			button2.targetGraphic = image3;
			button2.onClick.AddListener(delegate
			{
				bool selectedOvercharmed = GetSelectedOvercharmed();
				SetSelectedOvercharmed(!selectedOvercharmed);
				GearSwitcher.ApplyOvercharmedImmediate(GetSelectedPreset());
				UpdateGearSwitcherOvercharmedIcon();
			});
			gearSwitcherOvercharmedIcon = image3;
			gearSwitcherOvercharmedGlow = outline2;
			UpdateGearSwitcherOvercharmedIcon();
		}

		private void CreateGearSwitcherCharmPromptRow(Transform parent, float y)
		{
			GameObject gameObject = CreateRow(parent, "GearSwitcherCharmPromptRow", y, new Vector2(820f, 88f));
			Image image = gameObject.AddComponent<Image>();
			image.color = new Color(0f, 0f, 0f, 0f);
			AddHkGroupFrame(gameObject.transform, 120f, GearSwitcherBigIconFrameHeight, GearSwitcherBigIconFrameY);
			GameObject gameObject2 = new GameObject("CharmsPromptIcon");
			gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
			RectTransform rectTransform = gameObject2.AddComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			rectTransform.anchoredPosition = new Vector2(0f, -24f);
			rectTransform.sizeDelta = new Vector2(GearSwitcherBigIconSize, GearSwitcherBigIconSize);
			Image image2 = gameObject2.AddComponent<Image>();
			image2.preserveAspect = true;
			Button button = gameObject2.AddComponent<Button>();
			button.transition = Selectable.Transition.None;
			button.targetGraphic = image2;
			button.onClick.AddListener(OnGearSwitcherCharmPromptClicked);
			gearSwitcherCharmPromptIcon = image2;
			UpdateGearSwitcherCharmPromptIcon();
		}
    }
}
