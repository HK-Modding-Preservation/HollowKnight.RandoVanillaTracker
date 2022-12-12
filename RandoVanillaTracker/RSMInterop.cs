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
            if (settings is not null)
            {
                HashSet<string> invalidPools = new();

                // Validate interop settings
                foreach (KeyValuePair<string, bool> kvp in settings.trackInteropPool)
                {
                    ToggleButton button = Menu.Instance.rvtInteropButtons.Find(button => button.Name == kvp.Key);
                    if (button is null && kvp.Value)
                    {
                        invalidPools.Add(kvp.Key);
                    }
                }

                if (invalidPools.Count > 0)
                {
                    throw new ValidationException($"Connection mods are missing for the following pool settings in RandoVanillaTracker: {string.Join(", ", invalidPools)}");
                }

                // Set vanilla settings
                Menu.Instance.rvtMEF.SetMenuValues(settings);

                // Set interop settings
                foreach (ToggleButton b in Menu.Instance.rvtInteropButtons)
                {
                    if (settings.trackInteropPool.TryGetValue(b.Name, out bool value))
                    {
                        // This should be true if the settings were properly provided, but just in case...
                        b.SetValue(value);
                    }
                    else
                    {
                        b.SetValue(false);
                    }
                }
            }
            else
            {
                // Turn off everything except for pools from interop mods that aren't currently registered/installed
                // (and therefore are left alone to preserve their setting)
                foreach (IValueElement e in Menu.Instance.rvtMEF.Elements)
                {
                    e.SetValue(false);
                }

                foreach (ToggleButton b in Menu.Instance.rvtInteropButtons)
                {
                    b.SetValue(false);
                }
            }
        }

        public override bool TryProvideSettings(out GlobalSettings settings)
        {
            settings = GlobalSettings.MinimalClone(RVT.GS);
            return settings.AnyEnabled();
        }
    }
}