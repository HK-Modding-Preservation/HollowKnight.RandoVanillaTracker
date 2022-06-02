using ItemChanger;
using RandomizerCore.Extensions;
using RandomizerMod.Logging;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ElementType = RandomizerMod.RC.RequestBuilder.ElementType;
using RVT = RandoVanillaTracker.RandoVanillaTracker;

namespace RandoVanillaTracker
{
    internal class PlacementModifier
    {
        // We enumerate transition groups because the vanilla item group builder isn't an item group, so is returned by 
        private static VanillaItemGroupBuilder GetVanillaGroupBuilder(RequestBuilder rb) => rb.EnumerateTransitionGroups().OfType<VanillaItemGroupBuilder>().First();


        public static void Hook()
        {
            RequestBuilder.OnUpdate.Subscribe(-5000f, RecordTrackedPools);
            RequestBuilder.OnUpdate.Subscribe(300f, TrackTransitions);
            RequestBuilder.OnUpdate.Subscribe(5000f, TrackInteropItems);
            RequestBuilder.OnUpdate.Subscribe(300f, DerandomizeTrackedItems);

            RequestBuilder.OnUpdate.Subscribe(1.5f, RebalanceShopCounts);

            SettingsLog.AfterLogSettings += LogRVTSettings;
        }

        private static HashSet<string> _recordedPools = new();

        private static void LogRVTSettings(LogArguments args, TextWriter tw)
        {
            tw.WriteLine("RandoVanillaTracker Tracked Pools");
            foreach (string s in _recordedPools)
            {
                tw.WriteLine($"- {s}");
            }
        }

        private static void TrackTransitions(RequestBuilder rb)
        {
            if (!RVT.GS.Transitions) return;

            VanillaItemGroupBuilder vb = GetVanillaGroupBuilder(rb);

            foreach (VanillaDef vd in rb.Vanilla.Values.SelectMany(x => x).ToList())
            {
                if (Data.IsTransition(vd.Item) && Data.IsTransition(vd.Location))
                {
                    //rb.RemoveFromVanilla(vd);
                    RemoveFromVanilla(rb, vd);
                    vb.VanillaTransitions.Add(vd);
                }
            }
        }

        private static void TrackInteropItems(RequestBuilder rb)
        {
            VanillaItemGroupBuilder vb = GetVanillaGroupBuilder(rb);

            foreach (KeyValuePair<string, Func<List<VanillaDef>>> kvp in RVT.Instance.Interops)
            {
                if (RVT.GS.trackInteropPool[kvp.Key])
                {
                    foreach (VanillaDef vd in kvp.Value.Invoke())
                    {
                        if (rb.Vanilla.TryGetValue(vd.Location, out List<VanillaDef> defs))
                        {
                            int count = defs.Count(x => x == vd);
                            //rb.RemoveFromVanilla(vd);
                            RemoveFromVanilla(rb, vd);

                            for (int i = 0; i < count; i++)
                            {
                                vb.VanillaPlacements.Add(vd);
                            }
                        }
                    }
                }
            }
        }

        // Trick the randomizer into thinking that the pools are randomized
        private static void RecordTrackedPools(RequestBuilder rb)
        {
            _recordedPools.Clear();
            _shopVanillaCounts.Clear();

            // Add a group that will catch all vanilla items
            StageBuilder sb = rb.InsertStage(0, "RVT Item Stage");
            VanillaItemGroupBuilder vb = new();
            vb.label = "RVT Item Group";
            sb.Add(vb);

            HashSet<string> vanillaPaths = new();

            foreach (PoolDef pool in Data.Pools)
            {
                if (RVT.GS.GetFieldByName(pool.Path.Replace("PoolSettings.", "")) && pool.IsVanilla(rb.gs))
                {
                    _recordedPools.Add(pool.Name);
                    // Delay setting the vanilla path so dream warriors and bosses don't go out of sync
                    vanillaPaths.Add(pool.Path);

                    foreach (VanillaDef vd in pool.Vanilla)
                    {
                        vb.VanillaPlacements.Add(vd);

                        // Record the number of vanilla items that each shop will hold, to fix rebalancing issues later
                        if (ShopNames.Contains(vd.Location))
                        {
                            if (_shopVanillaCounts.ContainsKey(vd.Location))
                            {
                                _shopVanillaCounts[vd.Location]++;
                            }
                            else
                            {
                                _shopVanillaCounts.Add(vd.Location, 1);
                            }
                        }
                    }
                }
            }

            foreach (string vanillaPath in vanillaPaths)
            {
                rb.gs.Set(vanillaPath, true);
            }
        }

        // We need to remove items one by one so cursed masks work
        private static void DerandomizeTrackedItems(RequestBuilder rb)
        {
            foreach (PoolDef pool in Data.Pools)
            {
                if (!_recordedPools.Contains(pool.Name)) continue;

                foreach (string item in pool.IncludeItems)
                {
                    ((ItemGroupBuilder)rb.GetGroupFor(item, ElementType.Item)).Items.Remove(item, 1);
                }
                foreach (string location in pool.IncludeLocations)
                {
                    ((ItemGroupBuilder)rb.GetGroupFor(location, ElementType.Location)).Locations.Remove(location, 1);
                }

                // Tracked VanillaDefs might be added to vanilla by randomizer, for example depending on long location settings.
                // Undo that behaviour here.
                foreach (VanillaDef vd in pool.Vanilla)
                {
                    // TODO - this will only work when the RemoveFromVanilla(VanillaDef) function is fixed in Randomizer
                    //rb.RemoveFromVanilla(vd);

                    // Temporary fixed version of RemoveFromVanilla
                    RemoveFromVanilla(rb, vd);
                }
            }
        }

        private static void RemoveFromVanilla(RequestBuilder rb, VanillaDef vd)
        {
            if (rb.Vanilla.TryGetValue(vd.Location, out List<VanillaDef> defs))
            {
                defs.RemoveAll(def => def.Equals(vd));
            }
        }

        private static Dictionary<string, int> _shopVanillaCounts = new();

        public static readonly HashSet<string> ShopNames = new()
        {
            LocationNames.Iselda,
            LocationNames.Sly,
            LocationNames.Sly_Key,
            LocationNames.Salubra,
            LocationNames.Leg_Eater,
            LocationNames.Seer,
            LocationNames.Grubfather
        };

        // Rebalance so there are at least enough locations in each shop for vanilla items
        private static void RebalanceShopCounts(RequestBuilder rb)
        {
            Dictionary<string, HashSet<string>> multiSets = new();

            Dictionary<string, int> counts = new();

            // Decouple the counting from the rebalancing
            foreach (ItemGroupBuilder gb in rb.EnumerateItemGroups())
            {

                multiSets.Add(gb.label, new());

                int count = 0;
                foreach (string l in gb.Locations.EnumerateDistinct())
                {
                    if (rb.TryGetLocationDef(l, out LocationDef def) && def.FlexibleCount)
                    {
                        multiSets[gb.label].Add(l);
                        count += gb.Locations.GetCount(l);
                    }
                }

                counts.Add(gb.label, count);
            }

            // Allocate vanilla slots by removing them evenly from counts
            int vanillaTotalCount = _shopVanillaCounts.Values.Sum();

            int countPerGB = Math.DivRem(vanillaTotalCount, counts.Count(), out int rem);

            foreach(ItemGroupBuilder gb in rb.EnumerateItemGroups())
            {
                counts[gb.label] -= countPerGB;

                // Also distribute remainder evenly
                if (rem > 0)
                {
                    counts[gb.label]--;
                    rem--;
                }
            }

            //TODO: Make sure each group gets one of each location if possible

            foreach (ItemGroupBuilder gb in rb.EnumerateItemGroups())
            {
                string[] multi = multiSets[gb.label].OrderBy(s => s).ToArray();
                foreach (string l in multi)
                {
                    gb.Locations.Set(l, 0);
                }
                while (counts[gb.label]-- > 0)
                {
                    gb.Locations.Add(rb.rng.Next(multi));
                }
            }

            foreach (KeyValuePair<string, int> kvp in _shopVanillaCounts)
            {
                ((ItemGroupBuilder)rb.GetGroupFor(kvp.Key, ElementType.Location)).Locations.AddRange(Enumerable.Repeat(kvp.Key, kvp.Value));
            }
        }
    }
}
