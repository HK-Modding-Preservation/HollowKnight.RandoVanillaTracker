using MenuChanger;
using MenuChanger.MenuElements;
using static Modding.ReflectionHelper;
using RandomizerMod.Menu;
using RandomizerMod.Settings;
using RVT = RandoVanillaTracker.RandoVanillaTracker;
using System;

namespace RandoVanillaTracker
{
    internal class CostFixes
    {
        private static IValueElement poolCharms;
        private static IValueElement poolRelics;
        private static IValueElement poolPaleOre;
        private static IValueElement poolRancidEggs;
        private static IValueElement poolMaskShards;

        private static IValueElement maxGrubCost;
        private static IValueElement grubTolerance;

        private static int origMaxGrubCost = 23;
        private static int origGrubTolerance = 2;

        private static bool otherSettingChanged = false;

        public static void Hook()
        {
            RandomizerMenuAPI.AddMenuPage(OnMenuLoad, NoButton);
        }

        private static void OnMenuLoad(MenuPage landingPage)
        {
            MenuElementFactory<PoolSettings> poolMEF = GetField<RandomizerMenu, MenuElementFactory<PoolSettings>>(RandomizerMenuAPI.Menu, "poolMEF");

            poolCharms = poolMEF.ElementLookup["Charms"];
            poolCharms.SelfChanged += Other_SelfChanged;

            poolRelics = poolMEF.ElementLookup["Relics"];
            poolRelics.SelfChanged += Other_SelfChanged;

            poolPaleOre = poolMEF.ElementLookup["PaleOre"];
            poolPaleOre.SelfChanged += Other_SelfChanged;

            poolRancidEggs = poolMEF.ElementLookup["RancidEggs"];
            poolRancidEggs.SelfChanged += Other_SelfChanged;

            poolMaskShards = poolMEF.ElementLookup["MaskShards"];
            poolMaskShards.SelfChanged += Other_SelfChanged;

            MenuElementFactory<CostSettings> costMEF = GetField<RandomizerMenu, MenuElementFactory<CostSettings>>(RandomizerMenuAPI.Menu, "costMEF");
            
            maxGrubCost = costMEF.ElementLookup["MaximumGrubCost"];
            maxGrubCost.SelfChanged += MaxGrubCost_SelfChanged;

            grubTolerance = costMEF.ElementLookup["GrubTolerance"];
            grubTolerance.SelfChanged += GrubTolerance_SelfChanged;

            origMaxGrubCost = (int)maxGrubCost.Value;
            origGrubTolerance = (int)grubTolerance.Value;

            Other_SelfChanged(null);
        }

        private static bool NoButton(MenuPage landingPage, out SmallButton button)
        {
            button = null;
            return false;
        }

        public static int MaxGrubCostFloor()
        {
            if (RVT.GS.Charms && !(bool)poolCharms.Value)
            {
                return 46;
            }
            else if (RVT.GS.Relics && !(bool)poolRelics.Value)
            {
                return 38;
            }
            else if (RVT.GS.PaleOre && !(bool)poolPaleOre.Value)
            {
                return 31;
            }
            else if (RVT.GS.RancidEggs && !(bool)poolRancidEggs.Value)
            {
                return 16;
            }
            else if (RVT.GS.MaskShards && !(bool)poolMaskShards.Value)
            {
                return 5;
            }
            else
            {
                return 0;
            }
        }

        public static void Other_SelfChanged(IValueElement obj)
        {
            otherSettingChanged = true;

            if ((int)maxGrubCost.Value != MaxGrubCostFloor())
            {
                maxGrubCost.SetValue(Math.Max(MaxGrubCostFloor(), origMaxGrubCost));
                grubTolerance.SetValue(origGrubTolerance);
            }

            otherSettingChanged = false;
        }

        public static void MaxGrubCost_SelfChanged(IValueElement obj)
        {
            if (otherSettingChanged) return;

            if ((int)obj.Value > MaxGrubCostFloor())
            {
                origMaxGrubCost = (int)obj.Value;
            }
        }

        public static void GrubTolerance_SelfChanged(IValueElement obj)
        {
            if (otherSettingChanged) return;

            if ((int)obj.Value > MaxGrubCostFloor() + origGrubTolerance)
            {
                origGrubTolerance = (int)obj.Value;
            }
        }
    }
}
