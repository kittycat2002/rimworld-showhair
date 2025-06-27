using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ShowHair;

internal static class CustomThingFilterUI
{
	internal static void DoThingFilterConfigWindow(Rect rect, UIState state, ThingFilter filter,
		ThingFilter parentFilter, int openMask = 1, IEnumerable<ThingDef>? forceHiddenDefs = null,
		IEnumerable<SpecialThingFilterDef>? forceHiddenFilters = null)
	{
		Widgets.DrawMenuSection(rect);
		float num = rect.width - 2f;
		Rect rect2 = new(rect.x + 3f, rect.y + 3f, num / 2f - 3f - 1.5f, 24f);
		if (Widgets.ButtonText(rect2, "ClearAll".Translate()))
		{
			filter.SetDisallowAll(forceHiddenDefs, forceHiddenFilters);
			SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
		}

		if (Widgets.ButtonText(new Rect(rect2.xMax + 3f, rect2.y, rect2.width, 24f), "AllowAll".Translate()))
		{
			filter.SetAllowAll(parentFilter);
			SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
		}

		rect.yMin = rect2.yMax;
		Rect rect3 = new(rect.x + 3f, rect.y + 3f, rect.width - 16f - 6f, 24f);
		state.quickSearch.OnGUI(rect3);
		rect.yMin = rect3.yMax + 3f;
		TreeNode_ThingCategory treeNodeThingCategory = filter.RootNode;

		rect.xMax -= 4f;
		rect.yMax -= 6f;
		Rect rect4 = new(0f, 0f, rect.width - 16f, viewHeight);
		Rect rect5 = new(0f, 0f, rect.width, rect.height);
		rect5.position += state.scrollPosition;
		Widgets.BeginScrollView(rect, ref state.scrollPosition, rect4);
		Rect rect6 = new(0f, 2f, rect4.width, 9999f);
		rect5.position -= rect6.position;
		Listing_HatTree listingTreeThingFilter = new(filter, parentFilter, state.quickSearch.filter);
		listingTreeThingFilter.Begin(rect6);
		listingTreeThingFilter.ListCategoryChildren(treeNodeThingCategory, openMask, rect5);
		listingTreeThingFilter.End();
		state.quickSearch.noResultsMatched = listingTreeThingFilter.matchCount == 0;
		if (Event.current.type == EventType.Layout)
		{
			viewHeight = listingTreeThingFilter.CurHeight + 92f;
		}

		Widgets.EndScrollView();
	}

	private static float viewHeight;

	public class UIState
	{
		public Vector2 scrollPosition;

		public readonly CustomQuickSearchWidget quickSearch = new();
	}
}

internal class CustomQuickSearchWidget
{
	public void OnGUI(Rect rect, Action? onFilterChange = null, Action? onClear = null)
	{
		if (CurrentlyFocused() && Event.current.type == EventType.KeyDown &&
		    Event.current.keyCode == KeyCode.Escape)
		{
			Unfocus();
			Event.current.Use();
		}

		if (OriginalEventUtility.EventType == EventType.MouseDown && !rect.Contains(Event.current.mousePosition))
		{
			Unfocus();
		}

		Color color = GUI.color;
		GUI.color = Color.white;
		float num = Mathf.Min(18f, rect.height);
		float num2 = num + 8f;
		float num3 = rect.y + (rect.height - num2) / 2f + 4f;
		Rect rect2 = new(rect.x + 4f, num3, num, num);
		GUI.DrawTexture(rect2, TexButton.Search);
		GUI.SetNextControlName(controlName);
		Rect rect3 = new(rect2.xMax + 4f, rect.y, rect.width - num2, rect.height);
		if (filter.Active)
		{
			rect3.xMax -= num2;
		}

		using (new TextBlock(GameFont.Small))
		{
			if (noResultsMatched && filter.Active)
			{
				GUI.color = ColorLibrary.RedReadable;
			}
			else if (!filter.Active && !CurrentlyFocused())
			{
				GUI.color = inactiveTextColor;
			}

			string text = Widgets.TextField(rect3, filter.Text, MaxSearchTextLength);
			GUI.color = Color.white;
			if (text != filter.Text)
			{
				filter.Text = text;
				onFilterChange?.Invoke();
			}
		}

		if (filter.Active && Widgets.ButtonImage(new Rect(rect3.xMax + 4f, num3, num, num), TexButton.CloseXSmall))
		{
			filter.Text = "";
			SoundDefOf.CancelMode.PlayOneShotOnCamera();
			onFilterChange?.Invoke();
			onClear?.Invoke();
		}

		GUI.color = color;
	}

	private void Unfocus()
	{
		if (CurrentlyFocused())
		{
			UI.UnfocusCurrentControl();
		}
	}

	private bool CurrentlyFocused()
	{
		return GUI.GetNameOfFocusedControl() == this.controlName;
	}

	internal readonly CustomQuickSearchFilter filter = new();

	internal bool noResultsMatched;

	private readonly Color inactiveTextColor = Color.white;

	private const int MaxSearchTextLength = 30;

	private readonly string controlName = $"QuickSearchWidget_{QuickSearchWidget.instanceCounter++}";
}

internal class CustomQuickSearchFilter
{
	public string Text
	{
		get => inputText;
		set
		{
			inputText = value;
			searchText = value.Trim();
			cachedMatches.Clear();
		}
	}

	public bool Active => !inputText.NullOrEmpty();

	public bool Matches(string value)
	{
		if (!Active)
		{
			return true;
		}

		if (value.NullOrEmpty())
		{
			return false;
		}

		if (!cachedMatches.TryGetValue(value, out bool flag))
		{
			flag = MatchImpl(value);
			cachedMatches.Add(value, flag);
		}

		return flag;
	}

	private bool MatchImpl(string value)
	{
		return value.IndexOf(searchText, StringComparison.InvariantCultureIgnoreCase) != -1;
	}

	public bool Matches(ThingDef td)
	{
		return Matches(td.label);
	}

	private string inputText = "";

	private string searchText = "";

	private readonly LRUCache<string, bool> cachedMatches = new(5000);
}

// ReSharper disable once InconsistentNaming
internal class Listing_HatTree(ThingFilter filter, ThingFilter parentFilter, CustomQuickSearchFilter searchFilter)
	: Listing_Tree
{
	public void ListCategoryChildren(TreeNode_ThingCategory node, int openMask, Rect visibleRect2)
	{
		visibleRect = visibleRect2;

		DoCategoryChildren(node, 0, openMask, false);
	}

	private void DoCategoryChildren(TreeNode_ThingCategory node, int indentLevel, int openMask,
		bool subtreeMatchedSearch)
	{
		foreach (TreeNode_ThingCategory treeNodeThingCategory in node.ChildCategoryNodes)
		{
			if (Visible(treeNodeThingCategory) && (!searchFilter.Active || subtreeMatchedSearch ||
			                                       CategoryMatches(treeNodeThingCategory) ||
			                                       ThisOrDescendantsVisibleAndMatchesSearch(treeNodeThingCategory)))
			{
				DoCategory(treeNodeThingCategory, indentLevel, openMask, subtreeMatchedSearch);
			}
		}

		foreach (ThingDef thingDef in node.catDef.SortedChildThingDefs.Where(thingDef => Visible(thingDef) &&
			         (!searchFilter.Active || subtreeMatchedSearch || searchFilter.Matches(thingDef))))
		{
			DoThingDef(thingDef, indentLevel);
		}
	}

	private void DoCategory(TreeNode_ThingCategory node, int indentLevel, int openMask, bool subtreeMatchedSearch)
	{
		Color? color = null;
		if (searchFilter.Active)
		{
			if (CategoryMatches(node))
			{
				subtreeMatchedSearch = true;
				matchCount++;
			}
			else
			{
				color = Listing_TreeThingFilter.NoMatchColor;
			}
		}

		if (CurrentRowVisibleOnScreen())
		{
			OpenCloseWidget(node, indentLevel, openMask);
			LabelLeft(node.LabelCap, node.catDef.description, indentLevel, 0f, color);
			MultiCheckboxState multiCheckboxState = AllowanceStateOf(node);
			MultiCheckboxState multiCheckboxState2 = Widgets.CheckboxMulti(
				new Rect(LabelWidth, curY, lineHeight, lineHeight), multiCheckboxState, true);
			if (multiCheckboxState != multiCheckboxState2)
			{
				filter.SetAllow(node.catDef, multiCheckboxState2 == MultiCheckboxState.On);
			}
		}

		EndLine();
		if (IsOpen(node, openMask))
		{
			DoCategoryChildren(node, indentLevel + 1, openMask, subtreeMatchedSearch);
		}
	}

	private void DoThingDef(ThingDef tDef, int nestLevel)
	{
		Color? color = null;
		if (searchFilter.Matches(tDef))
		{
			matchCount++;
		}
		else
		{
			color = Listing_TreeThingFilter.NoMatchColor;
		}

		if (tDef.uiIcon != null && tDef.uiIcon != BaseContent.BadTex)
		{
			nestLevel++;
			Widgets.DefIcon(new Rect(XAtIndentLevel(nestLevel) - 6f, curY, 20f, 20f), tDef, null, 1f, null, true,
				color);
		}

		if (CurrentRowVisibleOnScreen())
		{
			string text = tDef.DescriptionDetailed;
			LabelLeft(tDef.LabelCap, text, nestLevel, -23f, color);
			bool flag = filter.Allows(tDef);
			bool flag2 = flag;
			Widgets.Checkbox(new Vector2(LabelWidth, curY), ref flag, lineHeight, false, true);
			if (flag != flag2)
			{
				filter.SetAllow(tDef, flag);
			}
		}

		EndLine();
	}

	private MultiCheckboxState AllowanceStateOf(TreeNode_ThingCategory cat)
	{
		int num = 0;
		int num2 = 0;
		foreach (ThingDef thingDef in cat.catDef.DescendantThingDefs)
		{
			if (!Visible(thingDef)) continue;
			num++;
			if (filter.Allows(thingDef))
			{
				num2++;
			}
		}

		if (num2 == 0)
		{
			return MultiCheckboxState.Off;
		}

		return num == num2 ? MultiCheckboxState.On : MultiCheckboxState.Partial;
	}

	private bool Visible(ThingDef td)
	{
		return td is { PlayerAcquirable: true, virtualDefParent: null } && parentFilter.Allows(td) &&
		       !parentFilter.IsAlwaysDisallowedDueToSpecialFilters(td);
	}

	public override bool IsOpen(TreeNode node, int openMask)
	{
		if (base.IsOpen(node, openMask))
		{
			return true;
		}

		if (node is not TreeNode_ThingCategory treeNodeThingCategory)
		{
			return false;
		}

		return searchFilter.Active && ThisOrDescendantsVisibleAndMatchesSearch(treeNodeThingCategory);
	}

	private bool ThisOrDescendantsVisibleAndMatchesSearch(TreeNode_ThingCategory node)
	{
		if (Visible(node) && CategoryMatches(node))
		{
			return true;
		}

		return node.catDef.childThingDefs.Any(thingDef => Visible(thingDef) && searchFilter.Matches(thingDef)) ||
		       node.catDef.childCategories.Any(thingCategoryDef =>
			       ThisOrDescendantsVisibleAndMatchesSearch(thingCategoryDef.treeNode));
	}

	private bool CategoryMatches(TreeNode_ThingCategory node)
	{
		return searchFilter.Matches(node.catDef.label);
	}

	private bool Visible(TreeNode_ThingCategory node)
	{
		return node.catDef.DescendantThingDefs.Any(Visible);
	}

	private bool CurrentRowVisibleOnScreen()
	{
		Rect rect = new(0f, curY, ColumnWidth, lineHeight);
		return visibleRect.Overlaps(rect);
	}

	public int matchCount;

	private Rect visibleRect;
}

internal static class CustomWidgets
{
	public static MultiCheckboxState CheckboxMulti(Rect rect, MultiCheckboxState state)
	{
		Texture2D texture2D = state switch
		{
			MultiCheckboxState.On => Widgets.CheckboxOnTex,
			MultiCheckboxState.Off => Widgets.CheckboxOffTex,
			_ => Widgets.CheckboxPartialTex
		};
		MouseoverSounds.DoRegion(rect);
		MultiCheckboxState multiCheckboxState = state switch
		{
			MultiCheckboxState.On => MultiCheckboxState.Off,
			MultiCheckboxState.Off => MultiCheckboxState.Partial,
			_ => MultiCheckboxState.On
		};
		bool flag = false;
		Widgets.DraggableResult draggableResult = Widgets.ButtonImageDraggable(rect, texture2D);
		if (draggableResult.AnyPressed())
		{
			flag = true;
		}

		if (!flag) return state;
		if (multiCheckboxState == MultiCheckboxState.On)
		{
			SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
		}
		else
		{
			SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
		}

		return multiCheckboxState;
	}
}