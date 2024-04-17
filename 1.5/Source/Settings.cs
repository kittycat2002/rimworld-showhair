using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ShowHair
{
    public class HatSaver : IExposable
    {
        public string defName;
        public string label;
        public HatEnum hatHide;
        public HatStateEnum draftedHide;
        public HatStateEnum indoorsHide;
        public HatStateEnum bedHide;
        public HatSaver()
        {
        }
        public HatSaver(ThingDef hatDef)
        {
            defName = hatDef.defName;
            label = hatDef.label;
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref hatHide, "hideHat", HatEnum.HideHat);
            Scribe_Values.Look(ref draftedHide, "hideDrafted", HatStateEnum.Default);
            Scribe_Values.Look(ref indoorsHide, "hideIndoors", HatStateEnum.Default);
            Scribe_Values.Look(ref bedHide, "hideBed", HatStateEnum.Default);
        }
    }
    public class HairSaver : IExposable
    {
        public string defName;
        public string label;
        public bool forceHide;
        public HairSaver()
        {
        }
        public HairSaver(HairDef hairDef)
        {
            defName = hairDef.defName;
            label = hairDef.label;
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref forceHide, "forceHide", false);
        }
    }
    public class SettingsController : Mod
    {

        private Vector2 scrollPosition = new Vector2(0, 0);
        private Vector2 scrollPosition2 = new Vector2(0, 0);
        private float previousHatY, previousHairY;
        private string leftTableSearchBuffer = "", rightTableSearchBuffer = "";

        public SettingsController(ModContentPack content) : base(content)
        {
            GetSettings<Settings>();
        }

        public override string SettingsCategory()
        {
            return "ShowHair.ShowHair".Translate();
        }

        public override void DoSettingsWindowContents(Rect rect)
        {
            Settings.Initialize();

            Widgets.CheckboxLabeled(new Rect(0, 60, 250, 22), "ShowHair.OnlyApplyToColonists".Translate(), ref Settings.OnlyApplyToColonists);
            Widgets.CheckboxLabeled(new Rect(0, 90, 250, 22), "ShowHair.UseDontShaveHead".Translate(), ref Settings.UseDontShaveHead);
            float split = (float)Math.Floor(rect.width / 3 * 2);
            DrawTableHats(0, 220, split - 10, ref scrollPosition, ref previousHatY, "ShowHair.Hats", ref leftTableSearchBuffer);
            DrawTableHair(split + 10, 220, rect.width - split - 10, ref scrollPosition2, ref previousHairY, "ShowHair.HairThatWillBeHidden", ref rightTableSearchBuffer);
        }

        private void DrawTableHats(float x, float y, float width, ref Vector2 scroll, ref float innerY, string header, ref string searchBuffer)
        {
            Text.Font = GameFont.Small;
            const float ROW_HEIGHT = 32;
            GUI.BeginGroup(new Rect(x, y, width, 400));
            Widgets.Label(new Rect(0, 0, width, 20), header.Translate());
            searchBuffer = Widgets.TextArea(new Rect(0, 24, 200, 28), searchBuffer);
            if (Widgets.ButtonText(new Rect(200, 24, 28, 28), "X"))
                searchBuffer = "";
            if (Widgets.ButtonText(new Rect(width - 316, 28, 60, 24), "ShowHair.DefaultMode".Translate()))
            {
                string sb = searchBuffer;
                Find.WindowStack.Add(FloatMenuGenerator(typeof(HatEnum), (i) => { SetHatHide(sb, (HatEnum)i); }));
            }
            if (Widgets.ButtonText(new Rect(width - 256, 28, 60, 24), "ShowHair.DraftedMode".Translate()))
            {
                string sb = searchBuffer;
                Find.WindowStack.Add(FloatMenuGenerator(typeof(HatStateEnum), (i) => { SetDraftedHide(sb, (HatStateEnum)i); }));
            }
            if (Widgets.ButtonText(new Rect(width - 196, 28, 60, 24), "ShowHair.IndoorsMode".Translate()))
            {
                string sb = searchBuffer;
                Find.WindowStack.Add(FloatMenuGenerator(typeof(HatStateEnum), (i) => { SetIndoorsHide(sb, (HatStateEnum)i); }));
            }
            if (Widgets.ButtonText(new Rect(width - 136, 28, 60, 24), "ShowHair.BedMode".Translate()))
            {
                string sb = searchBuffer;
                Find.WindowStack.Add(FloatMenuGenerator(typeof(HatStateEnum), (i) => { SetBedHide(sb, (HatStateEnum)i); }));
            }
            Widgets.BeginScrollView(new Rect(0, 75, width, 300), ref scroll, new Rect(0, 0, width - 16, innerY));

            innerY = 0;
            int index = 0;
            foreach (HatSaver t in Settings.HatDict.Values)
            {
                if (searchBuffer != "" && !MatchesSearch(searchBuffer, t))
                    continue;

                innerY = index++ * ROW_HEIGHT;

                Widgets.ThingIcon(new Rect(x, innerY - 2, ROW_HEIGHT - 2, ROW_HEIGHT - 2), DefDatabase<ThingDef>.GetNamed(t.defName));
                Widgets.Label(new Rect(34, innerY, 184, ROW_HEIGHT), t.label + ":");

                Text.Font = GameFont.Tiny;
                if (Widgets.ButtonText(new Rect(width - 316, innerY, 60, 26), t.hatHide.ToString().Translate()))
                {
                    Find.WindowStack.Add(FloatMenuGenerator(typeof(HatEnum), (i) => { t.hatHide = (HatEnum)i; }));
                }
                if (Widgets.ButtonText(new Rect(width - 256, innerY, 60, 26), t.draftedHide.ToString().Translate()))
                {
                    Find.WindowStack.Add(FloatMenuGenerator(typeof(HatStateEnum), (i) => { t.draftedHide = (HatStateEnum)i; }));
                }
                if (Widgets.ButtonText(new Rect(width - 196, innerY, 60, 26), t.indoorsHide.ToString().Translate()))
                {
                    Find.WindowStack.Add(FloatMenuGenerator(typeof(HatStateEnum), (i) => { t.indoorsHide = (HatStateEnum)i; }));
                }
                if (Widgets.ButtonText(new Rect(width - 136, innerY, 60, 26), t.bedHide.ToString().Translate()))
                {
                    Find.WindowStack.Add(FloatMenuGenerator(typeof(HatStateEnum), (i) => { t.bedHide = (HatStateEnum)i; }));
                }
                Text.Font = GameFont.Small;


            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            innerY += ROW_HEIGHT;
        }

        private void DrawTableHair(float x, float y, float width, ref Vector2 scroll, ref float innerY, string header, ref string searchBuffer)
        {
            Text.Font = GameFont.Small;
            const float ROW_HEIGHT = 32;
            GUI.BeginGroup(new Rect(x, y, width, 400));
            Widgets.Label(new Rect(0, 0, width, 20), header.Translate());
            searchBuffer = Widgets.TextArea(new Rect(0, 24, 200, 28), searchBuffer);
            if (Widgets.ButtonText(new Rect(200, 24, 28, 28), "X"))
                searchBuffer = "";
            Widgets.BeginScrollView(new Rect(0, 75, width, 300), ref scroll, new Rect(0, 0, width - 16, innerY));

            innerY = 0;
            int index = 0;
            foreach (HairSaver t in Settings.HairDict.Values)
            {
                if (!MatchesSearch(searchBuffer, t))
                    continue;

                innerY = index++ * ROW_HEIGHT;

                Widgets.Label(new Rect(0, innerY, 184, ROW_HEIGHT), t.label + ":");
                Widgets.Checkbox(new Vector2(width - 40, innerY - 1), ref t.forceHide);
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            innerY += ROW_HEIGHT;
        }
        private FloatMenu FloatMenuGenerator(Type enumType, Action<int> action)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (int i in Enum.GetValues(enumType))
            {
                string name = Enum.GetName(enumType, i);
                options.Add(new FloatMenuOption(name.Translate(), () => action(i)));
            }
            return new FloatMenu(options);
        }

        private bool MatchesSearch(string searchBuffer, HatSaver t)
        {
            return MatchesSearch(searchBuffer, t.label);
        }
        private bool MatchesSearch(string searchBuffer, HairSaver t)
        {
            return MatchesSearch(searchBuffer, t.label);
        }
        private bool MatchesSearch(string searchBuffer, string t)
        {
            return searchBuffer == "" || t.ToLower().Contains(searchBuffer);
        }

        private void SetHatHide(string searchBuffer, HatEnum v)
        {
            foreach (HatSaver t in Settings.HatDict.Values)
            {
                if (MatchesSearch(searchBuffer, t))
                {
                    t.hatHide = v;
                }
            }
        }
        private void SetDraftedHide(string searchBuffer, HatStateEnum v)
        {
            foreach (HatSaver t in Settings.HatDict.Values)
            {
                if (MatchesSearch(searchBuffer, t))
                {
                    t.draftedHide = v;
                }
            }
        }
        private void SetIndoorsHide(string searchBuffer, HatStateEnum v)
        {
            foreach (HatSaver t in Settings.HatDict.Values)
            {
                if (MatchesSearch(searchBuffer, t))
                {
                    t.indoorsHide = v;
                }
            }
        }
        private void SetBedHide(string searchBuffer, HatStateEnum v)
        {
            foreach (HatSaver t in Settings.HatDict.Values)
            {
                if (MatchesSearch(searchBuffer, t))
                {
                    t.bedHide = v;
                }
            }
        }
    }

    public enum HatEnum
    {
        HideHat,
        ShowsHair,
        HidesAllHair,
        HidesHairShowBeard,
        ShowsHairHidesBeard
    }
    public enum HatStateEnum
    {
        Default,
        HideHat,
        ShowsHair,
        HidesAllHair,
        HidesHairShowBeard,
        ShowsHairHidesBeard
    }

    class Settings : ModSettings
    {
        public static bool OnlyApplyToColonists = false;
        public static bool UseDontShaveHead = true;

        public static Dictionary<ThingDef, HatSaver> HatDict = new Dictionary<ThingDef, HatSaver>();
        public static Dictionary<HairDef, HairSaver> HairDict = new Dictionary<HairDef, HairSaver>();

        private static ToSave ToSave = null;

        public static bool TryGetPawnHatState(Pawn pawn, ThingDef def, out HatEnum hatEnum)
        {
            if (HatDict.TryGetValue(def, out var hat))
            {
                if (hat.bedHide != HatStateEnum.Default && pawn.InBed())
                {
                    hatEnum = (HatEnum)(hat.bedHide - 1);
                }
                else if (hat.draftedHide != HatStateEnum.Default && pawn.Drafted) {
                    hatEnum = (HatEnum)(hat.draftedHide - 1);
                }
                else if (hat.indoorsHide != HatStateEnum.Default && pawn.TryGetComp<CompCeilingDetect>(out var comp) && comp.isIndoors.GetValueOrDefault())
                {
                    hatEnum = (HatEnum)(hat.indoorsHide - 1);
                } else
                {
                    hatEnum = hat.hatHide;
                }
                return true;
            }
            hatEnum = default;
            return false;
        }

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
                ToSave.hatList = HatDict.Values.Where((h) => h.hatHide != HatEnum.HideHat
                                                             || h.draftedHide != HatStateEnum.Default
                                                             || h.indoorsHide != HatStateEnum.Default
                                                             || h.bedHide != HatStateEnum.Default).ToList();
                ToSave.hairList = HairDict.Values.Where((h) => h.forceHide).ToList();
            }
            Scribe_Values.Look(ref OnlyApplyToColonists, "OnlyApplyToColonists", false, false);
            Scribe_Values.Look(ref UseDontShaveHead, "UseDontShaveHead", true, false);
            Scribe_Collections.Look(ref ToSave.hatList, "Hats", LookMode.Deep);
            Scribe_Collections.Look(ref ToSave.hairList, "Hair", LookMode.Deep);

            switch (Scribe.mode)
            {
                case LoadSaveMode.Saving:
                    ToSave?.Clear();
                    ToSave = null;
                    break;
            }
        }

        private static bool isInitialized = false;
        internal static void Initialize()
        {
            if (!isInitialized)
            {
                if (ToSave == null)
                    ToSave = new ToSave();

                foreach (ThingDef d in DefDatabase<ThingDef>.AllDefs)
                {
                    isInitialized = true;

                    if (d.apparel == null ||
                    !IsHeadwear(d.apparel) ||
                    (String.IsNullOrEmpty(d.apparel.wornGraphicPath) &&
                    d.apparel.wornGraphicPaths?.Count == 0))
                    {
                        continue;
                    }

                    HatSaver hat = ToSave?.hatList?.FirstOrFallback((h) => h.defName == d.defName);
                    if (hat != null)
                    {
                        hat.label = d.label;
                        HatDict.Add(d, hat);
                    }
                    else
                        HatDict.Add(d, new HatSaver(d));
                }

                foreach (HairDef d in DefDatabase<HairDef>.AllDefs)
                {
                    isInitialized = true;

                    if (d == HairDefOf.Bald)
                    {
                        continue;
                    }

                    HairSaver hair = ToSave?.hairList?.FirstOrFallback((h) => h.defName == d.defName);
                    if (hair != null)
                    {
                        hair.label = d.label;
                        HairDict.Add(d, hair);
                    }
                    else
                        HairDict.Add(d, new HairSaver(d));
                }

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
        public List<HatSaver> hatList = null;
        public List<HairSaver> hairList = null;

        public ToSave()
        {
            hatList = new List<HatSaver>();
            hairList = new List<HairSaver>();
        }

        public void Clear()
        {
            hatList?.Clear();
            hairList?.Clear();
        }
    }
}