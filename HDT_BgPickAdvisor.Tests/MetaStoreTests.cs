using System.Collections.Generic;
using HDT_BgPickAdvisor.Meta;
using HDT_BgPickAdvisor.Models;
using Xunit;

namespace HDT_BgPickAdvisor.Tests
{
    public sealed class MetaStoreTests
    {
        [Fact]
        public void RankTrinketOffers_with_missing_meta_does_not_throw()
        {
            var store = new MetaStore();
            var offers = new List<TrinketOffer>
            {
                new TrinketOffer { DbfId = 999001, CardId = "TEST_A", Name = "A", ZonePosition = 1, Pool = TrinketPool.Unknown },
                new TrinketOffer { DbfId = 999002, CardId = "TEST_B", Name = "B", ZonePosition = 2, Pool = TrinketPool.Lesser },
            };

            store.RankTrinketOffers(offers);

            Assert.Null(offers[0].Meta);
            Assert.Null(offers[1].Meta);
            Assert.True(offers[0].IsBestPick || offers[1].IsBestPick);
        }
    }
}
