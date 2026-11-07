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
		private void OnGearSwitcherPresetClicked()
		{
			SetGearSwitcherVisible(value: false);
			SetGearSwitcherPresetVisible(value: true);
		}

		private void OnGearSwitcherPresetBackClicked()
		{
			SetGearSwitcherPresetVisible(value: false);
			SetGearSwitcherVisible(value: true);
		}

		private void OnGearSwitcherResetGearClicked()
		{
			GetSelectedPreset();
			string presetName = gearSwitcherSelectedPreset;
			if (!GearSwitcher.ResetBuiltinPreset(presetName))
			{
				return;
			}
			GearSwitcher.ApplyPreset(presetName, allowQueue: true);
			RefreshGearSwitcherUi();
		}

		private void UpdateGearSwitcherResetGearVisibility()
		{
			if (gearSwitcherResetGearRow == null)
			{
				return;
			}
			GetSelectedPreset();
			gearSwitcherResetGearRow.SetActive(IsBuiltinPresetName(gearSwitcherSelectedPreset));
		}

		private void OnGearSwitcherPresetCreateClicked()
		{
			string value = GearSwitcher.CreateCustomPresetFromCurrentGear();
			if (!string.IsNullOrWhiteSpace(value))
			{
				gearSwitcherSelectedPreset = value;
				GearSwitcher.ApplyPreset(gearSwitcherSelectedPreset, allowQueue: true);
				RebuildGearSwitcherPresetOverlay();
				SetGearSwitcherPresetVisible(value: false);
				SetGearSwitcherVisible(value: true);
			}
		}

		private void OnGearSwitcherPresetSelected(int index)
		{
			string[] gearSwitcherPresetOptions = GetGearSwitcherPresetOptions();
			if (index >= 0 && index < gearSwitcherPresetOptions.Length)
			{
				gearSwitcherSelectedPreset = gearSwitcherPresetOptions[index];
				if (IsFullGearPreset(gearSwitcherSelectedPreset))
				{
					ResetFullGearBindings();
					GearSwitcher.RestoreAllBindingsImmediate();
				}
				GearSwitcher.ApplyPreset(gearSwitcherSelectedPreset, allowQueue: true);
				SetGearSwitcherPresetVisible(value: false);
				SetGearSwitcherVisible(value: true);
			}
		}

		private void RebuildGearSwitcherPresetOverlay()
		{
			bool active = gearSwitcherPresetVisible;
			DestroyRoot(ref gearSwitcherPresetRoot);
			BuildGearSwitcherPresetOverlayUi();
			if (gearSwitcherPresetRoot != null)
			{
				gearSwitcherPresetRoot.SetActive(active);
			}
		}

		private void ResetFullGearBindings()
		{
			if (GearSwitcherSettings.Presets == null || GearSwitcherSettings.Presets.Count == 0)
			{
				GearSwitcherSettings.Presets = GearPresetDefaults.CreateDefaults();
			}
			if (GearSwitcherSettings.Presets.TryGetValue("FullGear", out GearPreset value))
			{
				value.HasAllBindings = false;
				GearPreset gearPreset = value;
				if (gearPreset.Bindings == null)
				{
					Dictionary<string, bool> dictionary = (gearPreset.Bindings = new Dictionary<string, bool>());
				}
				value.Bindings["CharmsBinding"] = false;
				value.Bindings["NailBinding"] = false;
				value.Bindings["ShellBinding"] = false;
				value.Bindings["SoulBinding"] = false;
				GodhomeQoL.SaveGlobalSettingsSafe();
			}
		}

		private void RefreshGearSwitcherPresetSelectUi()
		{
			string[] gearSwitcherPresetOptions = GetGearSwitcherPresetOptions();
			int gearSwitcherPresetIndex = GetGearSwitcherPresetIndex(gearSwitcherPresetOptions);
			foreach (GearSwitcherPresetEntry gearSwitcherPresetEntry in gearSwitcherPresetEntries)
			{
				string presetName = ((gearSwitcherPresetEntry.Index >= 0 && gearSwitcherPresetEntry.Index < gearSwitcherPresetOptions.Length) ? gearSwitcherPresetOptions[gearSwitcherPresetEntry.Index] : string.Empty);
				gearSwitcherPresetEntry.Label.text = GetGearSwitcherPresetDisplayName(presetName);
				gearSwitcherPresetEntry.Highlight.SetManualActive(gearSwitcherPresetEntry.Index == gearSwitcherPresetIndex);
			}
		}

		private void RefreshGearSwitcherPresetFields()
		{
			GearPreset selectedPreset = GetSelectedPreset();
			UpdateIntInputValue(gearSwitcherNailDamageField, selectedPreset.NailDamage);
			UpdateGearSwitcherBaseNailDamage();
			UpdateIntInputValue(gearSwitcherCharmSlotsField, selectedPreset.CharmSlots);
			UpdateIntInputValue(gearSwitcherMainSoulGainField, selectedPreset.MainSoulGain);
			UpdateIntInputValue(gearSwitcherReserveSoulGainField, selectedPreset.ReserveSoulGain);
			UpdateGearSwitcherFireballIcon();
			UpdateGearSwitcherQuakeIcon();
			UpdateGearSwitcherScreamIcon();
			UpdateGearSwitcherCycloneIcon();
			UpdateGearSwitcherDashSlashIcon();
			UpdateGearSwitcherGreatSlashIcon();
			UpdateGearSwitcherCloakRowIcons();
			UpdateGearSwitcherBindingsRowIcons();
			UpdateGearSwitcherHpMaskIcons();
			UpdateGearSwitcherSoulVesselIcons();
			UpdateGearSwitcherNaillessIcon();
			UpdateGearSwitcherOvercharmedIcon();
			UpdateGearSwitcherCharmPromptIcon();
			RefreshGearSwitcherCharmCostUi();
			UpdateGearSwitcherSelectedPresetColor(gearSwitcherSelectedPreset);
		}
    }
}
