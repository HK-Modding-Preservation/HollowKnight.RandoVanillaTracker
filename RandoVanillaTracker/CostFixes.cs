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

        private static bool overrideMaxGrubCost;

        public static void Hook()
        {
            RandomizerMenuAPI.AddMenuPage(OnMenuLoad, NoButton);
        }

        private static void OnMenuLoad(MenuPage landingPage)
        {
            MenuElementFactory<PoolSettings> poolMEF = GetField<RandomizerMenu, MenuElementFactory<PoolSettings>>(RandomizerMenuAPI.Menu, "poolMEF");

            poolCharms = poolMEF.ElementLookup["Charms"];
            poolCharms.SelfChanged += SelfChanged;

            poolRelics = poolMEF.ElementLookup["Relics"];
            poolRelics.SelfChanged += SelfChanged;

            poolPaleOre = poolMEF.ElementLookup["PaleOre"];
            poolPaleOre.SelfChanged += SelfChanged;

            poolRancidEggs = poolMEF.ElementLookup["RancidEggs"];
            poolRancidEggs.SelfChanged += SelfChanged;

            poolMaskShards = poolMEF.ElementLookup["MaskShards"];
            poolMaskShards.SelfChanged += SelfChanged;

            MenuElementFactory<CostSettings> costMEF = GetField<RandomizerMenu, MenuElementFactory<CostSettings>>(RandomizerMenuAPI.Menu, "costMEF");
            
            maxGrubCost = costMEF.ElementLookup["MaximumGrubCost"];
            maxGrubCost.SelfChanged += SelfChanged;

            grubTolerance = costMEF.ElementLookup["GrubTolerance"];

            overrideMaxGrubCost = false;
            SetGrubCostSettings();
        }

        private static bool NoButton(MenuPage landingPage, out SmallButton button)
        {
            button = null;
            return false;
        }

        public static void SelfChanged(IValueElement obj)
        {
            SetGrubCostSettings();
        }

        private static void SetGrubCostSettings()
        {
            if (!overrideMaxGrubCost)
            {
                origMaxGrubCost = (int)maxGrubCost.Value;
                origGrubTolerance = (int)grubTolerance.Value;
            }

            if (RVT.GS.Charms && !(bool)poolCharms.Value)
            {
                TryOverrideMaxGrubCost(46);
            }
            else if (RVT.GS.Relics && !(bool)poolRelics.Value)
            {
                TryOverrideMaxGrubCost(38);
            }
            else if (RVT.GS.PaleOre && !(bool)poolPaleOre.Value)
            {
                TryOverrideMaxGrubCost(31);
            }
            else if (RVT.GS.RancidEggs && !(bool)poolRancidEggs.Value)
            {
                TryOverrideMaxGrubCost(16);
            }
            else if (RVT.GS.MaskShards && !(bool)poolMaskShards.Value)
            {
                TryOverrideMaxGrubCost(5);
            }
            else
            {
                if ((int)grubTolerance.Value != origGrubTolerance)
                {
                    grubTolerance.SetValue(origGrubTolerance);
                }

                if ((int)maxGrubCost.Value != origMaxGrubCost)
                {
                    maxGrubCost.SetValue(origMaxGrubCost);
                }

                overrideMaxGrubCost = false;
            }
        }

        private static void TryOverrideMaxGrubCost(int value)
        {
            // If the user tried to change the max grub cost while it is overridden, respect the change
            // Unfortunately there doesn't seem to be an easy way to tell if the user set the value to one of the shown numbers
            // as opposed to this mod
            if (overrideMaxGrubCost
                && (int)maxGrubCost.Value > value
                && (int)maxGrubCost.Value != 46
                && (int)maxGrubCost.Value != 38
                && (int)maxGrubCost.Value != 31
                && (int)maxGrubCost.Value != 16
                && (int)maxGrubCost.Value != 5)
            {
                origMaxGrubCost = (int)maxGrubCost.Value;
            }

            int newValue = Math.Max(value, origMaxGrubCost);

            overrideMaxGrubCost = true;

            if ((int)maxGrubCost.Value != newValue)
            {
                maxGrubCost.SetValue(newValue);

                grubTolerance.SetValue(origGrubTolerance);
            }
        }
    }
}
