using System.IO;
using System.Linq;
using HDT_BgPickAdvisor.Detection;
using Xunit;

namespace HDT_BgPickAdvisor.Tests
{
    public class HeroDbfResolverTests
    {
        [Theory]
        [InlineData("BG22_HERO_001_SKIN_E", "BG22_HERO_001")]
        [InlineData("TB_BaconShop_HERO_95_SKIN_F", "TB_BaconShop_HERO_95")]
        [InlineData("TB_BaconShop_HERO_01", "TB_BaconShop_HERO_01")]
        [InlineData("BG20_HERO_282_SKIN_E", "BG20_HERO_282")]
        public void GetBaseHeroCardId_strips_skin_suffix(string skinId, string expectedBase)
        {
            Assert.Equal(expectedBase, HeroDbfResolver.GetBaseHeroCardId(skinId));
        }

        [Fact]
        public void ResolveMetaDbfId_maps_hacker_tamsin_skin_when_hearthdb_present()
        {
            if (!TryLoadHearthDb())
                return;

            var metaDbf = HeroDbfResolver.ResolveMetaDbfId(117881, "BG20_HERO_282_SKIN_E");
            Assert.Equal(74646, metaDbf);
        }

        private static bool TryLoadHearthDb()
        {
            try
            {
                var hdt = Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                    "HearthstoneDeckTracker");
                var dll = Directory.GetDirectories(hdt, "app-*")
                    .SelectMany(d => Directory.GetFiles(d, "HearthDb.dll", SearchOption.AllDirectories))
                    .FirstOrDefault();
                if (dll == null)
                    return false;

                System.Reflection.Assembly.LoadFrom(dll);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
