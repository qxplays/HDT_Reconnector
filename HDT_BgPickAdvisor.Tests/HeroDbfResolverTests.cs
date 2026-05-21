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
        public void GetBaseHeroCardId_strips_skin_suffix(string skinId, string expectedBase)
        {
            Assert.Equal(expectedBase, HeroDbfResolver.GetBaseHeroCardId(skinId));
        }
    }
}
