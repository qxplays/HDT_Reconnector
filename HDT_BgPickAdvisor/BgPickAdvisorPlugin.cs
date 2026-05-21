using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using HDT_BgPickAdvisor.Logging;
using HDT_BgPickAdvisor.Meta;
using HDT_BgPickAdvisor.UI;
using Hearthstone_Deck_Tracker.Plugins;
using Core = Hearthstone_Deck_Tracker.API.Core;

namespace HDT_BgPickAdvisor
{
    public class BgPickAdvisorPlugin : IPlugin
    {
        private MenuItem _menuItem;
        private PickAdvisorOverlay _overlay;
        private DebugOffersWindow _debugWindow;

        public string Name => "BgPickAdvisor";

        public string Description =>
            "Battlegrounds hero and accessory pick overlay (meta from BgMetaApi).";

        public string ButtonText => "Debug offers";

        public string Author => "thorx";

        public Version Version => new Version(1, 2, 0, 0);

        public MenuItem MenuItem => _menuItem;

        public void OnLoad()
        {
            FileLogger.ResetSession();
            FileLogger.Info($"Log: {FileLogger.LogPath}");

            _menuItem = new MenuItem
            {
                Header = "BG Pick Advisor",
                IsCheckable = true
            };
            _menuItem.Checked += OnMenuChecked;
            _menuItem.Unchecked += OnMenuUnchecked;

            EnableOverlay();
            _menuItem.IsChecked = true;

            Task.Run(RefreshMetaOnStartup);
        }

        public void OnUnload()
        {
            if (_menuItem != null)
            {
                _menuItem.Checked -= OnMenuChecked;
                _menuItem.Unchecked -= OnMenuUnchecked;
                DisableOverlay();
                _menuItem.IsChecked = false;
            }

            _debugWindow?.Close();
            _debugWindow = null;
        }

        public void OnButtonPress()
        {
            if (_debugWindow == null || !_debugWindow.IsVisible)
            {
                _debugWindow = new DebugOffersWindow();
                _debugWindow.Closed += (_, __) => _debugWindow = null;
                _debugWindow.Show();
            }
            else
            {
                _debugWindow.Activate();
            }
        }

        public void OnUpdate() => _overlay?.OnUpdate();

        private static void RefreshMetaOnStartup()
        {
            try
            {
                FileLogger.Info($"Loading meta from {MetaCatalog.ApiBaseUrl}...");
                MetaService.Store.EnsureLoaded();
                MetaService.Store.RefreshAllAsync().GetAwaiter().GetResult();
                FileLogger.Info(
                    $"Meta ready: heroes={MetaService.Store.HeroCount}, lesser={MetaService.Store.TrinketLesserCount}, greater={MetaService.Store.TrinketGreaterCount}");
            }
            catch (Exception ex)
            {
                FileLogger.Error("Startup meta load failed", ex);
            }
        }

        private void OnMenuChecked(object sender, EventArgs e) => EnableOverlay();

        private void OnMenuUnchecked(object sender, EventArgs e) => DisableOverlay();

        private void EnableOverlay()
        {
            if (_overlay != null)
                return;

            _overlay = new PickAdvisorOverlay();
            _overlay.Reset();
            Core.OverlayCanvas.Children.Add(_overlay);
            FileLogger.Info("Overlay enabled");
        }

        private void DisableOverlay()
        {
            if (_overlay == null)
                return;

            _overlay.Reset();
            Core.OverlayCanvas.Children.Remove(_overlay);
            _overlay.Dispose();
            _overlay = null;
            FileLogger.Info("Overlay disabled");
        }
    }
}
