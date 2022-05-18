using MonoMod.RuntimeDetour;
using RandomizerCore;
using RandomizerCore.Logic;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using System;
using System.Collections.Generic;
using RC = RandomizerMod.RC.RandoController;
using Rand = RandomizerCore.Randomization.Randomizer;
using RVT = RandoVanillaTracker.RandoVanillaTracker;

namespace RandoVanillaTracker
{
    internal class PlacementModifier
    {
        private static RC rc;

        private static Hook ControllerRunHook;
        private static Hook RandomizerRunHook;

        public static void Hook()
        {
            Type ControllerRun = Type.GetType("RandomizerMod.RC.RandoController, RandomizerMod");
            Type RandomizerRun = Type.GetType("RandomizerCore.Randomization.Randomizer, RandomizerCore");

            if (ControllerRun == null || RandomizerRun == null) return;

            ControllerRunHook = new Hook(ControllerRun.GetMethod("Run"), typeof(PlacementModifier).GetMethod(nameof(OnControllerRun)));
            RandomizerRunHook = new Hook(RandomizerRun.GetMethod("Run"), typeof(PlacementModifier).GetMethod(nameof(OnRandomizerRun)));
        }

        public static void OnControllerRun(Action<RC> orig, RC self)
        {
            rc = self;

            orig(self);
        }

        public static List<List<RandoPlacement>[]> OnRandomizerRun(Func<Rand, List<List<RandoPlacement>[]>> orig, Rand self)
        {
            return AddVanillaPlacements(orig(self));
        }

        public static List<List<RandoPlacement>[]> AddVanillaPlacements(List<List<RandoPlacement>[]> stagedPlacements)
        {
            HashSet<string> ItemsToConvert = new();
            List<RandoPlacement> newPlacements = new();

            foreach (PoolDef pool in Data.Pools)
            {
                if (!rc.gs.PoolSettings.GetFieldByName(pool.Path.Replace("PoolSettings.", ""))
                    && RVT.GS.GetFieldByName(pool.Path.Replace("PoolSettings.", "")))
                {
                    foreach (string item in pool.IncludeItems)
                    {
                        ItemsToConvert.Add(item);
                    }
                }
            }

            foreach (GeneralizedPlacement p in rc.ctx.Vanilla)
            {
                // Ignore shop placements
                if (ItemsToConvert.Contains(p.Item.Name) && !ShopNames.Contains(p.Location.Name))
                {
                    // More or less copied from homothety's RandomizerMod RandoFactory
                    RandoModItem item = new()
                    {
                        item = rc.ctx.LM.GetItem(p.Item.Name),
                        ItemDef = Data.GetItemDef(p.Item.Name)
                    };

                    RandoModLocation rl = new()
                    {
                        logic = rc.ctx.LM.GetLogicDef(p.Location.Name),
                        LocationDef = Data.GetLocationDef(p.Location.Name)
                    };

                    // Given that shops have been filtered out, some of the following is probably redundant
                    if (Data.TryGetCost(p.Location.Name, out CostDef def))
                    {
                        switch (def.Term)
                        {
                            case "ESSENCE":
                            case "GRUBS":
                                break;
                            case "SIMPLE":
                                rl.AddCost(new SimpleCost(rc.ctx.LM.GetTerm("SIMPLE"), 1));
                                break;
                            case "Spore_Shroom":
                                rl.AddCost(new SimpleCost(rc.ctx.LM.GetTerm("Spore_Shroom"), 1));
                                break;
                            case "GEO":
                                rl.AddCost(new LogicGeoCost(rc.ctx.LM, def.Amount));
                                break;
                            default:
                                rl.AddCost(new SimpleCost(rc.ctx.LM.GetTerm(def.Term), def.Amount));
                                break;
                        }
                    }

                    newPlacements.Add(new(item, rl));
                }
            }

            stagedPlacements.Add(new List<RandoPlacement>[] { newPlacements });

            rc.ctx.Vanilla.RemoveAll(p => ItemsToConvert.Contains(p.Item.Name));

            foreach (KeyValuePair<string, InteropInfo> kvp in RVT.Instance.Interops)
            {
                if (!kvp.Value.RandomizePool.Invoke() && RVT.GS.trackInteropPool[kvp.Key])
                {
                    stagedPlacements.Add(new List<RandoPlacement>[] { kvp.Value.GetPlacements.Invoke() });
                }
            }

            return stagedPlacements;
        }

        private static readonly HashSet<string> ShopNames = new()
        {
            "Sly",
            "Sly_(Key)",
            "Iselda",
            "Salubra",
            "Salubra_(Requires_Charms)",
            "Leg_Eater",
            "Grubfather",
            "Seer",
            "Egg_Shop"
        };

        
    }
}
