using System.Linq;
using ItemChanger.Tags;
using RandomizerMod.RC;

namespace RandoVanillaTracker
{
    internal static class RMMInterop
    {
        internal static void Hook()
        {
            RandoController.OnExportCompleted += AddVanillaPinTag;
        }

        internal static void AddVanillaPinTag(RandoController rc)
        {
            if (
                rc.rb.EnumerateTransitionGroups().OfType<VanillaItemGroupBuilder>().FirstOrDefault()
                is not VanillaItemGroupBuilder gb
            )
            {
                return;
            }

            foreach (var p in ItemChanger.Internal.Ref.Settings.Placements.Values)
            {
                if (gb.VanillaPlacements.Any(vp => vp.Location == p.Name))
                {
                    p.tags.Add(
                        new InteropTag()
                        {
                            Message = "RandoSupplementalMetadata",
                            Properties = { { "MakeVanillaPin", true } },
                        }
                    );
                }
            }
        }
    }
}
