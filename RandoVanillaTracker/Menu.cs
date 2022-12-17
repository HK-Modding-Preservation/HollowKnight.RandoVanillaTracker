using MenuChanger;
using MenuChanger.Extensions;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
using RandomizerMod.Menu;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RandomizerMod.Localization;
using RVT = RandoVanillaTracker.RandoVanillaTracker;

namespace RandoVanillaTracker
{
    // More or less copied from flibber's RandoPlus
    internal class Menu
    {
        internal MenuPage rvtPage;
        internal MenuLabel rvtPageTitle;
        internal MenuElementFactory<GlobalSettings> rvtMEF;
        internal GridItemPanel rvtGIP;

        internal List<ToggleButton> rvtInteropButtons;
        internal SmallButton JumpToRVTButton;

        internal static Menu Instance { get; private set; }

        public static void OnExitMenu()
        {
            Instance = null;
        }

        public static void Hook()
        {
            RandomizerMenuAPI.AddMenuPage(ConstructMenu, HandleButton);
            MenuChangerMod.OnExitMainMenu += OnExitMenu;
        }

        private static bool HandleButton(MenuPage landingPage, out SmallButton button)
        {
            button = Instance.JumpToRVTButton;
            return true;
        }

        private static void ConstructMenu(MenuPage landingPage) => Instance = new(landingPage);

        private Menu(MenuPage landingPage)
        {
            rvtPage = new MenuPage(Localize("RandoVanillaTracker"), landingPage);
            rvtPageTitle = new MenuLabel(rvtPage, "Select vanilla placements to track", MenuLabel.Style.Title);
            rvtPageTitle.MoveTo(new Vector2(0, 400));
            rvtMEF = new(rvtPage, RVT.GS);

            rvtMEF.ElementLookup["Charms"].SelfChanged += CostFixes.Other_SelfChanged;
            rvtMEF.ElementLookup["Relics"].SelfChanged += CostFixes.Other_SelfChanged;
            rvtMEF.ElementLookup["PaleOre"].SelfChanged += CostFixes.Other_SelfChanged;
            rvtMEF.ElementLookup["RancidEggs"].SelfChanged += CostFixes.Other_SelfChanged;
            rvtMEF.ElementLookup["MaskShards"].SelfChanged += CostFixes.Other_SelfChanged;

            ConstructInteropButtons();
            rvtGIP = new(rvtPage, new Vector2(0, 300), 4, 50f, 400f, true, rvtMEF.Elements.Concat(rvtInteropButtons).ToArray());
            Localize(rvtMEF);

            foreach (IValueElement e in rvtMEF.Elements)
            {
                e.SelfChanged += obj => SetTopLevelButtonColor();
            }

            foreach (ToggleButton b in rvtInteropButtons)
            {
                b.SelfChanged += obj => SetTopLevelButtonColor();
            }

            JumpToRVTButton = new(landingPage, Localize("RandoVanillaTracker"));
            JumpToRVTButton.AddHideAndShowEvent(landingPage, rvtPage);
            SetTopLevelButtonColor();
        }

        private void ConstructInteropButtons()
        {
            rvtInteropButtons = new();

            foreach (string pool in RVT.Instance.Interops.Keys)
            {
                ToggleButton button = new(rvtPage, pool);
                button.SetValue(RVT.GS.trackInteropPool[pool]);
                button.SelfChanged += b => RVT.GS.trackInteropPool[pool] = (bool)b.Value;

                rvtInteropButtons.Add(button);
            }
        }

        private void SetTopLevelButtonColor()
        {
            if (JumpToRVTButton != null)
            {
                JumpToRVTButton.Text.color = rvtMEF.Elements.Any(e => e.Value is true) || rvtInteropButtons.Any(b => b.Value is true)
                    ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;
            }
        }
    }
}
