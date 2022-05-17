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
        public override string GetVersion() => "1.1.0";

        public static GlobalSettings GS = new();
        public void OnLoadGlobal(GlobalSettings gs) => GS = gs;
        public GlobalSettings OnSaveGlobal() => GS;

        internal Dictionary<string, InteropInfo> Interops = new();

        public override void Initialize()
        {
            Instance = this;

            if (ModHooks.GetMod("Randomizer 4") is not Mod) return;
            
            Menu.Hook();
            PlacementModifier.Hook();
        }

        /// <summary>
        /// Pass interop information to RandoVanillaTracker. Needs to be called once before the Randomizer Connections menu is entered for the first time.
        /// TrackPool should correspond to a bool in GlobalSettings (or equivalent)
        /// </summary>
        public static void AddInterop(string pool, Func<bool> RandomizePool, FieldInfo TrackPool, object TrackPoolObj, Func<List<RandoPlacement>> GetPlacements)
        {
            if (Instance.Interops.ContainsKey(pool)) return;

            Instance.Interops.Add(pool, new()
            {
                RandomizePool = RandomizePool,
                TrackPool = TrackPool,
                TrackPoolObj = TrackPoolObj,
                GetPlacements = GetPlacements
            });
        }
    }

    internal class InteropInfo
    {
        public Func<bool> RandomizePool;
        public FieldInfo TrackPool;
        public object TrackPoolObj;
        public Func<List<RandoPlacement>> GetPlacements;
    }
}