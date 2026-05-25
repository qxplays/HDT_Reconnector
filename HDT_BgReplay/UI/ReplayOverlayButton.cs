using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HDT_BgReplay.Overlay;
using Core = Hearthstone_Deck_Tracker.API.Core;

namespace HDT_BgReplay.UI
{
    /// <summary>Visible on HDT overlay (not inside Hearthstone client UI).</summary>
    public sealed class ReplayOverlayButton : UserControl, IDisposable
    {
        private readonly Border _button;
        private readonly Action _onClick;

        public ReplayOverlayButton(Action onClick)
        {
            _onClick = onClick;
            Width = 110;
            Height = 32;

            var text = new TextBlock
            {
                Text = "BG Replay",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _button = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x2a, 0x5a, 0x8a)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x14, 0x16, 0x17)),
                BorderThickness = new Thickness(2),
                Cursor = Cursors.Hand,
                Child = text
            };
            _button.MouseLeftButtonDown += (_, __) => _onClick?.Invoke();

            Content = _button;
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
            Margin = new Thickness(16, 56, 0, 0);
            Visibility = Visibility.Collapsed;

            OverlayRegistration.RegisterClickable(_button);
        }

        public void OnUpdate()
        {
            var game = Core.Game;
            if (game == null)
            {
                Visibility = Visibility.Collapsed;
                return;
            }

            // BG menu, lobby, or active BG match — show on HDT overlay.
            var show = game.IsInMenu || game.IsBattlegroundsMatch;
            Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        public void Dispose() => OverlayRegistration.UnregisterClickable(_button);
    }
}
