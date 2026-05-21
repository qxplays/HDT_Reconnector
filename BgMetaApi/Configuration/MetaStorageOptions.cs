namespace BgMetaApi.Configuration;

public sealed class MetaStorageOptions
{
    public string DataDirectory { get; set; } = "data";
    public string HeroesFileName { get; set; } = "heroes.json";
    public string TrinketsFileName { get; set; } = "trinkets.json";
}
