using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BgPowerLog.Models;

namespace HDT_BgReplay.UI
{
    internal static class BoardCardView
    {
        public static FrameworkElement CreateHero(ReplayHero hero, Brush accent, double width = 110)
        {
            var panel = new StackPanel { Margin = new Thickness(4), Width = width };
            var img = CreateImage(hero?.CardId, width, width * 1.35);
            panel.Children.Add(img);

            var stats = new TextBlock
            {
                Text = hero == null
                    ? "(no hero)"
                    : $"{CardNameResolver.GetName(hero.CardId)}\nHP {hero.Health}  T{hero.TechLevel}",
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Foreground = accent,
                FontWeight = FontWeights.SemiBold,
                FontSize = 11,
                Margin = new Thickness(0, 4, 0, 0)
            };
            panel.Children.Add(stats);
            return panel;
        }

        public static FrameworkElement CreateMinion(ReplayMinion minion, double width = 78)
        {
            var border = new Border
            {
                BorderBrush = Brushes.DimGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(3),
                Background = Brushes.White,
                Width = width
            };

            var stack = new StackPanel();
            stack.Children.Add(CreateImage(minion.CardId, width - 6, (width - 6) * 1.33));

            var tags = new StringBuilder();
            if (minion.Premium) tags.Append("★ ");
            if (minion.Taunt) tags.Append("[T] ");
            if (minion.DivineShield) tags.Append("[D] ");
            if (minion.Poisonous) tags.Append("[P] ");

            stack.Children.Add(new TextBlock
            {
                Text = $"{tags}{minion.Attack}/{minion.Health}",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 4)
            });

            border.Child = stack;
            return border;
        }

        private static Image CreateImage(string cardId, double width, double height)
        {
            var image = new Image
            {
                Width = width,
                Height = height,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(2)
            };

            if (string.IsNullOrWhiteSpace(cardId))
                return image;

            CardArtService.LoadPortrait(cardId, bmp =>
            {
                if (bmp == null)
                    return;

                Application.Current?.Dispatcher.BeginInvoke(new Action(() => image.Source = bmp));
            });

            return image;
        }
    }
}
