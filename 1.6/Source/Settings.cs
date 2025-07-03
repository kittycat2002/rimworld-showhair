using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ShowHair;

internal enum HatEnum
{
	HideHat,
	ShowsHair,
	HidesAllHair,
	HidesHairShowsBeard,
	ShowsHairHidesBeard
}

[UsedImplicitly]
internal class ShowHairMod : Mod
{
	private static Settings? settings;
	private Vector2 scrollPositionSettingsEntries = new(0f, 0f);
	private float viewHeightSettingsEntries = 1000f;
	private Vector2 scrollPositionHairSection = new(0f, 0f);
	private float viewHeightHairSection = 1000f;

	internal static Settings Settings
	{
		get
		{
			if (settings is null)
			{
				throw new NullReferenceException();
			}

			return settings;
		}
	}

	public ShowHairMod(ModContentPack content) : base(content)
	{
		if (ParseHelper.Parsers<ulong>.parser == null)
		{
			ParseHelper.Parsers<ulong>.Register(ulong.Parse);
		}

		settings = GetSettings<Settings>();
	}

	public override string SettingsCategory()
	{
		return "ShowHair.ShowHair".Translate();
	}

	public override void WriteSettings()
	{
		base.WriteSettings();
		settings?.ClearCache();
		PortraitsCache.Clear();
		GlobalTextureAtlasManager.FreeAllRuntimeAtlases();
	}

	public override void DoSettingsWindowContents(Rect rect)
	{
		Listing_Standard listing = new();
		listing.Begin(rect);
		listing.CheckboxLabeled("ShowHair.OnlyApplyToColonists".Translate(), ref Settings.onlyApplyToColonists);
		listing.CheckboxLabeled("ShowHair.UseDontShaveHead".Translate(), ref Settings.useDontShaveHead);
		listing.GapLine();
		Listing_Standard section = new();
		section.Begin(listing.GetRect(rect.height - listing.CurHeight));
		section.ColumnWidth = (section.listingRect.width - 17f) / 2;
		DrawSettingEntries(section);
		section.NewColumn();
		Rect rect2 = new(section.curX, section.CurHeight, section.ColumnWidth,
			section.listingRect.height - section.CurHeight);
		Settings.HairSelectorUI.DrawSection(rect2, ref scrollPositionHairSection, ref viewHeightHairSection);
		listing.EndSection(section);
		listing.End();
	}

	private void DrawSettingEntries(Listing_Standard listing)
	{
		if (listing.ButtonText("Add Setting Entry"))
		{
			Settings.AddEntry(new SettingEntry(Settings));
		}

		Rect rect = new(0f, listing.CurHeight, listing.ColumnWidth, listing.listingRect.height - listing.CurHeight);
		Rect rect2 = new(0f, 0f, rect.width - 16, viewHeightSettingsEntries);
		Widgets.BeginScrollView(rect, ref scrollPositionSettingsEntries, rect2);
		float num = 0f;

		for (int index = 0; index < Settings.settingEntries.Count; index++)
		{
			SettingEntry settingEntry = Settings.settingEntries[index];
			num += settingEntry.DoInterface(0, num, rect2.width, index).height;
		}

		if (Event.current.type == EventType.Layout)
		{
			viewHeightSettingsEntries = num;
		}

		Widgets.EndScrollView();
	}
}

internal class Settings : ModSettings
{
	private readonly ConcurrentDictionary<ThingDef, Dictionary<ulong, HatEnum>> cachedHatStates = new();

	private HairSelectorUI? hairSelectorUI;

	private HashSet<string> hairDefNames = [];

	internal HairSelectorUI HairSelectorUI
	{
		get
		{
			if (hairSelectorUI == null)
			{
				hairSelectorUI = new HairSelectorUI
				{
					enabledDefs = hairDefNames.Select(defName => DefDatabase<HairDef>.defsByName[defName]).ToHashSet()
				};
			}

			return hairSelectorUI;
		}
	}

	internal void ClearCache()
	{
		cachedHatStates.Clear();
	}

	public bool TryGetPawnHatState(ulong flags, ThingDef hat, out HatEnum hatEnum)
	{
		if (!cachedHatStates.TryGetValue(hat, out Dictionary<ulong, HatEnum> hatState))
		{
			cachedHatStates.TryAdd(hat, hatState = new Dictionary<ulong, HatEnum>());
		}

		if (hatState.TryGetValue(flags, out hatEnum)) return true;
		SettingEntry? settingEntry = settingEntries.FirstOrDefault(settingEntry => settingEntry.Matches(flags, hat));
		if (settingEntry != null)
		{
			hatState.Add(flags, hatEnum = settingEntry.hatState);
		}

		return true;
	}

	internal bool onlyApplyToColonists;
	internal bool useDontShaveHead = true;
	internal List<SettingEntry> settingEntries = [];

	internal int IndexOf(SettingEntry settingEntry)
	{
		return settingEntries.IndexOf(settingEntry);
	}

	internal void AddEntry(SettingEntry settingEntry)
	{
		settingEntries.Add(settingEntry);
	}

	internal void Delete(SettingEntry settingEntry)
	{
		settingEntries.Remove(settingEntry);
	}

	public void Reorder(SettingEntry settingEntry, int offset)
	{
		int num = settingEntries.IndexOf(settingEntry);
		num += offset;
		if (num < 0) return;
		settingEntries.Remove(settingEntry);
		settingEntries.Insert(num, settingEntry);
	}

	public override void ExposeData()
	{
		base.ExposeData();
		if (Scribe.mode == LoadSaveMode.Saving)
		{
			hairDefNames = HairSelectorUI.enabledDefs.Select(hat => hat.defName).ToHashSet();
		}

		Scribe_Values.Look(ref onlyApplyToColonists, "OnlyApplyToColonists");
		Scribe_Values.Look(ref useDontShaveHead, "UseDontShaveHead", true);
		Scribe_Collections.Look(ref hairDefNames, "hairDefNames", LookMode.Value);
		Scribe_Collections.Look(ref settingEntries, "settingEntries", LookMode.Deep);
		if (Scribe.mode != LoadSaveMode.ResolvingCrossRefs) return;
		foreach (SettingEntry settingEntry in settingEntries)
		{
			settingEntry.settings = this;
		}
	}
}

internal class SettingEntryDialog : Window
{
	private static readonly ThingFilter parentFilter;

	static SettingEntryDialog()
	{
		parentFilter = new ThingFilter
		{
			allowedHitPointsConfigurable = false,
			allowedQualitiesConfigurable = false,
			hiddenSpecialFilters = DefDatabase<SpecialThingFilterDef>.AllDefsListForReading
		};
		parentFilter.SetDisallowAll();
		foreach (ThingDef hat in DefDatabase<ThingDef>.AllDefs.Where(def => Utils.IsHeadwear(def.apparel)))
		{
			parentFilter.SetAllow(hat, true);
		}
	}

	public override Vector2 InitialSize => new(900f, 700f);
	private readonly SettingEntry settingsEntry;
	private readonly CustomThingFilterUI.UIState thingFilterState = new();

	internal SettingEntryDialog(SettingEntry settingEntry)
	{
		settingsEntry = settingEntry;
		doCloseX = true;
		doCloseButton = true;
		absorbInputAroundWindow = true;
		closeOnClickedOutside = true;
	}

	public override void DoWindowContents(Rect inRect)
	{
		Rect inRect2 = new(0f, 40f, inRect.width, inRect.height - 40 - CloseButSize.y);
		Listing_Standard listing = new() { ColumnWidth = (inRect2.width - 34f) / 3f };
		listing.Begin(inRect2);
		Listing_Standard section = listing.BeginSection(400, 3, 3);
		Rect rect = section.GetRect(24f);
		if (Widgets.ButtonText(new Rect(rect.x, rect.y, rect.width / 2f - 1.5f, rect.height),
			    $"ShowHair.{settingsEntry.mode}".Translate()))
		{
			settingsEntry.mode = settingsEntry.mode == "any" ? "all" : "any";
		}

		if (Widgets.ButtonText(new Rect(rect.x + rect.width / 2f + 1.5f, rect.y, rect.width / 2f - 1.5f, rect.height),
			    $"ShowHair.{settingsEntry.notMode}".Translate()))
		{
			settingsEntry.notMode =
				settingsEntry.notMode == "any" ? "all" : "any";
		}

		Text.Anchor = TextAnchor.MiddleLeft;
		foreach (HatConditionFlagDef flag in DefDatabase<HatConditionFlagDef>.AllDefs.Where(def =>
			         def != HatConditionFlagDefOf.None))
		{
			Rect rect2 = section.GetRect(20f);
			rect2.xMin += 13f;
			rect2.xMax -= 13f;
			Widgets.DrawHighlightIfMouseover(rect2);
			rect2.yMax += 5f;
			rect2.yMin -= 5f;
			Widgets.Label(rect2, flag.label);
			MultiCheckboxState state;
			if ((settingsEntry.conditions & flag) > 0)
			{
				state = MultiCheckboxState.On;
			}
			else if ((settingsEntry.notConditions & flag) > 0)
			{
				state = MultiCheckboxState.Off;
			}
			else
			{
				state = MultiCheckboxState.Partial;
			}

			MultiCheckboxState newState =
				CustomWidgets.CheckboxMulti(new Rect(section.ColumnWidth - 42f, section.curY - 20, 20, 20), state);
			if (newState != state)
			{
				switch (newState)
				{
					case MultiCheckboxState.Off:
						settingsEntry.conditions &= ~flag;
						settingsEntry.notConditions |= flag;
						break;
					case MultiCheckboxState.On:
						settingsEntry.conditions |= flag;
						settingsEntry.notConditions &= ~flag;
						break;
					case MultiCheckboxState.Partial:
						settingsEntry.conditions &= ~flag;
						settingsEntry.notConditions &= ~flag;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		Text.Anchor = TextAnchor.UpperLeft;
		section.End();
		listing.NewColumn();
		Widgets.Dropdown(listing.GetRect(24f), settingsEntry, entry => entry.hatState,
			_ => Enum.GetValues(typeof(HatEnum)).Cast<HatEnum>().Select(hatEnum =>
				new Widgets.DropdownMenuElement<HatEnum>
				{
					payload = hatEnum,
					option = new FloatMenuOption(
						$"ShowHair.{Enum.GetName(typeof(HatEnum), hatEnum)}".Translate().ToString(),
						() => settingsEntry.hatState = hatEnum)
				}), $"ShowHair.{Enum.GetName(typeof(HatEnum), settingsEntry.hatState)}".Translate());
		listing.NewColumn();
		Rect rect5 = listing.GetRect(listing.listingRect.height - listing.CurHeight);
		CustomThingFilterUI.DoThingFilterConfigWindow(rect5, thingFilterState, settingsEntry.Hats, parentFilter);
		listing.End();
	}
}

internal class SettingEntry : IExposable
{
	private bool initialized;
	private HashSet<string> hatDefNames = [];
	internal Settings? settings;
	internal ulong conditions;
	internal string mode = "any";
	internal ulong notConditions;
	internal string notMode = "any";
	internal HatEnum hatState;

	internal bool Matches(ulong pawnConditions, ThingDef hat)
	{
		if (!Hats.allowedDefs.Contains(hat)) return false;
		if (conditions > HatConditionFlagDefOf.None)
		{
			switch (mode)
			{
				case "any" when (pawnConditions & conditions) == HatConditionFlagDefOf.None:
				case "all" when (pawnConditions & conditions) != conditions:
					return false;
			}
		}

		if (notConditions == HatConditionFlagDefOf.None) return true;
		switch (notMode)
		{
			case "any" when (pawnConditions & notConditions) > HatConditionFlagDefOf.None:
			case "all" when (pawnConditions & notConditions) == conditions:
				return false;
		}

		return true;
	}

	internal ThingFilter Hats
	{
		get
		{
			if (initialized) return field;
			field.allowedDefs = hatDefNames.Select(defName => DefDatabase<ThingDef>.defsByName[defName])
				.ToHashSet();
			initialized = true;
			return field;
		}
	} = new();

	internal SettingEntry(Settings settings)
	{
		this.settings = settings;
	}

	internal SettingEntry()
	{
	}

	internal Rect DoInterface(float x, float y, float width, int index)
	{
		if (settings is null)
		{
			Log.Error("Settings on SettingEntry is null.");
			throw new ArgumentNullException();
		}

		Rect rect = new(x, y, width, 53f);
		if (index % 2 == 0)
		{
			Widgets.DrawAltRect(rect);
		}

		Widgets.BeginGroup(rect);
		if (settings.IndexOf(this) > 0)
		{
			Rect rect2 = new(0f, 0f, 24f, 24f);
			if (Widgets.ButtonImage(rect2, TexButton.ReorderUp, Color.white))
			{
				settings.Reorder(this, -1);
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
			}
		}

		if (settings.IndexOf(this) < settings.settingEntries.Count - 1)
		{
			Rect rect3 = new(0f, 24f, 24f, 24f);
			if (Widgets.ButtonImage(rect3, TexButton.ReorderDown, Color.white))
			{
				settings.Reorder(this, 1);
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
			}
		}

		string conditionList = string.Join(", ",
			DefDatabase<HatConditionFlagDef>.AllDefs
				.Where(def => (def & conditions) > HatConditionFlagDefOf.None)
				.Select(def => def.label.Colorize(Color.green)));
		string notConditionList = string.Join(", ",
			DefDatabase<HatConditionFlagDef>.AllDefs
				.Where(def => (def & notConditions) > HatConditionFlagDefOf.None)
				.Select(def => def.label.Colorize(Color.red)));

		Text.Anchor = TextAnchor.UpperRight;
		string text =
			$"{$"ShowHair.{mode}".Translate().Colorize(Color.green)}, {$"ShowHair.{notMode}".Translate().Colorize(Color.red)}";
		float size = Text.CalcSize(text).x;
		Widgets.Label(new Rect(28f + rect.width - 68f - size, 0f, size, rect.height + 5f), text);
		Text.Anchor = TextAnchor.UpperLeft;
		Widgets.Label(new Rect(28f, 0f, rect.width - 68f - size, rect.height + 5f),
			conditionList.Length > 0 || notConditionList.Length > 0
				? $"{(conditionList.Length > 0 ? conditionList : "")}{(conditionList.Length > 0 && notConditionList.Length > 0 ? ", " : "")}{(notConditionList.Length > 0 ? notConditionList : "")}"
				: (string)"ShowHair.Always".Translate());

		Rect rect4 = new(rect.width - 24f, 0f, 24f, 24f);
		if (Widgets.ButtonImage(rect4, TexButton.Delete, Color.white, Color.white * GenUI.SubtleMouseoverColor))
		{
			settings.Delete(this);
			SoundDefOf.Click.PlayOneShotOnCamera();
		}

		WidgetRow widgetRow = new(x + width, 29f, UIDirection.LeftThenUp, width - 28f);
		if (widgetRow.ButtonText($"{"Details".Translate()}..."))
		{
			Find.WindowStack.Add(new SettingEntryDialog(this));
		}

		const float iconSize = 20f;
		
		Rect rect5 = new(widgetRow.LeftX(widgetRow.FinalX - 28f), 29f, widgetRow.FinalX - 28f, 26f);
		size = Text.CalcSize("...").x;
		size += (rect5.width - size) % iconSize;
		Rect rect6 = rect5.RightPartPixels(size);
		rect5.xMax -= size;
		int index2 = 0;
		int maxCount = (int)(rect5.width / iconSize);
		foreach (ThingDef def in Hats.AllowedThingDefs)
		{
			if (index2 == maxCount)
			{
				Text.Anchor = TextAnchor.LowerLeft;
				Widgets.Label(rect6, "...");
				Text.Anchor = TextAnchor.UpperLeft;
				break;
			}
			Widgets.DefIcon(new Rect(rect5.x + index2 * iconSize, rect5.yMax - iconSize, iconSize, iconSize), def);
			index2++;
		}
		Widgets.EndGroup();
		return rect;
	}

	public void ExposeData()
	{
		if (Scribe.mode == LoadSaveMode.Saving)
		{
			hatDefNames = Hats.allowedDefs.Select(hat => hat.defName).ToHashSet();
		}

		Scribe_Values.Look(ref conditions, "conditions");
		Scribe_Values.Look(ref mode, "mode", "any");
		Scribe_Values.Look(ref notConditions, "notConditions");
		Scribe_Values.Look(ref notMode, "notMode", "any");
		Scribe_Values.Look(ref hatState, "hatState");
		Scribe_Collections.Look(ref hatDefNames, "hatDefNames", LookMode.Value);
	}
}