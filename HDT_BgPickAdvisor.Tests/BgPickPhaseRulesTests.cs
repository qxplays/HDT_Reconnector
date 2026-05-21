using HDT_BgPickAdvisor.Detection;
using Xunit;

namespace HDT_BgPickAdvisor.Tests
{
    public class BgPickPhaseRulesTests
    {
        [Fact]
        public void Trinket_overlay_requires_hero_pick_done_not_before()
        {
            Assert.False(BgPickPhaseRules.ShouldShowTrinketOverlay(
                isBattlegrounds: true,
                isInMenu: false,
                isHeroPickPhase: false,
                isHeroPickingDone: false,
                trinketOfferCount: 3));

            Assert.True(BgPickPhaseRules.ShouldShowTrinketOverlay(
                isBattlegrounds: true,
                isInMenu: false,
                isHeroPickPhase: false,
                isHeroPickingDone: true,
                trinketOfferCount: 3));
        }

        [Fact]
        public void Trinket_overlay_hidden_during_hero_pick_even_with_offers()
        {
            Assert.False(BgPickPhaseRules.ShouldShowTrinketOverlay(
                isBattlegrounds: true,
                isInMenu: false,
                isHeroPickPhase: true,
                isHeroPickingDone: false,
                trinketOfferCount: 3));
        }

        [Fact]
        public void Trinket_overlay_needs_at_least_two_offers()
        {
            Assert.False(BgPickPhaseRules.ShouldShowTrinketOverlay(
                isBattlegrounds: true,
                isInMenu: false,
                isHeroPickPhase: false,
                isHeroPickingDone: true,
                trinketOfferCount: 1));
        }
    }
}
