namespace HDT_BgPickAdvisor.Models
{
    public sealed class HeroOffer
    {
        public int DbfId { get; set; }
        public string CardId { get; set; }
        public string Name { get; set; }
        public int ZonePosition { get; set; }
        public string Source { get; set; }
        public HeroMeta Meta { get; set; }
        public bool IsBestPick { get; set; }
    }
}
