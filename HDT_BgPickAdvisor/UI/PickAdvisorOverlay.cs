using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HDT_BgPickAdvisor.Detection;
using HDT_BgPickAdvisor.Logging;
using HDT_BgPickAdvisor.Meta;
using HDT_BgPickAdvisor.Models;
using HDT_BgPickAdvisor.Overlay;
using Core = Hearthstone_Deck_Tracker.API.Core;

namespace HDT_BgPickAdvisor.UI
{
    public sealed class PickAdvisorOverlay : UserControl, IDisposable
    {
        private const double EdgeMargin = 12;
        private const double CardWidth = 150;
        private const double CardHeight = 96;

        private readonly BgHeroOfferDetector _heroDetector = new BgHeroOfferDetector();
        private readonly BgTrinketOfferDetector _trinketDetector = new BgTrinketOfferDetector();
        private readonly StackPanel _root;
        private readonly SizeChangedEventHandler _sizeChangedHandler;
        private string _lastFingerprint = "";

        public PickAdvisorOverlay()
        {
            _root = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x14, 0x16, 0x17))
            };
            Content = _root;
            Visibility = Visibility.Collapsed;

            _sizeChangedHandler = (_, __) => UpdatePosition();
            Core.OverlayCanvas.SizeChanged += _sizeChangedHandler;
            OverlayRegistration.RegisterClickable(this);
            UpdatePosition();
        }

        public void Reset()
        {
            _lastFingerprint = "";
            Visibility = Visibility.Collapsed;
            _root.Children.Clear();
        }

        public void OnUpdate()
        {
            var heroActive = _heroDetector.IsActive;
            var trinketActive = !heroActive && _trinketDetector.IsActive;

            if (!heroActive && !trinketActive)
            {
                _lastFingerprint = "";
                Visibility = Visibility.Collapsed;
                _root.Children.Clear();
                return;
            }

            var meta = MetaService.Store;

            if (heroActive)
            {
                var offers = _heroDetector.GetOffers();
                if (offers.Count < 2)
                {
                    Visibility = Visibility.Collapsed;
                    return;
                }

                var fingerprint = BuildFingerprint("H", offers.Select(o => $"{o.DbfId}:{o.CardId}"));
                if (fingerprint != _lastFingerprint || _root.Children.Count == 0)
                {
                    _lastFingerprint = fingerprint;
                    FileLogger.Info($"Hero pick: {offers.Count} offers [{string.Join(", ", offers.Select(o => $"{o.Name}(dbf={o.DbfId})"))}]");
                    meta.RankHeroOffers(offers);
                    RenderCards(offers.Select(ToCardModel).ToList(), "Hero pick");
                }
            }
            else
            {
                var offers = _trinketDetector.GetOffers();
                if (offers.Count < 2)
                {
                    _lastFingerprint = "";
                    Visibility = Visibility.Collapsed;
                    _root.Children.Clear();
                    return;
                }

                var fingerprint = BuildFingerprint("T", offers.Select(o => $"{o.DbfId}:{o.CardId}"));
                if (fingerprint != _lastFingerprint || _root.Children.Count == 0)
                {
                    _lastFingerprint = fingerprint;
                    FileLogger.Info($"Trinket pick: {offers.Count} offers [{string.Join(", ", offers.Select(o => $"{o.Name}(dbf={o.DbfId})"))}]");
                    meta.RankTrinketOffers(offers);
                    RenderCards(offers.Select(ToCardModel).ToList(), "Accessory pick");
                }
            }

            Visibility = Visibility.Visible;
            UpdatePosition();
        }

        public void Dispose()
        {
            Core.OverlayCanvas.SizeChanged -= _sizeChangedHandler;
            OverlayRegistration.UnregisterClickable(this);
        }

        private static string BuildFingerprint(string prefix, IEnumerable<string> parts) =>
            prefix + string.Join("|", parts.OrderBy(p => p));

        private static CardViewModel ToCardModel(HeroOffer o) =>
            new CardViewModel(o.Name, o.Meta?.Tier, o.Meta?.AvgPlacement, o.Meta?.PickRate, o.IsBestPick, o.Meta == null);

        private static CardViewModel ToCardModel(TrinketOffer o) =>
            new CardViewModel(o.Name, o.Meta?.Tier, o.Meta?.AvgPlacement, o.Meta?.PickRate, o.IsBestPick, o.Meta == null);

        private void RenderCards(IReadOnlyList<CardViewModel> cards, string title)
        {
            _root.Children.Clear();

            var header = new TextBlock
            {
                Text = title,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(8, 6, 8, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            _root.Children.Add(header);

            foreach (var card in cards)
            {
                var border = new Border
                {
                    Width = CardWidth,
                    Height = CardHeight,
                    Margin = new Thickness(6),
                    BorderThickness = new Thickness(card.IsBest ? 3 : 1),
                    BorderBrush = card.IsBest
                        ? new SolidColorBrush(Color.FromRgb(0x3d, 0xa9, 0x4a))
                        : new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
                    Background = new SolidColorBrush(Color.FromRgb(0x23, 0x27, 0x2a)),
                    Child = BuildCardContent(card)
                };
                _root.Children.Add(border);
            }
        }

        private static UIElement BuildCardContent(CardViewModel card)
        {
            var panel = new StackPanel { Margin = new Thickness(8) };

            panel.Children.Add(new TextBlock
            {
                Text = card.Name ?? "?",
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            panel.Children.Add(new TextBlock
            {
                Text = FormatTierLine(card),
                Foreground = Brushes.LightGray,
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0)
            });

            var pickRateText = FormatPickRateLine(card);
            if (!string.IsNullOrEmpty(pickRateText))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = pickRateText,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xa8, 0xb0, 0xb8)),
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 0)
                });
            }

            if (card.IsBest)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "Best pick",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x3d, 0xa9, 0x4a)),
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 0)
                });
            }

            return panel;
        }

        private void UpdatePosition()
        {
            var canvas = Core.OverlayCanvas;
            var width = canvas.ActualWidth > 0 ? canvas.ActualWidth : canvas.Width;
            var height = canvas.ActualHeight > 0 ? canvas.ActualHeight : canvas.Height;
            if (width <= 0 || height <= 0)
                return;

            var selfWidth = Math.Max(ActualWidth, _root.Children.Count * (CardWidth + 12) + 80);
            Canvas.SetLeft(this, Math.Max(EdgeMargin, (width - selfWidth) / 2));
            Canvas.SetTop(this, EdgeMargin + 40);
        }

        private static string FormatTierLine(CardViewModel card)
        {
            var tierText = string.IsNullOrEmpty(card.Tier) ? "—" : card.Tier;
            if (card.MissingMeta)
                return $"Tier {tierText} · no meta";

            if (card.AvgPlacement.HasValue)
                return $"Tier {tierText} · {card.AvgPlacement:0.00} avg";

            return $"Tier {tierText}";
        }

        private static string FormatPickRateLine(CardViewModel card)
        {
            if (card.MissingMeta || !card.PickRate.HasValue)
                return null;

            return $"Pick {card.PickRate:0.#}%";
        }

        private sealed class CardViewModel
        {
            public CardViewModel(
                string name,
                string tier,
                double? avgPlacement,
                double? pickRate,
                bool isBest,
                bool missingMeta)
            {
                Name = name;
                Tier = tier;
                AvgPlacement = avgPlacement;
                PickRate = pickRate;
                IsBest = isBest;
                MissingMeta = missingMeta;
            }

            public string Name { get; }
            public string Tier { get; }
            public double? AvgPlacement { get; }
            public double? PickRate { get; }
            public bool IsBest { get; }
            public bool MissingMeta { get; }
        }
    }
}
