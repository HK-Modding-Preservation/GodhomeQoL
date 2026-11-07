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
		private const float GearSwitcherCharmCostTopPadding = 74f;
		private const float CharmNotchInfoY = 124f;
		private const float CharmNotchInfoHeight = 30f;
		private const float CharmCostIconY = 26f;
		private const float CharmCostControlsY = -36f;
		private const float CharmCostButtonOffset = 36f;

		private Transform CreateGearSwitcherCharmCostsRow(Transform parent, string name, float y)
		{
			GameObject gameObject = CreateRow(parent, name, y, new Vector2(820f, GearSwitcherCharmCostRowHeight));
			Image image = gameObject.AddComponent<Image>();
			image.color = new Color(0f, 0f, 0f, 0f);
			return gameObject.transform;
		}

		private float CharmCostColumnX(int index)
		{
			return -300f + 150f * (float)index;
		}

		private Image CreateCharmCostColumn(Transform parent, string name, float columnCenterX, string spriteName, string spriteKey, ref Text? valueField, Action<int> adjustCost, Action? iconClick = null, bool allowAdjust = true)
		{
			GameObject gameObject = new GameObject(name + "Icon");
			gameObject.transform.SetParent(parent, worldPositionStays: false);
			RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			rectTransform.anchoredPosition = new Vector2(columnCenterX, CharmCostIconY);
			rectTransform.sizeDelta = new Vector2(44f, 44f);
			Image image = gameObject.AddComponent<Image>();
			image.preserveAspect = true;
			Outline outline = gameObject.AddComponent<Outline>();
			outline.effectColor = GearSwitcherCharmCostGlowColor;
			outline.effectDistance = new Vector2(4f, -4f);
			outline.enabled = false;
			gearSwitcherCharmCostGlows[name] = outline;
			Sprite? sprite = LoadCollectorIconSprite(spriteName, spriteKey);
			if (sprite != null)
			{
				image.sprite = sprite;
				float num = Mathf.Min(1.4f, 56f / sprite.rect.height);
				rectTransform.sizeDelta = new Vector2(sprite.rect.width * num, sprite.rect.height * num);
			}
			if (GearSwitcher.TryGetCharmId(spriteKey, out int charmId))
			{
				gearSwitcherCharmIcons[charmId] = image;
				iconClick = delegate
				{
					OnGearSwitcherCharmIconClicked(charmId);
				};
			}
			if (iconClick != null)
			{
				Button button = gameObject.AddComponent<Button>();
				button.transition = Selectable.Transition.None;
				button.targetGraphic = image;
				button.onClick.AddListener(delegate
				{
					iconClick();
				});
			}
			GameObject gameObject2 = new GameObject(name + "Value");
			gameObject2.transform.SetParent(parent, worldPositionStays: false);
			RectTransform rectTransform2 = gameObject2.AddComponent<RectTransform>();
			rectTransform2.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform2.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform2.pivot = new Vector2(0.5f, 0.5f);
			rectTransform2.anchoredPosition = new Vector2(columnCenterX, CharmCostControlsY);
			rectTransform2.sizeDelta = new Vector2(40f, 44f);
			Text text = CreateText(gameObject2.transform, "Value", "1", 22, TextAnchor.MiddleCenter);
			RectTransform rectTransform3 = text.rectTransform;
			rectTransform3.anchorMin = Vector2.zero;
			rectTransform3.anchorMax = Vector2.one;
			rectTransform3.offsetMin = Vector2.zero;
			rectTransform3.offsetMax = Vector2.zero;
			valueField = text;
			if (allowAdjust)
			{
				Button button2 = CreateCenteredMiniButton(parent, name + "Minus", "-", new Vector2(columnCenterX - CharmCostButtonOffset, CharmCostControlsY));
				button2.onClick.AddListener(delegate
				{
					adjustCost(-1);
				});
				Button button3 = CreateCenteredMiniButton(parent, name + "Plus", "+", new Vector2(columnCenterX + CharmCostButtonOffset, CharmCostControlsY));
				button3.onClick.AddListener(delegate
				{
					adjustCost(1);
				});
			}
			return image;
		}

		private void AddCharmSwapBadge(Image? icon)
		{
			if (!(icon == null))
			{
				GameObject gameObject = new GameObject("SwapBadge");
				gameObject.transform.SetParent(icon.transform, worldPositionStays: false);
				RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
				rectTransform.anchorMin = new Vector2(1f, 1f);
				rectTransform.anchorMax = new Vector2(1f, 1f);
				rectTransform.pivot = new Vector2(1f, 1f);
				rectTransform.anchoredPosition = new Vector2(-2f, -2f);
				rectTransform.sizeDelta = new Vector2(46f, 22f);
				Text text = gameObject.AddComponent<Text>();
				text.text = "SWAP";
				text.font = GetMenuFont();
				text.fontSize = 13;
				text.alignment = TextAnchor.MiddleCenter;
				text.color = new Color(1f, 1f, 1f, 0.9f);
				text.raycastTarget = true;
				Outline outline = gameObject.AddComponent<Outline>();
				outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
				outline.effectDistance = new Vector2(1f, -1f);
				Action swap = (icon == gearSwitcherCarefreeIcon) ? OnGearSwitcherCarefreeToggle : OnGearSwitcherVoidHeartToggle;
				Button badgeButton = gameObject.AddComponent<Button>();
				badgeButton.transition = Selectable.Transition.None;
				badgeButton.targetGraphic = text;
				badgeButton.onClick.AddListener(delegate
				{
					swap();
				});
			}
		}

		private void CreateCharmCostAdjustButtons(Transform parent, string name, float columnCenterX, Action<int> adjustCost)
		{
			Button button = CreateCenteredMiniButton(parent, name + "Minus", "-", new Vector2(columnCenterX - CharmCostButtonOffset, CharmCostControlsY));
			button.onClick.AddListener(delegate
			{
				adjustCost(-1);
			});
			button.transform.SetAsLastSibling();
			Button button2 = CreateCenteredMiniButton(parent, name + "Plus", "+", new Vector2(columnCenterX + CharmCostButtonOffset, CharmCostControlsY));
			button2.onClick.AddListener(delegate
			{
				adjustCost(1);
			});
			button2.transform.SetAsLastSibling();
		}

		private void RegisterCharmCostHighlight(string key, Text? valueText, Func<GearPreset, int> currentCost, Func<GearPreset, int> defaultCost)
		{
			if (!(valueText == null) && gearSwitcherCharmCostGlows.TryGetValue(key, out Outline value))
			{
				gearSwitcherCharmCostHighlightEntries.Add(new CharmCostHighlightEntry(valueText, value, currentCost, defaultCost));
			}
		}

		private void RegisterGearSwitcherCharmCostHighlights()
		{
			gearSwitcherCharmCostHighlightEntries.Clear();
			for (int i = 0; i < CharmCostDefinitions.Length; i++)
			{
				CharmCostDefinition definition = CharmCostDefinitions[i];
				RegisterCharmCostHighlight(definition.Key, gearSwitcherCharmCostValues[i], definition.Get, (GearPreset _) => definition.Get(DefaultGearPreset));
			}
			RegisterCharmCostHighlight("CarefreeMelody", gearSwitcherCarefreeMelodyCostValue, (GearPreset preset) => GetPresetUseGrimmchild(preset) ? preset.GrimmchildCost : preset.CarefreeMelodyCost, (GearPreset preset) => GetPresetUseGrimmchild(preset) ? DefaultGearPreset.GrimmchildCost : DefaultGearPreset.CarefreeMelodyCost);
			RegisterCharmCostHighlight("VoidHeart", gearSwitcherVoidHeartCostValue, (GearPreset preset) => GetPresetUseVoidHeart(preset) ? preset.VoidHeartCost : preset.KingsoulCost, (GearPreset preset) => GetPresetUseVoidHeart(preset) ? DefaultGearPreset.VoidHeartCost : DefaultGearPreset.KingsoulCost);
		}

		private void UpdateGearSwitcherCharmCostHighlights()
		{
			if (gearSwitcherCharmCostHighlightEntries.Count == 0)
			{
				return;
			}
			GearPreset selectedPreset = GetSelectedPreset();
			foreach (CharmCostHighlightEntry gearSwitcherCharmCostHighlightEntry in gearSwitcherCharmCostHighlightEntries)
			{
				int num = Math.Max(0, Math.Min(99, gearSwitcherCharmCostHighlightEntry.CurrentCost(selectedPreset)));
				int num2 = Math.Max(0, Math.Min(99, gearSwitcherCharmCostHighlightEntry.DefaultCost(selectedPreset)));
				bool flag = num != num2;
				gearSwitcherCharmCostHighlightEntry.Glow.enabled = flag;
				gearSwitcherCharmCostHighlightEntry.ValueText.color = (flag ? GearSwitcherCharmCostChangedValueColor : Color.white);
			}
		}

		private void OnGearSwitcherCharmCostBackClicked()
		{
			SetGearSwitcherCharmCostVisible(value: false);
			SetGearSwitcherVisible(value: true);
		}

		private void OnGearSwitcherCharmCostResetDefaultsClicked()
		{
			GearPreset selectedPreset = GetSelectedPreset();
			foreach (CharmCostDefinition definition in CharmCostDefinitions)
			{
				definition.Set(selectedPreset, definition.Get(DefaultGearPreset));
			}
			selectedPreset.CarefreeMelodyCost = DefaultGearPreset.CarefreeMelodyCost;
			selectedPreset.GrimmchildCost = DefaultGearPreset.GrimmchildCost;
			selectedPreset.VoidHeartCost = DefaultGearPreset.VoidHeartCost;
			selectedPreset.KingsoulCost = DefaultGearPreset.KingsoulCost;
			selectedPreset.CarefreeMelodyCostInitialized = true;
			selectedPreset.KingsoulCostInitialized = true;
			GodhomeQoL.SaveGlobalSettingsSafe();
			GearSwitcher.ApplyCharmCostsImmediate(selectedPreset);
			RefreshGearSwitcherCharmCostUi();
		}

		private void OnGearSwitcherCharmCostAheClicked()
		{
			GearPreset selectedPreset = GetSelectedPreset();
			foreach (CharmCostDefinition definition in CharmCostDefinitions)
			{
				definition.Set(selectedPreset, definition.AheCost);
			}
			selectedPreset.GrimmchildCost = 0;
			selectedPreset.CarefreeMelodyCost = 2;
			selectedPreset.KingsoulCost = 2;
			selectedPreset.VoidHeartCost = 0;
			selectedPreset.CarefreeMelodyCostInitialized = true;
			selectedPreset.KingsoulCostInitialized = true;
			GodhomeQoL.SaveGlobalSettingsSafe();
			GearSwitcher.ApplyCharmCostsImmediate(selectedPreset);
			RefreshGearSwitcherCharmCostUi();
			MarkGearSwitcherPresetEdited();
		}

		private void RefreshGearSwitcherCharmCostUi()
		{
			for (int i = 0; i < CharmCostDefinitions.Length; i++)
			{
				UpdateCharmCostValue(i);
			}
			UpdateGearSwitcherCarefreeMelodyCostValue();
			UpdateGearSwitcherVoidHeartCostValue();
			UpdateGearSwitcherVoidHeartIcon();
			UpdateGearSwitcherCarefreeIcon();
			UpdateGearSwitcherCharmCostHighlights();
			UpdateGearSwitcherCharmIcons();
			UpdateGearSwitcherCharmNotches();
		}

		private static readonly Color GearSwitcherCharmPickDimColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);

		private static readonly Color GearSwitcherCharmPickMessageColor = new Color(1f, 0.55f, 0.45f, 1f);

		private void OnGearSwitcherCharmIconClicked(int charmId)
		{
			GearPreset preset = GetSelectedPreset();
			bool equip = !GearSwitcher.IsCharmSelected(preset, charmId);
			string? message = null;
			switch (GearSwitcher.TrySetCharmEquipped(gearSwitcherSelectedPreset, preset, charmId, equip))
			{
			case GearSwitcher.CharmSelectResult.BuiltinGear:
				message = "Charms can only be chosen on your own gears";
				break;
			case GearSwitcher.CharmSelectResult.CharmsBound:
				message = "Charms are bound by this gear";
				break;
			case GearSwitcher.CharmSelectResult.NoNotches:
				message = "No free notches: take a charm off first";
				break;
			}
			UpdateGearSwitcherCharmIcons();
			UpdateGearSwitcherCharmNotches(message);
		}

		private void UpdateGearSwitcherCharmIcons()
		{
			GearPreset preset = GetSelectedPreset();
			foreach (KeyValuePair<int, Image> entry in gearSwitcherCharmIcons)
			{
				if (entry.Value != null)
				{
					entry.Value.color = GearSwitcher.IsCharmSelected(preset, entry.Key) ? Color.white : GearSwitcherCharmPickDimColor;
				}
			}
		}

		private void UpdateGearSwitcherCharmNotches(string? message = null)
		{
			Text? label = EnsureGearSwitcherCharmNotchText();
			if (label == null)
			{
				return;
			}
			GearPreset preset = GetSelectedPreset();
			if (message != null)
			{
				label.text = message;
				label.color = GearSwitcherCharmPickMessageColor;
				return;
			}
			int used = GearSwitcher.GetPlannedNotchesUsed(preset);
			int slots = GearSwitcher.GetPresetNotchSlots(preset);
			bool overcharmed = used > slots;
			string temporary = IsBuiltinPresetName(gearSwitcherSelectedPreset) ? " - temporary, resets when you switch gear" : string.Empty;
			label.text = (overcharmed ? $"Notches {used}/{slots} (Overcharmed)" : $"Notches {used}/{slots}") + temporary;
			label.color = overcharmed ? GearSwitcherCharmPickMessageColor : Color.white;
		}

		private Text? EnsureGearSwitcherCharmNotchText()
		{
			if (gearSwitcherCharmNotchText != null)
			{
				return gearSwitcherCharmNotchText;
			}
			if (gearSwitcherCharmCostRoot == null)
			{
				return null;
			}
			Transform? panel = gearSwitcherCharmCostRoot.GetComponentsInChildren<Transform>(true).FirstOrDefault((Transform t) => t.name == "GearSwitcherCharmCostPanel");
			if (panel == null)
			{
				return null;
			}
			Text text = CreateText(panel, "CharmNotchInfo", string.Empty, 24, TextAnchor.MiddleCenter);
			text.resizeTextForBestFit = true;
			text.resizeTextMaxSize = 24;
			text.resizeTextMinSize = 16;
			text.horizontalOverflow = HorizontalWrapMode.Wrap;
			text.verticalOverflow = VerticalWrapMode.Truncate;
			RectTransform rectTransform = text.rectTransform;
			rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			rectTransform.anchoredPosition = new Vector2(0f, CharmNotchInfoY);
			rectTransform.sizeDelta = new Vector2(760f, CharmNotchInfoHeight);
			gearSwitcherCharmNotchText = text;
			return text;
		}

		private void AdjustCarefreeMelodyCost(int delta)
		{
			GearPreset selectedPreset = GetSelectedPreset();
			if (GetPresetUseGrimmchild(selectedPreset))
			{
				AdjustCharmCost((GearPreset p) => p.GrimmchildCost, delegate(GearPreset p, int value)
				{
					p.GrimmchildCost = value;
				}, UpdateGearSwitcherCarefreeMelodyCostValue, delta);
				return;
			}
			selectedPreset.CarefreeMelodyCostInitialized = true;
			AdjustCharmCost((GearPreset p) => p.CarefreeMelodyCost, delegate(GearPreset p, int value)
			{
				p.CarefreeMelodyCost = value;
			}, UpdateGearSwitcherCarefreeMelodyCostValue, delta);
		}

		private void AdjustVoidHeartCost(int delta)
		{
			GearPreset selectedPreset = GetSelectedPreset();
			if (GetPresetUseVoidHeart(selectedPreset))
			{
				AdjustCharmCost((GearPreset p) => p.VoidHeartCost, delegate(GearPreset p, int value)
				{
					p.VoidHeartCost = value;
				}, UpdateGearSwitcherVoidHeartCostValue, delta);
				return;
			}
			selectedPreset.KingsoulCostInitialized = true;
			AdjustCharmCost((GearPreset p) => p.KingsoulCost, delegate(GearPreset p, int value)
			{
				p.KingsoulCost = value;
			}, UpdateGearSwitcherVoidHeartCostValue, delta);
		}

		private bool GetPresetUseVoidHeart(GearPreset preset)
		{
			return preset.UseVoidHeart;
		}

		private bool GetPresetUseGrimmchild(GearPreset preset)
		{
			return preset.UseGrimmchild;
		}

		private void OnGearSwitcherCarefreeToggle()
		{
			GearPreset selectedPreset = GetSelectedPreset();
			if (!GearSwitcher.CanSwapCharmVariant(selectedPreset, 40))
			{
				UpdateGearSwitcherCharmNotches("No free notches: take a charm off first");
				return;
			}
			bool flag = (selectedPreset.UseGrimmchild = !GetPresetUseGrimmchild(selectedPreset));
			if (!flag && !selectedPreset.CarefreeMelodyCostInitialized)
			{
				selectedPreset.CarefreeMelodyCost = 3;
				selectedPreset.CarefreeMelodyCostInitialized = true;
			}
			if (flag && selectedPreset.GrimmchildCost < 0)
			{
				selectedPreset.GrimmchildCost = 2;
			}
			GodhomeQoL.SaveGlobalSettingsSafe();
			GearSwitcher.ApplyCharmCostsImmediate(selectedPreset);
			UpdateGearSwitcherCarefreeIcon();
			UpdateGearSwitcherCarefreeMelodyCostValue();
			UpdateGearSwitcherCharmCostHighlights();
			UpdateGearSwitcherCharmNotches();
			MarkGearSwitcherPresetEdited();
		}

		private void OnGearSwitcherVoidHeartToggle()
		{
			GearPreset selectedPreset = GetSelectedPreset();
			if (!GearSwitcher.CanSwapCharmVariant(selectedPreset, 36))
			{
				UpdateGearSwitcherCharmNotches("No free notches: take a charm off first");
				return;
			}
			bool flag = (selectedPreset.UseVoidHeart = !GetPresetUseVoidHeart(selectedPreset));
			if (!flag && !selectedPreset.KingsoulCostInitialized)
			{
				selectedPreset.KingsoulCost = 5;
				selectedPreset.KingsoulCostInitialized = true;
			}
			GodhomeQoL.SaveGlobalSettingsSafe();
			GearSwitcher.ApplyCharmCostsImmediate(selectedPreset);
			UpdateGearSwitcherVoidHeartIcon();
			UpdateGearSwitcherVoidHeartCostValue();
			UpdateGearSwitcherCharmCostHighlights();
			UpdateGearSwitcherCharmNotches();
			MarkGearSwitcherPresetEdited();
		}

		private void UpdateGearSwitcherCarefreeMelodyCostValue()
		{
			if (!(gearSwitcherCarefreeMelodyCostValue == null))
			{
				GearPreset selectedPreset = GetSelectedPreset();
				int val = (GetPresetUseGrimmchild(selectedPreset) ? selectedPreset.GrimmchildCost : selectedPreset.CarefreeMelodyCost);
				gearSwitcherCarefreeMelodyCostValue.text = Math.Max(0, Math.Min(99, val)).ToString();
			}
		}

		private void UpdateGearSwitcherVoidHeartCostValue()
		{
			if (!(gearSwitcherVoidHeartCostValue == null))
			{
				GearPreset selectedPreset = GetSelectedPreset();
				int val = (GetPresetUseVoidHeart(selectedPreset) ? selectedPreset.VoidHeartCost : selectedPreset.KingsoulCost);
				gearSwitcherVoidHeartCostValue.text = Math.Max(0, Math.Min(99, val)).ToString();
			}
		}

		private void UpdateGearSwitcherVoidHeartIcon()
		{
			if (!(gearSwitcherVoidHeartIcon == null))
			{
				if (voidHeartSprite is null)
				{
					voidHeartSprite = LoadCollectorIconSprite("Void Heart.png", "VoidHeart");
				}
				if (kingsoulSprite is null)
				{
					kingsoulSprite = LoadCollectorIconSprite("Kingsoul.png", "Kingsoul");
				}
				GearPreset selectedPreset = GetSelectedPreset();
				Sprite? sprite = (GetPresetUseVoidHeart(selectedPreset) ? voidHeartSprite : kingsoulSprite);
				if (sprite != null)
				{
					gearSwitcherVoidHeartIcon.sprite = sprite;
					float num = Mathf.Min(1.4f, 56f / sprite.rect.height);
					gearSwitcherVoidHeartIcon.rectTransform.sizeDelta = new Vector2(sprite.rect.width * num, sprite.rect.height * num);
				}
				else
				{
					gearSwitcherVoidHeartIcon.rectTransform.sizeDelta = new Vector2(44f, 44f);
				}
			}
		}

		private void UpdateGearSwitcherCarefreeIcon()
		{
			if (!(gearSwitcherCarefreeIcon == null))
			{
				if (grimmchildSprite is null)
				{
					grimmchildSprite = LoadCollectorIconSprite("Grimmchild.png", "Grimmchild");
				}
				if (carefreeMelodySprite is null)
				{
					carefreeMelodySprite = LoadCollectorIconSprite("Carefree Melody.png", "CarefreeMelody");
				}
				GearPreset selectedPreset = GetSelectedPreset();
				Sprite? sprite = (GetPresetUseGrimmchild(selectedPreset) ? grimmchildSprite : carefreeMelodySprite);
				if (sprite != null)
				{
					gearSwitcherCarefreeIcon.sprite = sprite;
					float num = Mathf.Min(1.4f, 56f / sprite.rect.height);
					gearSwitcherCarefreeIcon.rectTransform.sizeDelta = new Vector2(sprite.rect.width * num, sprite.rect.height * num);
				}
				else
				{
					gearSwitcherCarefreeIcon.rectTransform.sizeDelta = new Vector2(44f, 44f);
				}
			}
		}

		private void AdjustCharmCost(Func<GearPreset, int> getter, Action<GearPreset, int> setter, Action updateAction, int delta)
		{
			GearPreset selectedPreset = GetSelectedPreset();
			int num = Math.Max(0, Math.Min(99, getter(selectedPreset)));
			int num2 = Math.Max(0, Math.Min(99, num + delta));
			if (num2 != num)
			{
				setter(selectedPreset, num2);
				GodhomeQoL.SaveGlobalSettingsSafe();
				GearSwitcher.ApplyCharmCostsImmediate(selectedPreset);
				updateAction();
				UpdateGearSwitcherCharmCostHighlights();
				UpdateGearSwitcherCharmNotches();
				MarkGearSwitcherPresetEdited();
			}
		}

		private void AdjustCharmCost(int index, int delta)
		{
			CharmCostDefinition definition = CharmCostDefinitions[index];
			AdjustCharmCost(definition.Get, definition.Set, delegate
			{
				UpdateCharmCostValue(index);
			}, delta);
		}

		private void UpdateCharmCostValue(int index)
		{
			UpdateCharmCostValue(gearSwitcherCharmCostValues[index], CharmCostDefinitions[index].Get);
		}

		private void UpdateCharmCostValue(Text? valueField, Func<GearPreset, int> getter)
		{
			if (!(valueField == null))
			{
				GearPreset selectedPreset = GetSelectedPreset();
				valueField.text = Math.Max(0, Math.Min(99, getter(selectedPreset))).ToString();
			}
		}
    }
}
