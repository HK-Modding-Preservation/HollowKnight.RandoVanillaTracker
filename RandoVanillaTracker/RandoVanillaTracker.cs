using Modding;
using RandomizerCore;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace RandoVanillaTracker
{
    public class RandoVanillaTracker : Mod, IGlobalSettings<GlobalSettings>
    {
        public static RandoVanillaTracker Instance;
        
        public RandoVanillaTracker()
        {
            Instance = this;
        }
        
        public override string GetVersion() => "1.1.0";

        public static GlobalSettings GS = new();
        public void OnLoadGlobal(GlobalSettings gs) => GS = gs;
        public GlobalSettings OnSaveGlobal() => GS;

        internal Dictionary<string, InteropInfo> Interops = new();

        public override void Initialize()
        {
            if (ModHooks.GetMod("Randomizer 4") is not Mod) return;
            
            Menu.Hook();
            PlacementModifier.Hook();
        }

        /// <summary>
        /// Pass interop information to RandoVanillaTracker. Needs to be called once before the Randomizer Connections menu is entered for the first time.
        /// </summary>
        public static void AddInterop(string pool, Func<bool> RandomizePool, Func<List<RandoPlacement>> GetPlacements)
        {
            if (Instance.Interops.ContainsKey(pool)) return;

            Instance.Interops.Add(pool, new()
            {
                RandomizePool = RandomizePool,
                GetPlacements = GetPlacements
            });
            
            if (!GS.trackInteropPool.ContainsKey(pool))
            {
                GS.trackInteropPool.Add(pool, false);
            }
        }
    }

    internal class InteropInfo
    {
        public Func<bool> RandomizePool;
        public Func<List<RandoPlacement>> GetPlacements;
    }
}
