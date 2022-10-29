using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ShowHair
{
    public class SettingsController : Mod
    {
        private Settings Settings;

        private Vector2 scrollPosition = new Vector2(0, 0);
        private Vector2 scrollPosition2 = new Vector2(0, 0);
        private float previousHatY, previousHairY;
        private string leftTableSearchBuffer = "", rightTableSearchBuffer = "";

        public SettingsController(ModContentPack content) : base(content)
        {
            Settings = base.GetSettings<Settings>();

            HairUtilityFactory.GetHairUtility();
        }

        public override string SettingsCategory()
        {
            return "ShowHair.ShowHair".Translate();
        }

        public override void DoSettingsWindowContents(Rect rect)
        {
            Settings.Initialize();

            Widgets.CheckboxLabeled(new Rect(0, 60, 250, 22), "ShowHair.OnlyApplyToColonists".Translate(), ref Settings.OnlyApplyToColonists);
            Widgets.CheckboxLabeled(new Rect(0, 90, 250, 22), "ShowHair.HideAllHats".Translate(), ref Settings.HideAllHats);
            Widgets.CheckboxLabeled(new Rect(0, 120, 250, 22), "ShowHair.UseDontShaveHead".Translate(), ref Settings.UseDontShaveHead);

            if (!Settings.HideAllHats)
            {
                Widgets.CheckboxLabeled(new Rect(0, 150, 250, 22), "ShowHair.ShowHatsOnlyWhenDrafted".Translate(), ref Settings.ShowHatsOnlyWhenDrafted);

                if (!Settings.ShowHatsOnlyWhenDrafted)
                {
                    Widgets.Label(new Rect(0, 180, 225, 22), "ShowHair.HideHatsIndoors".Translate());
                    string label;
                    switch (Settings.Indoors)
                    {
                        case Indoors.ShowHats:
                            label = "Off";
                            break;
                        case Indoors.HideHats:
                            label = "ShowHair.HideHatsIndoors";
                            break;
                        default:
                            label = "ShowHair.HideHatsIndoorsShowWhenDrafted";
                            break;
                    }
                    if (Widgets.ButtonText(new Rect(235, 180, 200, 22), label.Translate()))
                    {
                        Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>()
                        {
                            new FloatMenuOption("Off".Translate(), delegate() {Settings.Indoors = Indoors.ShowHats; }),
                            new FloatMenuOption("ShowHair.HideHatsIndoors".Translate(), delegate() {Settings.Indoors = Indoors.HideHats; }),
                            new FloatMenuOption("ShowHair.HideHatsIndoorsShowWhenDrafted".Translate(), delegate() {Settings.Indoors = Indoors.ShowHatsWhenDrafted; }),
                        }));
                    }
                }
                DrawTableHats(0, 220, (float)Math.Floor(rect.width / 2) - 10, ref scrollPosition, ref previousHatY, "ShowHair.Hats", "ShowHair.HatsDesc", ref leftTableSearchBuffer, Settings.HatsThatHide.Keys, Settings.HatsThatHide, Settings.HatsRenderer);
                DrawTableHair((float)Math.Floor(rect.width / 2) + 10, 220, rect.width - (float)Math.Floor(rect.width / 2) - 10, ref scrollPosition2, ref previousHairY, "ShowHair.HairThatWillBeHidden", "", ref rightTableSearchBuffer, Settings.HairToHide.Keys, Settings.HairToHide);
            }
        }

        private void DrawTableHats<T>(float x, float y, float width, ref Vector2 scroll, ref float innerY, string header, string headerDesc, ref string searchBuffer, ICollection<T> labels, Dictionary<T, HatHideEnum> hideDict, Dictionary<T, HatRendererEnum> rendererDict) where T : ThingDef
        {
            Text.Font = GameFont.Small;
            const float ROW_HEIGHT = 32;
            GUI.BeginGroup(new Rect(x, y, width, 400));
            Widgets.Label(new Rect(0, 0, width - 200, 20), header.Translate());
            if (Widgets.ButtonText(new Rect(width - 200, 0, 100, 24), ((searchBuffer != "") ? "ShowHair.SetFiltered" : "ShowHair.SetAll").Translate()))
            {
                string sb = searchBuffer;
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>() {
                    new FloatMenuOption(HatHideEnum.ShowsHair.ToString().Translate(), () =>
                    { this.SetHatHideEnum(sb, hideDict, HatHideEnum.ShowsHair); }),
                    new FloatMenuOption(HatHideEnum.HideHat.ToString().Translate(), () =>
                    { this.SetHatHideEnum(sb, hideDict, HatHideEnum.HideHat); }),
                    new FloatMenuOption(HatHideEnum.HidesAllHair.ToString().Translate(), () =>
                    { this.SetHatHideEnum(sb, hideDict, HatHideEnum.HidesAllHair); }),
                    new FloatMenuOption(HatHideEnum.HidesHairShowBeard.ToString().Translate(), () =>
                    { this.SetHatHideEnum(sb, hideDict, HatHideEnum.HidesHairShowBeard); }),
                    new FloatMenuOption(HatHideEnum.OnlyDraftSH.ToString().Translate(), () =>
                    { this.SetHatHideEnum(sb, hideDict, HatHideEnum.OnlyDraftSH); }),
                    new FloatMenuOption(HatHideEnum.OnlyDraftHH.ToString().Translate(), () =>
                    { this.SetHatHideEnum(sb, hideDict, HatHideEnum.OnlyDraftHH); }),
                    new FloatMenuOption(HatHideEnum.OnlyDraftHHSB.ToString().Translate(), () =>
                    { this.SetHatHideEnum(sb, hideDict, HatHideEnum.OnlyDraftHHSB); })
                }));
            }
            if (Widgets.ButtonText(new Rect(width - 100, 0, 100, 24), ((searchBuffer != "") ? "ShowHair.SetFiltered" : "ShowHair.SetAll").Translate()))
            {
                string sb = searchBuffer;
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>() {
                    new FloatMenuOption(HatRendererEnum.NormalRender.ToString().Translate(), () =>
                    { this.SetHatRendererEnum(sb, rendererDict, HatRendererEnum.ForceOverHair); }),
                    new FloatMenuOption(HatRendererEnum.ForceOverHair.ToString().Translate(), () =>
                    { this.SetHatRendererEnum(sb, rendererDict, HatRendererEnum.ForceOverHair); })
                }));
            }
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(0, 20, width - 65, 20), headerDesc.Translate());
            Text.Font = GameFont.Small;
            searchBuffer = Widgets.TextArea(new Rect(0, 40, 200, 28), searchBuffer);
            if (Widgets.ButtonText(new Rect(200, 40, 28, 28), "X"))
                searchBuffer = "";
            Widgets.BeginScrollView(new Rect(0, 75, width, 300), ref scroll, new Rect(0, 0, width - 16, innerY));

            innerY = 0;
            int index = 0;
            foreach (T t in labels)
            {
                if (!MatchesSearch(searchBuffer, t))
                    continue;

                innerY = index++ * ROW_HEIGHT;

                Widgets.ThingIcon(new Rect(x, innerY - 2, ROW_HEIGHT - 2, ROW_HEIGHT - 2), t);
                Widgets.Label(new Rect(34, innerY, 184, ROW_HEIGHT), t.label + ":");

                Text.Font = GameFont.Tiny;
                if (Widgets.ButtonText(new Rect(width - 196, innerY, 90, 26), hideDict[t].ToString().Translate()))
                {
                    Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>() {
                        new FloatMenuOption(HatHideEnum.ShowsHair.ToString().Translate(), () =>
                        { hideDict[t] = HatHideEnum.ShowsHair; }),
                        new FloatMenuOption(HatHideEnum.HideHat.ToString().Translate(), () =>
                        { hideDict[t] = HatHideEnum.HideHat; }),
                        new FloatMenuOption(HatHideEnum.HidesAllHair.ToString().Translate(), () =>
                        { hideDict[t] = HatHideEnum.HidesAllHair; }),
                        new FloatMenuOption(HatHideEnum.HidesHairShowBeard.ToString().Translate(), () =>
                        { hideDict[t] = HatHideEnum.HidesHairShowBeard; }),
                        new FloatMenuOption(HatHideEnum.OnlyDraftSH.ToString().Translate(), () =>
                        { hideDict[t] = HatHideEnum.OnlyDraftSH; }),
                        new FloatMenuOption(HatHideEnum.OnlyDraftHH.ToString().Translate(), () =>
                        { hideDict[t] = HatHideEnum.OnlyDraftHH; }),
                        new FloatMenuOption(HatHideEnum.OnlyDraftHHSB.ToString().Translate(), () =>
                        { hideDict[t] = HatHideEnum.OnlyDraftHHSB; })
                    }));
                }
                if (Widgets.ButtonText(new Rect(width - 106, innerY, 90, 26), rendererDict[t].ToString().Translate()))
                {
                    Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>() {
                        new FloatMenuOption(HatRendererEnum.NormalRender.ToString().Translate(), () =>
                        { rendererDict[t] = HatRendererEnum.NormalRender; }),
                        new FloatMenuOption(HatRendererEnum.ForceOverHair.ToString().Translate(), () =>
                        { rendererDict[t] = HatRendererEnum.ForceOverHair; })
                    }));
                }
                Text.Font = GameFont.Small;


            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            innerY += ROW_HEIGHT;
        }

        private void DrawTableHair<T>(float x, float y, float width, ref Vector2 scroll, ref float innerY, string header, string headerDesc, ref string searchBuffer, ICollection<T> labels, Dictionary<T, bool> hairDict) where T : HairDef
        {
            Text.Font = GameFont.Small;
            const float ROW_HEIGHT = 32;
            GUI.BeginGroup(new Rect(x, y, width, 400));
            Widgets.Label(new Rect(0, 0, width - 100, 20), header.Translate());
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(0, 20, width - 65, 20), headerDesc.Translate());
            Text.Font = GameFont.Small;
            Rect rect = new Rect(0, 0, width - 16, innerY);
            searchBuffer = Widgets.TextArea(new Rect(0, 40, 200, 28), searchBuffer);
            if (Widgets.ButtonText(new Rect(200, 40, 28, 28), "X"))
                searchBuffer = "";
            Widgets.BeginScrollView(new Rect(0, 75, width, 300), ref scroll, rect);

            innerY = 0;
            int index = 0;
            foreach (T t in labels)
            {
                if (!MatchesSearch(searchBuffer, t))
                    continue;

                innerY = index++ * ROW_HEIGHT;

                rect = new Rect(0, innerY, 184, ROW_HEIGHT);
                Widgets.Label(rect, t.label + ":");

                if (hairDict != null)
                {
                    bool b, orig;
                    b = orig = hairDict[t];
                    Widgets.Checkbox(new Vector2(width - 40, innerY - 1), ref b);
                    if (b != orig)
                    {
                        hairDict[t] = b;
                        break;
                    }
                }
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            innerY += ROW_HEIGHT;
        }

        private bool MatchesSearch<T>(string searchBuffer, T t) where T : Def
        {
            return searchBuffer == "" || t.label.ToLower().Contains(searchBuffer);
        }

        private void SetHatHideEnum<T>(string searchBuffer, Dictionary<T, HatHideEnum> items, HatHideEnum v) where T : ThingDef
        {
            foreach (T t in new List<T>(items.Keys))
            {
                if (this.MatchesSearch(searchBuffer, t))
                {
                    items[t] = v;
                }
            }
        }
        private void SetHatRendererEnum<T>(string searchBuffer, Dictionary<T, HatRendererEnum> items, HatRendererEnum v) where T : ThingDef
        {
            foreach (T t in new List<T>(items.Keys))
            {
                if (MatchesSearch(searchBuffer, t))
                {
                    items[t] = v;
                }
            }
        }
    }

    public enum HatHideEnum
    {
        ShowsHair,
        HidesAllHair,
        HidesHairShowBeard,
        HideHat,
        OnlyDraftSH,
        OnlyDraftHH,
        OnlyDraftHHSB
    }

    public enum HatRendererEnum
    {
        NormalRender,
        ForceOverHair
    }

    public enum Indoors
    {
        ShowHats,
        HideHats,
        ShowHatsWhenDrafted
    }

    class Settings : ModSettings
    {
        public static bool OnlyApplyToColonists = false;
        public static bool HideAllHats = false;
        public static bool ShowHatsOnlyWhenDrafted = false;
        public static bool ShowHatsWhenDraftedIndoors = false;
        public static bool OptionsOpen = false;
        public static Indoors Indoors = Indoors.ShowHats;

        public static bool UseDontShaveHead = true;

        public static Dictionary<ThingDef, HatHideEnum> HatsThatHide = new Dictionary<ThingDef, HatHideEnum>();
        public static Dictionary<ThingDef, HatRendererEnum> HatsRenderer = new Dictionary<ThingDef, HatRendererEnum>();
        public static Dictionary<HairDef, bool> HairToHide = new Dictionary<HairDef, bool>();

        private static ToSave ToSave = null;

        public override void ExposeData()
        {
            base.ExposeData();

            if (ToSave == null)
                ToSave = new ToSave();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                ToSave.Clear();

                if (Current.Game != null)
                {
                    foreach (var p in PawnsFinder.AllMaps)
                    {
                        if (p.IsColonist && !p.Dead && p.def.race.Humanlike)
                        {
                            PortraitsCache.SetDirty(p);
                            GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(p);
                        }
                    }
                }

                foreach (KeyValuePair<ThingDef, HatHideEnum> kv in HatsThatHide)
                {
                    switch (kv.Value)
                    {
                        case HatHideEnum.HidesAllHair:
                            ToSave.hatsThatHideHair.Add(kv.Key.defName);
                            break;
                        case HatHideEnum.HidesHairShowBeard:
                            ToSave.hatsToHideShowBeards.Add(kv.Key.defName);
                            break;
                        case HatHideEnum.HideHat:
                            ToSave.hatsToHide.Add(kv.Key.defName);
                            break;
                        case HatHideEnum.OnlyDraftSH:
                            ToSave.hatsToHideUnlessDraftedSH.Add(kv.Key.defName);
                            break;
                        case HatHideEnum.OnlyDraftHH:
                            ToSave.hatsToHideUnlessDraftedHH.Add(kv.Key.defName);
                            break;
                        case HatHideEnum.OnlyDraftHHSB:
                            ToSave.hatsToHideUnlessDraftedHHSB.Add(kv.Key.defName);
                            break;
                    }
                }
                foreach (KeyValuePair<ThingDef, HatRendererEnum> kv in HatsRenderer)
                {
                    if (kv.Value == HatRendererEnum.ForceOverHair)
                    {
                        ToSave.HatsForcedOverHair.Add(kv.Key.defName);
                    }
                }

                ToSave.hairToHide = new List<string>();
                foreach (KeyValuePair<HairDef, bool> kv in HairToHide)
                    if (kv.Value)
                        ToSave.hairToHide.Add(kv.Key.defName);
            }

            Scribe_Collections.Look(ref ToSave.hatsThatHideHair, "HatsThatHide", LookMode.Value);
            Scribe_Collections.Look(ref ToSave.hatsToHideShowBeards, "HatsToHideShowBeards", LookMode.Value);
            Scribe_Collections.Look(ref ToSave.hatsToHideUnlessDraftedSH, "HatsToHideUnlessDraftedSH", LookMode.Value);
            Scribe_Collections.Look(ref ToSave.hatsToHideUnlessDraftedHH, "HatsToHideUnlessDraftedHH", LookMode.Value);
            Scribe_Collections.Look(ref ToSave.hatsToHideUnlessDraftedHHSB, "hatsToHideUnlessDraftedHHSB", LookMode.Value);
            Scribe_Collections.Look(ref ToSave.hatsToHide, "HatsToHide", LookMode.Value);
            Scribe_Collections.Look(ref ToSave.HatsForcedOverHair, "HatsForcedOverHair", LookMode.Value);
            Scribe_Collections.Look(ref ToSave.hairToHide, "HairToHide", LookMode.Value);
            Scribe_Values.Look<bool>(ref HideAllHats, "HideAllHats", false, false);
            Scribe_Values.Look<bool>(ref OnlyApplyToColonists, "OnlyApplyToColonists", false, false);
            Scribe_Values.Look<bool>(ref ShowHatsOnlyWhenDrafted, "ShowHatsOnlyWhenDrafted", false, false);
            Scribe_Values.Look<bool>(ref ShowHatsWhenDraftedIndoors, "ShowHatsWhenDraftedIndoors", false, false);
            Scribe_Values.Look<Indoors>(ref Indoors, "Indoors", Indoors.ShowHats, false);
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                bool b = false;
                Scribe_Values.Look<bool>(ref b, "HideHatsIndoors", false, false);
                if (b)
                    Indoors = Indoors.HideHats;
            }
            Scribe_Values.Look<bool>(ref UseDontShaveHead, "UseDontShaveHead", true, false);

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                ToSave?.Clear();
                ToSave = null;
            }

            OptionsOpen = false;
        }

        private static bool isInitialized = false;
        internal static void Initialize()
        {
            int defCount = 0;
            if (!isInitialized)
            {
                if (ToSave == null)
                    ToSave = new ToSave();

                foreach (ThingDef d in DefDatabase<ThingDef>.AllDefs)
                {
                    ++defCount;

                    if (d.apparel == null ||
                    !IsHeadwear(d.apparel) ||
                    (String.IsNullOrEmpty(d.apparel.wornGraphicPath) &&
                    d.apparel.wornGraphicPaths?.Count == 0))
                    {
                        continue;
                    }

                    HatHideEnum e = HatHideEnum.ShowsHair;
                    if (ToSave.hatsThatHideHair?.Contains(d.defName) == true)
                        e = HatHideEnum.HidesAllHair;
                    else if (ToSave.hatsToHideShowBeards?.Contains(d.defName) == true)
                        e = HatHideEnum.HidesHairShowBeard;
                    else if (ToSave.hatsToHide?.Contains(d.defName) == true)
                        e = HatHideEnum.HideHat;
                    else if (ToSave.hatsToHideUnlessDraftedSH?.Contains(d.defName) == true)
                        e = HatHideEnum.OnlyDraftSH;
                    else if (ToSave.hatsToHideUnlessDraftedHH?.Contains(d.defName) == true)
                        e = HatHideEnum.OnlyDraftHH;
                    else if (ToSave.hatsToHideUnlessDraftedHHSB?.Contains(d.defName) == true)
                        e = HatHideEnum.OnlyDraftHHSB;
                    HatsThatHide[d] = e;

                    HatRendererEnum f = HatRendererEnum.NormalRender;
                    if (ToSave.HatsForcedOverHair?.Contains(d.defName) == true)
                        f = HatRendererEnum.ForceOverHair;
                    HatsRenderer[d] = f;
                }

                foreach (HairDef d in DefDatabase<HairDef>.AllDefs)
                {
                    ++defCount;
                    bool selected = false;
                    if (ToSave.hairToHide != null)
                    {
                        foreach (string s in ToSave.hairToHide)
                        {
                            if (s.Equals(d.defName))
                            {
                                selected = true;
                                break;
                            }
                        }
                    }
                    HairToHide[d] = selected;
                }

                if (defCount > 0)
                    isInitialized = true;

                if (isInitialized)
                {
                    ToSave?.Clear();
                    ToSave = null;
                }
            }
        }

        public static bool IsHeadwear(ApparelProperties apparelProperties)
        {
            if (apparelProperties == null)
                return false;
            if (apparelProperties.LastLayer == ApparelLayerDefOf.Overhead || apparelProperties.LastLayer == ApparelLayerDefOf.EyeCover)
                return true;
            foreach (var g in apparelProperties.bodyPartGroups)
            {
                if (g == BodyPartGroupDefOf.FullHead || g == BodyPartGroupDefOf.UpperHead || g == BodyPartGroupDefOf.Eyes)
                {
                    return true;
                }
            }
            return false;
        }
    }

    class ToSave
    {
        public List<string> hatsThatHideHair = null;
        public List<string> hatsToHide = null;
        public List<string> hatsToHideShowBeards = null;
        public List<string> hatsToHideUnlessDraftedSH = null;
        public List<string> hatsToHideUnlessDraftedHH = null;
        public List<string> hatsToHideUnlessDraftedHHSB = null;
        public List<string> HatsForcedOverHair = null;
        public List<string> hairToHide = null;

        public ToSave()
        {
            hatsThatHideHair = new List<string>();
            hatsToHide = new List<string>();
            hatsToHideShowBeards = new List<string>();
            hatsToHideUnlessDraftedSH = new List<string>();
            hatsToHideUnlessDraftedHH = new List<string>();
            hatsToHideUnlessDraftedHHSB = new List<string>();
            HatsForcedOverHair = new List<string>();
            hairToHide = new List<string>();
        }

        public void Clear()
        {
            hatsThatHideHair?.Clear();
            hatsToHide?.Clear();
            hatsToHideShowBeards?.Clear();
            hatsToHideUnlessDraftedSH?.Clear();
            hatsToHideUnlessDraftedHH?.Clear();
            hatsToHideUnlessDraftedHHSB?.Clear();
            HatsForcedOverHair?.Clear();
            hairToHide?.Clear();
        }
    }
}