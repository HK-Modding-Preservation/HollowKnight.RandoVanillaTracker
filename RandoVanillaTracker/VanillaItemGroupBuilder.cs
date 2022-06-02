using ItemChanger;
using RandomizerCore.Logic;
using RandomizerCore.Randomization;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using System.Collections.Generic;
using System.Linq;

namespace RandoVanillaTracker
{
    public class VanillaItemGroupBuilder : GroupBuilder
    {
        public static readonly Dictionary<string, int> UniqueShopCostLookup = new()
        {
            [ItemNames.Wayward_Compass] = 220,
            [ItemNames.Quill] = 120,
            
            [ItemNames.Simple_Key] = 950,
            [ItemNames.Rancid_Egg] = 50,
            [ItemNames.Lumafly_Lantern] = 1800,
            [ItemNames.Gathering_Swarm] = 300,
            [ItemNames.Stalwart_Shell] = 300,
            [ItemNames.Heavy_Blow] = 350,
            [ItemNames.Elegant_Key] = 800,
            [ItemNames.Sprintmaster] = 400,

            [ItemNames.Longnail] = 300,
            [ItemNames.Shaman_Stone] = 220,
            [ItemNames.Lifeblood_Heart] = 250,
            [ItemNames.Steady_Body] = 120,
            [ItemNames.Quick_Focus] = 800,

            [ItemNames.Fragile_Heart] = 350,
            [ItemNames.Fragile_Greed] = 250,
            [ItemNames.Fragile_Strength] = 600,
        };

        public static readonly HashSet<string> ShopNames = new()
        {
            LocationNames.Iselda,
            LocationNames.Sly,
            LocationNames.Sly_Key,
            LocationNames.Salubra,
            LocationNames.Leg_Eater,
        };

        public static readonly List<int> SlyMaskShardCosts = new() { 150, 500, 800, 1500 };
        public static readonly List<int> SlyVesselFragmentCosts = new() { 550, 900 };

        public List<VanillaDef> VanillaPlacements = new();
        public List<VanillaDef> VanillaTransitions = new();

        public override void Apply(List<RandomizationGroup> groups, RandoFactory factory)
        {
            int count = 0;
            int slyMaskShards = 0;
            int slyVesselFragments = 0;

            foreach (VanillaDef vd in VanillaPlacements)
            {
                RandoModItem item = factory.MakeItem(vd.Item);
                
                RandoModLocation location = factory.MakeLocation(vd.Location);

                if (vd.Costs is not null)
                {
                    // Clear cost for grubfather/seer added by randomizer
                    if ((vd.Location == LocationNames.Grubfather || vd.Location == LocationNames.Seer) && location.costs is not null)
                    {
                        location.costs.Clear();
                    }

                    foreach (CostDef cost in vd.Costs ?? Enumerable.Empty<CostDef>())
                    {
                        switch (cost.Term)
                        {
                            case "GEO":
                                location.AddCost(new LogicGeoCost(factory.lm, cost.Amount));
                                break;
                            default:
                                location.AddCost(new SimpleCost(factory.lm.GetTerm(cost.Term), cost.Amount));
                                break;
                        }
                    }
                }

                else if (ShopNames.Contains(vd.Location))
                {
                    if (location.costs is not null)
                    {
                        location.costs.Clear();
                    }

                    if (UniqueShopCostLookup.TryGetValue(vd.Item, out int amount))
                    {
                        location.AddCost(new LogicGeoCost(factory.lm, amount));
                    }
                    else if (vd.Item == ItemNames.Mask_Shard)
                    {
                        location.AddCost(new LogicGeoCost(factory.lm, SlyMaskShardCosts[slyMaskShards]));
                        slyMaskShards++;
                    }
                    else if (vd.Item == ItemNames.Vessel_Fragment)
                    {
                        location.AddCost(new LogicGeoCost(factory.lm, SlyMaskShardCosts[slyVesselFragments]));
                        slyVesselFragments++;
                    }
                }

                RandomizationGroup group = new()
                {
                    Items = new[] { item },
                    Locations = new[] { location },
                    Label = $"{label}-{++count}",
                    Strategy = strategy ?? factory.gs.ProgressionDepthSettings.GetItemPlacementStrategy(),
                };

                groups.Add(group);
            }

            foreach (VanillaDef vd in VanillaTransitions)
            {
                RandomizationGroup group = new()
                {
                    Items = new[] { factory.MakeTransition(vd.Item) },
                    Locations = new[] { factory.MakeTransition(vd.Location) },
                    Label = $"{label}-{++count}",
                    Strategy = strategy ?? factory.gs.ProgressionDepthSettings.GetTransitionPlacementStrategy(),
                };

                groups.Add(group);

            }
        }
    }
}
