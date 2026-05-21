namespace HDT_BgPickAdvisor.Models
{
    public enum TrinketPool
    {
        Unknown,
        Lesser,
        Greater
    }

    public sealed class TrinketOffer
    {
        public int DbfId { get; set; }
        public string CardId { get; set; }
        public string Name { get; set; }
        public int ZonePosition { get; set; }
        public TrinketPool Pool { get; set; }
        public string Source { get; set; }
        public TrinketMeta Meta { get; set; }
        public bool IsBestPick { get; set; }
    }
}
