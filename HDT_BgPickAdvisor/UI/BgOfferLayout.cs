using System;
using HDT_BgPickAdvisor.Meta;

namespace HDT_BgPickAdvisor.UI
{
    /// <summary>Compact per-slot cards centered over BG pick rows (heroes low, trinkets mid).</summary>
    internal static class BgOfferLayout
    {
        public const string DefaultTier = MetaDefaults.DefaultTier;

        private const double HeroHalfSpan4 = 0.205;
        private const double HeroHalfSpan3 = 0.165;
        private const double HeroHalfSpan2 = 0.105;

        private const double TrinketHalfSpan4 = 0.200;
        private const double TrinketHalfSpan3 = 0.160;
        private const double TrinketHalfSpan2 = 0.100;

        public struct SlotRect
        {
            public double Left;
            public double Top;
            public double Width;
            public double Height;
        }

        public static SlotRect GetSlotRect(int slotIndex, int slotCount, bool heroPick, double canvasWidth, double canvasHeight)
        {
            slotCount = Math.Max(1, slotCount);
            slotIndex = Math.Max(0, Math.Min(slotIndex, slotCount - 1));

            if (canvasWidth <= 0)
                canvasWidth = 1920;
            if (canvasHeight <= 0)
                canvasHeight = 1080;

            if (heroPick)
                return GetHeroSlot(slotIndex, slotCount, canvasWidth, canvasHeight);

            return GetTrinketSlot(slotIndex, slotCount, canvasWidth, canvasHeight);
        }

        private static SlotRect GetHeroSlot(int index, int count, double w, double h)
        {
            var centerX = w * GetCenterRatio(index, count, heroPick: true);
            var cardWidth = Clamp(w * 0.064, 96, w >= 1600 ? 118 : 112);
            var cardHeight = Clamp(h * 0.058, 54, 72);
            var topY = h * ResolveHeroTopRatio(w, h);

            return new SlotRect
            {
                Left = centerX - cardWidth / 2,
                Top = topY,
                Width = cardWidth,
                Height = cardHeight
            };
        }

        private static SlotRect GetTrinketSlot(int index, int count, double w, double h)
        {
            var centerX = w * GetCenterRatio(index, count, heroPick: false);
            var cardWidth = Clamp(w * 0.058, 90, w >= 1600 ? 112 : 106);
            var cardHeight = Clamp(h * 0.058, 52, 72);
            var topY = h * ResolveTrinketTopRatio(w, h, count);

            return new SlotRect
            {
                Left = centerX - cardWidth / 2,
                Top = topY,
                Width = cardWidth,
                Height = cardHeight
            };
        }

        private static double GetCenterRatio(int index, int count, bool heroPick)
        {
            var halfSpan = heroPick ? GetHeroHalfSpan(count) : GetTrinketHalfSpan(count);
            return BuildCenteredCenters(count, halfSpan)[index];
        }

        private static double GetHeroHalfSpan(int count)
        {
            if (count >= 4) return HeroHalfSpan4;
            if (count == 3) return HeroHalfSpan3;
            if (count == 2) return HeroHalfSpan2;
            return 0;
        }

        private static double GetTrinketHalfSpan(int count)
        {
            if (count >= 4) return TrinketHalfSpan4;
            if (count == 3) return TrinketHalfSpan3;
            if (count == 2) return TrinketHalfSpan2;
            return 0;
        }

        private static double[] BuildCenteredCenters(int count, double halfSpan)
        {
            if (count <= 1)
                return new[] { 0.5 };

            var left = 0.5 - halfSpan;
            var right = 0.5 + halfSpan;
            var result = new double[count];
            for (var i = 0; i < count; i++)
                result[i] = left + (right - left) * i / (count - 1);
            return result;
        }

        /// <summary>Just above the 4 hero portraits (bottom-center row).</summary>
        private static double ResolveHeroTopRatio(double w, double h)
        {
            var aspect = w / Math.Max(h, 1);
            var top = 0.785;

            if (aspect >= 1.70)
                top += 0.012;
            if (h >= 1200)
                top += 0.015;
            if (h >= 1440)
                top += 0.01;

            return Clamp(top, 0.75, 0.86);
        }

        /// <summary>Mid-lower row for trinket discover (above hero pick band).</summary>
        private static double ResolveTrinketTopRatio(double w, double h, int count)
        {
            var aspect = w / Math.Max(h, 1);
            var top = count <= 2 ? 0.618 : 0.608;

            if (aspect >= 1.70)
                top += 0.01;
            if (h >= 1200)
                top += 0.015;
            if (h >= 1440)
                top += 0.01;

            return Clamp(top, 0.59, 0.74);
        }

        private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));
    }
}
