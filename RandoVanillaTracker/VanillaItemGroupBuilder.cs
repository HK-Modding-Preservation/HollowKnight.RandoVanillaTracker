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
        public List<VanillaDef> VanillaPlacements = new();
        public List<VanillaDef> VanillaTransitions = new();

        public override void Apply(List<RandomizationGroup> groups, RandoFactory factory)
        {
            int count = 0;

            foreach (VanillaDef vd in VanillaPlacements)
            {
                RandoModItem item = factory.MakeItem(vd.Item);
                
                RandoModLocation location = factory.MakeLocation(vd.Location);

                if (vd.Costs is not null)
                {
                    // Clear cost for grubfather/seer added by randomizer
                    if ((vd.Location == LocationNames.Grubfather || vd.Location == LocationNames.Seer) && vd.Costs is not null)
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
