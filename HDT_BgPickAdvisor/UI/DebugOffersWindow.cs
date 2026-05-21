using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HDT_BgPickAdvisor.Detection;
using HDT_BgPickAdvisor.Logging;
using HDT_BgPickAdvisor.Meta;
using HDT_BgPickAdvisor.Models;

namespace HDT_BgPickAdvisor.UI
{
    public sealed class DebugOffersWindow : Window
    {
        private readonly TextBox _textBox;
        private readonly BgHeroOfferDetector _heroDetector = new BgHeroOfferDetector();
        private readonly BgTrinketOfferDetector _trinketDetector = new BgTrinketOfferDetector();

        public DebugOffersWindow()
        {
            Title = "BG Pick Advisor — Debug";
            Width = 720;
            Height = 520;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var panel = new DockPanel { Margin = new Thickness(8) };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };
            DockPanel.SetDock(buttons, Dock.Top);

            var refreshBtn = new Button { Content = "Refresh dump", Width = 120, Margin = new Thickness(0, 0, 8, 0) };
            refreshBtn.Click += (_, __) => RefreshDump();
            buttons.Children.Add(refreshBtn);

            var copyBtn = new Button { Content = "Copy", Width = 80, Margin = new Thickness(0, 0, 8, 0) };
            copyBtn.Click += (_, __) => Clipboard.SetText(_textBox.Text);
            buttons.Children.Add(copyBtn);

            var metaBtn = new Button { Content = "Reload meta", Width = 110, Margin = new Thickness(0, 0, 8, 0) };
            metaBtn.Click += async (_, __) =>
            {
                metaBtn.IsEnabled = false;
                try
                {
                    await MetaService.Store.RefreshAllAsync();
                    MessageBox.Show(
                        $"Heroes: {MetaService.Store.HeroCount}\nLesser: {MetaService.Store.TrinketLesserCount}\nGreater: {MetaService.Store.TrinketGreaterCount}",
                        "Meta",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                finally
                {
                    metaBtn.IsEnabled = true;
                }
            };
            buttons.Children.Add(metaBtn);

            var apiBtn = new Button { Content = "Open API", Width = 90, Margin = new Thickness(0, 0, 8, 0) };
            apiBtn.Click += (_, __) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(MetaCatalog.ApiBaseUrl);
                }
                catch
                {
                    // ignored
                }
            };
            buttons.Children.Add(apiBtn);

            var logBtn = new Button { Content = "Log file", Width = 90 };
            logBtn.Click += (_, __) =>
            {
                if (File.Exists(FileLogger.LogPath))
                    System.Diagnostics.Process.Start(FileLogger.LogPath);
            };
            buttons.Children.Add(logBtn);

            panel.Children.Add(buttons);

            _textBox = new TextBox
            {
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                TextWrapping = TextWrapping.NoWrap
            };
            panel.Children.Add(_textBox);

            Content = panel;
            RefreshDump();
        }

        private void RefreshDump()
        {
            var snapshot = BuildSnapshot();
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer
            {
                MaxJsonLength = int.MaxValue,
                RecursionLimit = 32
            };
            _textBox.Text = serializer.Serialize(snapshot);

            try
            {
                File.WriteAllText(FileLogger.DebugLastPath, _textBox.Text, Encoding.UTF8);
            }
            catch
            {
                // ignored
            }
        }

        private DebugSnapshot BuildSnapshot()
        {
            var game = Hearthstone_Deck_Tracker.API.Core.Game;
            return new DebugSnapshot
            {
                TimestampUtc = DateTime.UtcNow,
                IsBattlegroundsMatch = game?.IsBattlegroundsMatch ?? false,
                IsInMenu = game?.IsInMenu ?? true,
                HeroPickActive = BgPickPhaseHelper.IsHeroPickPhase(),
                TrinketPickActive = BgPickPhaseHelper.IsTrinketPickPhase(),
                Heroes = _heroDetector.GetOffers(),
                Trinkets = _trinketDetector.GetOffers(),
                MetaApi = MetaCatalog.ApiBaseUrl,
                HeroMetaCount = MetaService.Store.HeroCount,
                TrinketLesserCount = MetaService.Store.TrinketLesserCount,
                TrinketGreaterCount = MetaService.Store.TrinketGreaterCount
            };
        }
    }
}
