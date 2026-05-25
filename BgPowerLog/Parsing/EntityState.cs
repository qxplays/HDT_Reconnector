namespace BgPowerLog.Parsing
{
    internal sealed class EntityState
    {
        public int EntityId { get; set; }
        public string CardId { get; set; }
        public string CardType { get; set; }
        public string Zone { get; set; }
        public int Controller { get; set; }
        public int ZonePosition { get; set; }
        public int Attack { get; set; }
        public int Health { get; set; }
        public int Damage { get; set; }
        public int TechLevel { get; set; }
        public int Premium { get; set; }
        public int Taunt { get; set; }
        public int DivineShield { get; set; }
        public int Poisonous { get; set; }
        public int PlayerId { get; set; }
        public int BaconDummyPlayer { get; set; }

        public bool IsMinion =>
            CardType == "MINION" ||
            (!string.IsNullOrEmpty(CardId) &&
             !CardId.Contains("HERO") &&
             Zone == "PLAY" &&
             Attack > 0);

        public bool IsHero =>
            CardType == "HERO" || (!string.IsNullOrEmpty(CardId) && CardId.Contains("HERO"));

        public int EffectiveHealth => Health > 0 ? System.Math.Max(0, Health - Damage) : Health;
    }
}
