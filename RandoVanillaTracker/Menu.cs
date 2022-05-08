using MenuChanger;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
using MenuChanger.Extensions;
using RandomizerMod.Menu;
using static RandomizerMod.Localization;
using UnityEngine;

namespace RandoVanillaTracker
{
    // More or less copied from flibber's RandoPlus
    internal class Menu
    {
        internal MenuPage rvtPage;
        internal MenuLabel rvtPageTitle;
        internal MenuElementFactory<GlobalSettings> rvtMEF;
        internal GridItemPanel rvtVIP;

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
            rvtPageTitle = new MenuLabel(rvtPage, "Select vanilla pools to track", MenuLabel.Style.Title);
            rvtPageTitle.MoveTo(new Vector2(0, 400));
            rvtMEF = new(rvtPage, RandoVanillaTracker.GS);
            rvtVIP = new(rvtPage, new Vector2(0, 300), 4, 50f, 400f, true, rvtMEF.Elements);
            Localize(rvtMEF);
        }
    }
}
