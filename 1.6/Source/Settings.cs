using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ShowHair;

public enum HatEnum
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
	internal static Harmony? harmony;
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
		harmony = new Harmony("cat2002.showhair");
		harmony.PatchCategory("ModInitialization");
		if (!ParseHelper.HandlesType(typeof(Version)))
		{
			ParseHelper.Parsers<Version>.Register(Version.Parse);
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
		Utils.ConditionFlagDefsEnabled = settings?.GetEnabledConditions() ?? Utils.ConditionFlagDefsEnabled;
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
		section.Begin(listing.GetRect(Mathf.Floor(rect.height - listing.CurHeight)));
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
		Rect buttonRect = listing.GetRect(30f);
		if (Widgets.ButtonText(buttonRect, "ShowHair.AddSettingEntry".Translate().TrimMultiline()))
		{
			Settings.AddEntry(new SettingEntry(Settings, Settings.version));
		}

		listing.Gap(listing.verticalSpacing);
		if (Mouse.IsOver(buttonRect))
		{
			Rect r = new(new Vector2(UI.MousePositionOnUI.x + 10f, UI.MousePositionOnUIInverted.y),
				Text.CalcSize("ShowHair.AddSettingEntryTooltip".Translate().TrimMultiline()));
			r.xMax += 20;
			r.yMax += 20;
			Find.WindowStack.ImmediateWindow(619002489, r, WindowLayer.Super, delegate
			{
				Rect rect5 = r.AtZero();
				rect5.x += 10;
				rect5.y += 10;
				Widgets.Label(rect5, "ShowHair.AddSettingEntryTooltip".Translate().TrimMultiline());
			});
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
	internal static readonly Version latestVersion = new(1, 1, 0);
	internal Version version = latestVersion;
	private readonly ConcurrentDictionary<ThingDef, Dictionary<ulong, (HatEnum, bool)>> cachedHatStates = new();

	private HairSelectorUI? hairSelectorUI;

	private HashSet<string> hairDefNames = [];

	internal HairSelectorUI HairSelectorUI
	{
		get
		{
			return hairSelectorUI ??= new HairSelectorUI
			{
				enabledDefs = hairDefNames.Select(defName => DefDatabase<HairDef>.defsByName[defName]).ToHashSet()
			};
		}
	}

	internal void ClearCache()
	{
		cachedHatStates.Clear();
	}

	internal IEnumerable<HatConditionFlagDef> GetEnabledConditions()
	{
		return DefDatabase<HatConditionFlagDef>.AllDefs.Where(def =>
			def != HatConditionFlagDefOf.None && settingEntries.Any(entry =>
				(def & (entry.Conditions | entry.NotConditions)) > HatConditionFlagDefOf.None));
	}

	internal HatEnum GetHatState(ulong flags, ThingDef hat)
	{
		Dictionary<ulong, (HatEnum, bool)> hatState =
			cachedHatStates.GetOrAdd(hat, new Dictionary<ulong, (HatEnum, bool)>());

		if (hatState.TryGetValue(flags, out (HatEnum, bool) hatTuple)) return hatTuple.Item1;
		if (!hat.apparel.IsHeadwear())
		{
			hatState.Add(flags, (HatEnum.HidesAllHair, false));
			return HatEnum.HidesAllHair;
		}
		SettingEntry? settingEntry = settingEntries.FirstOrDefault(settingEntry => settingEntry.Matches(flags, hat));
		if (settingEntry != null)
		{
			hatState.Add(flags, (settingEntry.hatState, settingEntry.useDontShaveHead));
		}

		return HatEnum.HideHat;
	}

	internal bool GetHatDontShaveHead(ulong flags, ThingDef hat)
	{
		Dictionary<ulong, (HatEnum, bool)> hatState =
			cachedHatStates.GetOrAdd(hat, new Dictionary<ulong, (HatEnum, bool)>());

		if (hatState.TryGetValue(flags, out (HatEnum, bool) hatTuple)) return hatTuple.Item2;
		SettingEntry? settingEntry = settingEntries.FirstOrDefault(settingEntry => settingEntry.Matches(flags, hat));
		if (settingEntry != null)
		{
			hatState.Add(flags, (settingEntry.hatState, settingEntry.useDontShaveHead));
		}

		return hatTuple.Item2;
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

	internal void Reorder(SettingEntry settingEntry, int offset)
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

		Scribe_Values.Look(ref version, "version", latestVersion, true);
		if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			GetVersion();
		}

		Scribe_Values.Look(ref onlyApplyToColonists, "OnlyApplyToColonists");
		Scribe_Values.Look(ref useDontShaveHead, "UseDontShaveHead", true);
		Scribe_Collections.Look(ref hairDefNames, "hairDefNames", LookMode.Value);
		hairDefNames ??= [];
		Scribe_Collections.Look(ref settingEntries, "settingEntries", LookMode.Deep, this, version);
		settingEntries ??= [];
		if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
		{
			version = latestVersion;
		}

		return;

		void GetVersion()
		{
			if (version != latestVersion)
			{
				return;
			}

			XmlNodeList? childNodes = Scribe.loader.curXmlParent["settingEntries"]?.ChildNodes;
			if (childNodes == null) return;
			foreach (XmlNode node in childNodes)
			{
				if (node["conditions"] != null || node["notConditions"] != null)
				{
					version = new Version(1, 0, 0);
				}
			}
		}
	}
}

internal class SettingEntryDialog : Window
{
	private static readonly ThingFilter parentFilter;
	private float viewHeight;
	private Vector2 scrollPosition = new(0f, 0f);

	static SettingEntryDialog()
	{
		parentFilter = new ThingFilter(ThingCategoryDefOf.NGXYZ_HatRoot)
		{
			allowedHitPointsConfigurable = false,
			allowedQualitiesConfigurable = false,
			hiddenSpecialFilters = DefDatabase<SpecialThingFilterDef>.AllDefsListForReading
		};
		parentFilter.SetAllow(ThingCategoryDefOf.NGXYZ_HatRoot, true);
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
		Listing_Standard section = listing.BeginSection(listing.listingRect.height - listing.CurHeight - 6, 3, 3);
		Rect rect = section.GetRect(24f);
		if (Mouse.IsOver(rect))
		{
			Rect r = new(new Vector2(UI.MousePositionOnUI.x + 10f, UI.MousePositionOnUIInverted.y),
				Text.CalcSize("ShowHair.AnyAllTooltip".Translate().TrimMultiline()));
			r.xMax += 20;
			r.yMax += 20;
			Find.WindowStack.ImmediateWindow(739160518, r, WindowLayer.Super, delegate
			{
				Rect rect5 = r.AtZero();
				rect5.x += 10;
				rect5.y += 10;
				Widgets.Label(rect5, "ShowHair.AnyAllTooltip".Translate().TrimMultiline());
			});
		}

		Color color = GUI.color;
		GUI.color = Color.green;
		if (Widgets.ButtonText(new Rect(rect.x, rect.y, rect.width / 2f - 1.5f, rect.height),
			    $"ShowHair.{settingsEntry.mode}".Translate()))
		{
			settingsEntry.mode = settingsEntry.mode == "any" ? "all" : "any";
		}

		GUI.color = Color.red;

		if (Widgets.ButtonText(new Rect(rect.x + rect.width / 2f + 1.5f, rect.y, rect.width / 2f - 1.5f, rect.height),
			    $"ShowHair.{settingsEntry.notMode}".Translate()))
		{
			settingsEntry.notMode =
				settingsEntry.notMode == "any" ? "all" : "any";
		}

		GUI.color = color;

		Text.Anchor = TextAnchor.MiddleLeft;

		Rect rect2 = new(0f, section.CurHeight, section.ColumnWidth, section.listingRect.height - section.CurHeight);
		Rect rect3 = new(0f, 0f, rect2.width - 16, viewHeight);
		if (Mouse.IsOver(rect2))
		{
			Rect r = new(new Vector2(UI.MousePositionOnUI.x + 10f, UI.MousePositionOnUIInverted.y),
				Text.CalcSize("ShowHair.ConditionsTooltip".Translate().TrimMultiline()));
			r.xMax += 20;
			r.yMax += 20;
			Find.WindowStack.ImmediateWindow(1461335794, r, WindowLayer.Super, delegate
			{
				Rect rect5 = r.AtZero();
				rect5.x += 10;
				rect5.y += 10;
				Widgets.Label(rect5, "ShowHair.ConditionsTooltip".Translate().TrimMultiline());
			});
		}

		Widgets.BeginScrollView(rect2, ref scrollPosition, rect3);
		float num = 0f;
		foreach (HatConditionFlagDef flag in DefDatabase<HatConditionFlagDef>.AllDefs.Where(def =>
			         def != HatConditionFlagDefOf.None))
		{
			Rect rect4 = new(rect3.x, rect3.y + num, rect3.width, 20);
			rect4.xMin += 16f;
			Widgets.DrawHighlightIfMouseover(rect4);
			rect4.yMax += 5f;
			rect4.yMin -= 5f;
			Widgets.Label(rect4, flag.label);
			MultiCheckboxState state;
			if ((settingsEntry.Conditions & flag) > 0)
			{
				state = MultiCheckboxState.On;
			}
			else if ((settingsEntry.NotConditions & flag) > 0)
			{
				state = MultiCheckboxState.Off;
			}
			else
			{
				state = MultiCheckboxState.Partial;
			}

			MultiCheckboxState newState =
				CustomWidgets.CheckboxMulti(new Rect(rect4.x + rect4.width - 26f, rect4.y + 5, 20, 20), state);
			if (newState != state)
			{
				switch (newState)
				{
					case MultiCheckboxState.Off:
						settingsEntry.Conditions &= ~flag;
						settingsEntry.NotConditions |= flag;
						break;
					case MultiCheckboxState.On:
						settingsEntry.Conditions |= flag;
						settingsEntry.NotConditions &= ~flag;
						break;
					case MultiCheckboxState.Partial:
						settingsEntry.Conditions &= ~flag;
						settingsEntry.NotConditions &= ~flag;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			num += 20;
		}

		if (Event.current.type == EventType.Layout)
		{
			viewHeight = num;
		}

		Widgets.EndScrollView();

		Text.Anchor = TextAnchor.UpperLeft;
		section.End();
		listing.NewColumn();
		Rect rectDropdown = listing.GetRect(24f);
		if (Find.WindowStack.FloatMenu == null && Mouse.IsOver(rectDropdown))
		{
			Rect r = new(new Vector2(UI.MousePositionOnUI.x + 10f, UI.MousePositionOnUIInverted.y),
				Text.CalcSize("ShowHair.DropdownTooltip".Translate().TrimMultiline()));
			r.xMax += 20;
			r.yMax += 20;
			Find.WindowStack.ImmediateWindow(-2122180146, r, WindowLayer.Super, delegate
			{
				Rect rect5 = r.AtZero();
				rect5.x += 10;
				rect5.y += 10;
				Widgets.Label(rect5, "ShowHair.DropdownTooltip".Translate().TrimMultiline());
			});
		}

		Widgets.Dropdown(rectDropdown, settingsEntry, entry => entry.hatState,
			_ => Enum.GetValues(typeof(HatEnum)).Cast<HatEnum>().Select(hatEnum =>
				new Widgets.DropdownMenuElement<HatEnum>
				{
					payload = hatEnum,
					option = new FloatMenuOption(
						$"ShowHair.{Enum.GetName(typeof(HatEnum), hatEnum)}".Translate().ToString(),
						() => settingsEntry.hatState = hatEnum)
				}), $"ShowHair.{Enum.GetName(typeof(HatEnum), settingsEntry.hatState)}".Translate());
		listing.CheckboxLabeled($"ShowHair.UseDontShaveHead".Translate(), ref settingsEntry.useDontShaveHead);
		listing.NewColumn();
		Rect rect5 = listing.GetRect(listing.listingRect.height - listing.CurHeight);
		CustomThingFilterUI.DoThingFilterConfigWindow(rect5, thingFilterState, settingsEntry.Hats, parentFilter);
		listing.End();
	}
}

internal class SettingEntry : IExposable
{
	private HashSet<string> hatDefNames = [];
	private readonly Settings settings;
	private Version version;
	private ulong? conditions;
	private HashSet<string> conditionDefNames = [];
	internal string mode = "any";
	private HashSet<string> notConditionDefNames = [];
	private ulong? notConditions;
	internal string notMode = "any";
	internal HatEnum hatState;
	internal bool useDontShaveHead = true;
	private ThingFilter? hats;

	internal bool Matches(ulong pawnConditions, ThingDef hat)
	{
		if (!Hats.allowedDefs.Contains(hat)) return false;
		if (Conditions > HatConditionFlagDefOf.None)
		{
			switch (mode)
			{
				case "any" when (pawnConditions & Conditions) == HatConditionFlagDefOf.None:
				case "all" when (pawnConditions & Conditions) != Conditions:
					return false;
			}
		}

		if (NotConditions == HatConditionFlagDefOf.None) return true;
		switch (notMode)
		{
			case "any" when (pawnConditions & NotConditions) > HatConditionFlagDefOf.None:
			case "all" when (pawnConditions & NotConditions) == Conditions:
				return false;
		}

		return true;
	}

	internal ulong Conditions
	{
		get
		{
			if (conditions.HasValue)
				return conditions.Value;
			if (version != Settings.latestVersion)
				ConvertToLatestVersion();
			conditions = conditionDefNames.Select(DefDatabase<HatConditionFlagDef>.GetNamedSilentFail)
				.Where(def => def != null)
				.Aggregate<HatConditionFlagDef, ulong>(HatConditionFlagDefOf.None, (current, def) => current | def);
			return conditions.Value;
		}
		set => conditions = value;
	}

	internal ulong NotConditions
	{
		get
		{
			if (notConditions.HasValue)
				return notConditions.Value;
			if (version != Settings.latestVersion)
				ConvertToLatestVersion();

			notConditions = notConditionDefNames.Select(DefDatabase<HatConditionFlagDef>.GetNamedSilentFail)
				.Where(def => def != null)
				.Aggregate<HatConditionFlagDef, ulong>(HatConditionFlagDefOf.None, (current, def) => current | def);
			return notConditions.Value;
		}
		set => notConditions = value;
	}

	internal ThingFilter Hats =>
		hats ??= new ThingFilter(ThingCategoryDefOf.NGXYZ_HatRoot)
		{
			allowedDefs = hatDefNames.Select(DefDatabase<ThingDef>.GetNamedSilentFail).Where(def => def != null)
				.ToHashSet()
		};

	internal SettingEntry(Settings settings, Version version)
	{
		this.settings = settings;
		this.version = version;
	}

	internal Rect DoInterface(float x, float y, float width, int index)
	{
		if (settings is null)
		{
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
				.Where(def => (def & Conditions) > HatConditionFlagDefOf.None)
				.Select(def => def.label.Colorize(Color.green)));
		string notConditionList = string.Join(", ",
			DefDatabase<HatConditionFlagDef>.AllDefs
				.Where(def => (def & NotConditions) > HatConditionFlagDefOf.None)
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

	private ulong conditionsOld;
	private ulong notConditionsOld;

	private void ConvertToLatestVersion()
	{
		if (version == new Version(1, 0, 0))
		{
			conditionDefNames = DefDatabase<HatConditionFlagDef>.AllDefs.Where(def =>
			{
				ulong mask = def;
				if (!ModsConfig.royaltyActive && mask > HatConditionFlagDefOf.InHomeArea)
				{
					mask <<= 1;
				}

				return (mask & conditionsOld) > HatConditionFlagDefOf.None;
			}).Select(def => def.defName).ToHashSet();
			notConditionDefNames = DefDatabase<HatConditionFlagDef>.AllDefs.Where(def =>
			{
				ulong mask = def;
				if (!ModsConfig.royaltyActive && mask > HatConditionFlagDefOf.InHomeArea)
				{
					mask <<= 1;
				}

				return (mask & notConditionsOld) > HatConditionFlagDefOf.None;
			}).Select(def => def.defName).ToHashSet();
		}

		version = Settings.latestVersion;
	}

	public void ExposeData()
	{
		if (Scribe.mode == LoadSaveMode.Saving)
		{
			hatDefNames = Hats.allowedDefs.Select(hat => hat.defName).ToHashSet();
			conditionDefNames = DefDatabase<HatConditionFlagDef>.AllDefs
				.Where(def => (Conditions & def) > HatConditionFlagDefOf.None).Select(def => def.defName).ToHashSet();
			notConditionDefNames = DefDatabase<HatConditionFlagDef>.AllDefs
				.Where(def => (NotConditions & def) > HatConditionFlagDefOf.None).Select(def => def.defName)
				.ToHashSet();
		}

		Scribe_Collections.Look(ref conditionDefNames, "conditionDefNames", LookMode.Value);
		conditionDefNames ??= [];
		Scribe_Values.Look(ref mode, "mode", "any");
		Scribe_Collections.Look(ref notConditionDefNames, "notConditionDefNames", LookMode.Value);
		notConditionDefNames ??= [];
		Scribe_Values.Look(ref notMode, "notMode", "any");
		Scribe_Values.Look(ref hatState, "hatState");
		Scribe_Collections.Look(ref hatDefNames, "hatDefNames", LookMode.Value);
		hatDefNames ??= [];
		Scribe_Values.Look(ref useDontShaveHead, "useDontShaveHead", true);
		if (version == Settings.latestVersion) return;
		if (Scribe.mode != LoadSaveMode.LoadingVars) return;
		Scribe_Values.Look(ref conditionsOld, "conditions");
		Scribe_Values.Look(ref notConditionsOld, "notConditions");
	}
}