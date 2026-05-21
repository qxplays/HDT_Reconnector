using System;
using System.Collections.Generic;

namespace HDT_BgPickAdvisor.Models
{
    public sealed class DebugSnapshot
    {
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string GamePhase { get; set; }
        public bool IsBattlegroundsMatch { get; set; }
        public bool IsInMenu { get; set; }
        public bool HeroPickActive { get; set; }
        public bool TrinketPickActive { get; set; }
        public List<HeroOffer> Heroes { get; set; } = new List<HeroOffer>();
        public List<TrinketOffer> Trinkets { get; set; } = new List<TrinketOffer>();
        public string MetaApi { get; set; }
        public int HeroMetaCount { get; set; }
        public int TrinketLesserCount { get; set; }
        public int TrinketGreaterCount { get; set; }
        public List<EntityDebugInfo> PlayerEntities { get; set; } = new List<EntityDebugInfo>();
    }

    public sealed class EntityDebugInfo
    {
        public int Id { get; set; }
        public string CardId { get; set; }
        public int DbfId { get; set; }
        public string Name { get; set; }
        public int ZonePosition { get; set; }
        public bool IsHero { get; set; }
        public Dictionary<string, int> Tags { get; set; } = new Dictionary<string, int>();
    }
}
