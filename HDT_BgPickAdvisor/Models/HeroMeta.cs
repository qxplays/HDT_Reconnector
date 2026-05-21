namespace HDT_BgPickAdvisor.Models
{
    public sealed class HeroMeta
    {
        public int DbfId { get; set; }
        public string CardId { get; set; }
        public string Name { get; set; }
        public double? AvgPlacement { get; set; }
        public double? PickRate { get; set; }
        public string Tier { get; set; }
        public int Rank { get; set; }
    }
}
