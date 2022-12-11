using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RandoVanillaTracker
{
    // More or less copied from homothety's RandomizerMod PoolSettings
    public class GlobalSettings
    {
        public bool Transitions;

        public bool Dreamers;
        public bool Skills;
        public bool Charms;
        public bool Keys;
        public bool MaskShards;
        public bool VesselFragments;
        public bool PaleOre;
        public bool CharmNotches;
        public bool GeoChests;
        public bool Relics;
        public bool RancidEggs;
        public bool Stags;
        public bool Maps;
        public bool WhisperingRoots = true;
        public bool Grubs = true;
        public bool LifebloodCocoons;
        public bool SoulTotems;
        public bool GrimmkinFlames;
        public bool GeoRocks;
        public bool BossEssence = true;
        public bool BossGeo;
        public bool LoreTablets;

        public bool JournalEntries;
        public bool JunkPitChests;

        private static readonly Dictionary<string, FieldInfo> fields = typeof(GlobalSettings)
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.FieldType == typeof(bool))
            .ToDictionary(f => f.Name, f => f);

        public bool GetFieldByName(string fieldName)
        {
            if (fields.TryGetValue(fieldName, out FieldInfo field))
            {
                return (bool)field.GetValue(this);
            }
            return false;
        }
        
        public Dictionary<string, bool> trackInteropPool = new();

        public bool AnyEnabled()
        {
            return fields.Keys.Any(f => GetFieldByName(f)) || trackInteropPool.Values.Any(interop => interop);
        }
    }
}
