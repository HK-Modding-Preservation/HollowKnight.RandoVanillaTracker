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

        private static Menu _instance = null;
        internal static Menu Instance => _instance ??= new Menu();

        public static void OnExitMenu()
        {
            _instance = null;
        }

        public static void Hook()
        {
            RandomizerMenuAPI.AddMenuPage(Instance.ConstructMenu, Instance.HandleButton);
            MenuChangerMod.OnExitMainMenu += OnExitMenu;
        }

        private bool HandleButton(MenuPage landingPage, out SmallButton button)
        {
            JumpToRVTButton = new(landingPage, Localize("RandoVanillaTracker"));
            JumpToRVTButton.AddHideAndShowEvent(landingPage, rvtPage);
            button = JumpToRVTButton;
            return true;
        }

        private void ConstructMenu(MenuPage landingPage)
        {
            rvtPage = new MenuPage(Localize("RandoVanillaTracker"), landingPage);
            rvtPageTitle = new MenuLabel(rvtPage, "Select vanilla placements to track", MenuLabel.Style.Title);
            rvtPageTitle.MoveTo(new Vector2(0, 400));
            rvtMEF = new(rvtPage, RVT.GS);
            ConstructInteropButtons();
            rvtGIP = new(rvtPage, new Vector2(0, 300), 4, 50f, 400f, true, rvtMEF.Elements.Concat(rvtInteropButtons).ToArray());
            Localize(rvtMEF);
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
    }
}
