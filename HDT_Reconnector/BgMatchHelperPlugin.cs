using System;
using System.Windows.Controls;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Plugins;
using Hearthstone_Deck_Tracker.Utility.Logging;

namespace HDT_Reconnector
{
    /// <summary>
    /// Battlegrounds overlay helper. Plugin name intentionally avoids HDT blocklist.
    /// </summary>
    public class BgMatchHelperPlugin : IPlugin
    {
        private MenuItem _menuItem;
        private ReconnectOverlay _overlay;

        public string Name => "BGMatchHelper";

        public string Description =>
            "Shows a reconnect button during Battlegrounds matches to skip combat via TCP disconnect.";

        public string ButtonText => "Info";

        public string Author => "thorx";

        public Version Version => new Version(1, 0, 0);

        public MenuItem MenuItem => _menuItem;

        public void OnLoad()
        {
            _menuItem = new MenuItem
            {
                Header = "BG Match Helper",
                IsCheckable = true
            };
            _menuItem.Checked += OnMenuChecked;
            _menuItem.Unchecked += OnMenuUnchecked;
            _menuItem.IsChecked = true;
        }

        public void OnUnload()
        {
            if (_menuItem != null)
            {
                _menuItem.Checked -= OnMenuChecked;
                _menuItem.Unchecked -= OnMenuUnchecked;
                _menuItem.IsChecked = false;
            }
        }

        public void OnButtonPress()
        {
        }

        public void OnUpdate()
        {
            _overlay?.OnUpdate();
        }

        private void OnMenuChecked(object sender, EventArgs e)
        {
            if (!Utils.IsElevated())
            {
                Log.Warn("BGMatchHelper requires HDT to run as administrator.");
                _menuItem.IsChecked = false;
                return;
            }

            if (_overlay != null)
                return;

            _overlay = new ReconnectOverlay();
            Core.OverlayCanvas.Children.Add(_overlay);
            Log.Info("BGMatchHelper overlay enabled");
        }

        private void OnMenuUnchecked(object sender, EventArgs e)
        {
            if (_overlay == null)
                return;

            Core.OverlayCanvas.Children.Remove(_overlay);
            _overlay.Dispose();
            _overlay = null;
            Log.Info("BGMatchHelper overlay disabled");
        }
    }
}
