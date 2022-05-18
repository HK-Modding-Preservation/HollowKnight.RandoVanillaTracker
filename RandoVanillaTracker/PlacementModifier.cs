using MonoMod.RuntimeDetour;
using RandomizerCore;
using RandomizerCore.Logic;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using System;
using System.Collections.Generic;
using System.Linq;
using Rand = RandomizerCore.Randomization.Randomizer;
using RC = RandomizerMod.RC.RandoController;
using RVT = RandoVanillaTracker.RandoVanillaTracker;

namespace RandoVanillaTracker
{
    internal class PlacementModifier
    {
        private static RC rc;
        private static LogicManager Lm => rc.ctx.LM;

        private static HashSet<string> items = new();
        private static HashSet<string> locations = new();

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
            items = new();
            locations = new();

            foreach (List<RandoPlacement>[] rpll in stagedPlacements)
            {
                foreach (List<RandoPlacement> rpl in rpll)
                {
                    foreach (RandoPlacement rp in rpl)
                    {
                        if (!items.Contains(rp.Item.Name))
                        {
                            items.Add(rp.Item.Name);
                        }

                        if (!locations.Contains(rp.Location.Name))
                        {
                            locations.Add(rp.Location.Name);
                        }
                    }
                }
            }

            List<RandoPlacement> newPlacements = new();

            foreach (PoolDef pool in Data.Pools)
            {
                if (RVT.GS.GetFieldByName(pool.Path.Replace("PoolSettings.", "")))
                {
                    TryMakeItemPlacements(pool.Vanilla, out List<RandoPlacement> placements);

                    newPlacements.AddRange(placements);
                }
            }

            if (RVT.GS.Transitions)
            {
                TryMakeTransitionPlacements(Data.GetRoomTransitionNames(), out List<RandoPlacement> placements);

                newPlacements.AddRange(placements);
            }

            foreach (KeyValuePair<string, Func<List<RandoPlacement>>> kvp in RVT.Instance.Interops)
            {
                if (RVT.GS.trackInteropPool[kvp.Key])
                {
                    List<RandoPlacement> placements = new(kvp.Value.Invoke().Where(p => !ShopNames.Contains(p.Location.Name)));

                    newPlacements.AddRange(placements);
                }
            }

            rc.ctx.Vanilla.RemoveAll(gp => newPlacements.Any(np => gp.Item.Name == np.Item.Name));

            stagedPlacements.Add(new List<RandoPlacement>[] { newPlacements });

            return stagedPlacements;
        }

        private static void TryMakeItemPlacements(VanillaDef[] defs, out List<RandoPlacement> placements)
        {
            placements = new();

            foreach (VanillaDef vd in defs)
            {
                if (!items.Contains(vd.Item) && !ShopNames.Contains(vd.Location))
                {
                    placements.Add(MakeItemPlacement(vd));
                }
            }
        }

        private static RandoPlacement MakeItemPlacement(VanillaDef def)
        {
            RandoModItem item = new()
            {
                item = Lm.GetItem(def.Item),
                ItemDef = Data.GetItemDef(def.Item)
            };

            RandoModLocation location = new()
            {
                logic = Lm.GetLogicDef(def.Location),
                LocationDef = Data.GetLocationDef(def.Location)
            };

            void ApplyCost(CostDef cost)
            {
                switch (cost.Term)
                {
                    case "GEO":
                        location.AddCost(new LogicGeoCost(Lm, cost.Amount));
                        break;
                    default:
                        location.AddCost(new SimpleCost(Lm.GetTerm(cost.Term), cost.Amount));
                        break;
                }
            }

            if (Data.TryGetCost(def.Location, out CostDef baseCost))
            {
                ApplyCost(baseCost);
            }

            if (def.Costs != null)
            {
                foreach (CostDef cost in def.Costs)
                {
                    ApplyCost(cost);
                }
            }

            return new(item, location);
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

        private static void TryMakeTransitionPlacements(IEnumerable<string> transitions, out List<RandoPlacement> placements)
        {
            placements = new();

            foreach (string source in transitions)
            {
                TransitionDef sourceDef = Data.GetTransitionDef(source);

                if (sourceDef.VanillaTarget != null && Lm.TransitionLookup.ContainsKey(source)
                    && !locations.Contains(source) && !items.Contains(sourceDef.VanillaTarget))
                {
                    placements.Add(MakeTransitionPlacement(sourceDef));
                }
            }
        }

        private static RandoPlacement MakeTransitionPlacement(TransitionDef sourceDef)
        {
            RandoModTransition source = new(Lm.GetTransition(sourceDef.Name));
            source.TransitionDef = sourceDef;

            RandoModTransition target = new(Lm.GetTransition(sourceDef.VanillaTarget));
            target.TransitionDef = Data.GetTransitionDef(sourceDef.VanillaTarget);

            return new(target, source);
        }

    }
}
