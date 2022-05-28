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
                    rb.RemoveFromVanilla(vd);
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
                            rb.RemoveFromVanilla(vd);

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
                    rb.RemoveFromVanilla(vd);
                }
            }
        }
    }
}
