using System;
using System.Windows.Controls;
using HDT_BgReplay.UI;
using Hearthstone_Deck_Tracker.Plugins;
using Core = Hearthstone_Deck_Tracker.API.Core;

namespace HDT_BgReplay
{
    public class BgReplayPlugin : IPlugin
    {
        private MenuItem _menuItem;
        private ReplayWindow _window;
        private ReplayOverlayButton _overlayButton;

        public string Name => "BgReplay";

        public string Description =>
            "BG turn replay from Power.log. Enable plugin, then use overlay button 'BG Replay' (top-left on HDT overlay) or Plugins menu.";

        public string ButtonText => "Open replay";

        public string Author => "thorx";

        public Version Version => new Version(1, 0, 2, 0);

        public MenuItem MenuItem => _menuItem;

        public void OnLoad()
        {
            _menuItem = new MenuItem
            {
                Header = "BG Log Replay",
                IsCheckable = true
            };
            _menuItem.Checked += (_, __) => EnableOverlay();
            _menuItem.Unchecked += (_, __) => DisableOverlay();
            _menuItem.IsChecked = true;
        }

        public void OnUnload()
        {
            if (_menuItem != null)
            {
                _menuItem.IsChecked = false;
            }

            DisableOverlay();
            _window?.Close();
            _window = null;
        }

        public void OnButtonPress() => OpenReplayWindow();

        public void OnUpdate() => _overlayButton?.OnUpdate();

        private void EnableOverlay()
        {
            if (_overlayButton != null)
                return;

            _overlayButton = new ReplayOverlayButton(OpenReplayWindow);
            Core.OverlayCanvas.Children.Add(_overlayButton);
            _overlayButton.OnUpdate();
        }

        private void DisableOverlay()
        {
            if (_overlayButton == null)
                return;

            Core.OverlayCanvas.Children.Remove(_overlayButton);
            _overlayButton.Dispose();
            _overlayButton = null;
        }

        private void OpenReplayWindow()
        {
            if (_window == null || !_window.IsVisible)
            {
                _window = new ReplayWindow();
                _window.Closed += (_, __) => _window = null;
                _window.Show();
                _window.Activate();
            }
            else
            {
                _window.Activate();
            }
        }
    }
}
