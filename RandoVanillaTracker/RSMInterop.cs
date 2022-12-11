using System.Collections.Generic;
using MenuChanger.MenuElements;
using RandoSettingsManager;
using RandoSettingsManager.SettingsManagement;
using RandoSettingsManager.SettingsManagement.Versioning;
using RVT = RandoVanillaTracker.RandoVanillaTracker;

namespace RandoVanillaTracker
{
    internal static class RSMInterop
    {
        public static void Hook()
        {
            RandoSettingsManagerMod.Instance.RegisterConnection(new RVTSettingsProxy());
        }
    }

    internal class RVTSettingsProxy : RandoSettingsProxy<GlobalSettings, string>
    {
        public override string ModKey => RVT.Instance.GetName();

        public override VersioningPolicy<string> VersioningPolicy { get; }
            = new EqualityVersioningPolicy<string>(RVT.Instance.GetVersion());

        public override void ReceiveSettings(GlobalSettings settings)
        {
            settings ??= new();

            // Set vanilla buttons
            Menu.Instance.rvtMEF.SetMenuValues(settings);

            // Set interop buttons
            foreach (KeyValuePair<string, bool> kvp in settings.trackInteropPool)
            {
                ToggleButton button = Menu.Instance.rvtInteropButtons.Find(button => button.Name == kvp.Key);
                if (button is not null)
                {
                    button.SetValue(kvp.Value);
                }
            }
        }

        public override bool TryProvideSettings(out GlobalSettings settings)
        {
            settings = RVT.GS;
            return settings.AnyEnabled();
        }
    }
}