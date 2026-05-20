using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HDT_Reconnector.GameLog;
using HDT_Reconnector.Network;
using HDT_Reconnector.Overlay;
using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Enums.Hearthstone;
using Hearthstone_Deck_Tracker.Utility.Logging;
using Core = Hearthstone_Deck_Tracker.API.Core;

namespace HDT_Reconnector
{
    public class ReconnectOverlay : UserControl, IDisposable
    {
        private const string LogSearchPattern = "Hearthstone.log";
        private const int DisconnectTimeoutSeconds = 20;
        private const double EdgeMargin = 16;
        private const double OffsetLeft = 100;
        private const double OffsetTop = 100;

        private readonly ConnectionBreaker _breaker = new ConnectionBreaker();
        private readonly Border _reconnectButton;
        private readonly TextBlock _reconnectText;
        private readonly Brush _originalBrush;
        private readonly Timer _disconnectTimer;

        private LogWatcher _logWatcher;
        private bool _logWatcherInitialized;
        private DateTime _lastGameStartTime;
        private readonly SizeChangedEventHandler _overlaySizeChangedHandler;

        public string RemoteAddr { get; set; }
        public ushort RemotePort { get; set; }

        public ReconnectOverlay()
        {
            Width = 130;
            Height = 36;

            _reconnectText = new TextBlock
            {
                Text = "reconnect",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _reconnectButton = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x14, 0x16, 0x17)),
                BorderThickness = new Thickness(2),
                Background = new SolidColorBrush(Color.FromRgb(0x23, 0x27, 0x2a)),
                Cursor = Cursors.Hand,
                Child = _reconnectText
            };

            _originalBrush = _reconnectButton.Background;
            _reconnectButton.MouseEnter += ReconnectButton_MouseEnter;
            _reconnectButton.MouseLeave += ReconnectButton_MouseLeave;
            _reconnectButton.MouseLeftButtonDown += ReconnectButton_Click;

            Content = _reconnectButton;
            Visibility = Visibility.Collapsed;

            _disconnectTimer = new Timer(DisconnectedTimeout);
            OverlayRegistration.RegisterClickable(_reconnectButton);

            _overlaySizeChangedHandler = (_, __) => UpdatePosition();
            Core.OverlayCanvas.SizeChanged += _overlaySizeChangedHandler;
            UpdatePosition();
        }

        public void Dispose()
        {
            _disconnectTimer.Dispose();
            _logWatcher?.Stop();
            Core.OverlayCanvas.SizeChanged -= _overlaySizeChangedHandler;
            OverlayRegistration.UnregisterClickable(_reconnectButton);
        }

        private void UpdatePosition()
        {
            var canvas = Core.OverlayCanvas;
            var width = canvas.ActualWidth > 0 ? canvas.ActualWidth : canvas.Width;
            var height = canvas.ActualHeight > 0 ? canvas.ActualHeight : canvas.Height;
            if (width <= 0 || height <= 0)
                return;

            Canvas.SetLeft(this, width - Width - EdgeMargin - OffsetLeft);
            Canvas.SetTop(this, height - Height - EdgeMargin - OffsetTop);
        }

        public void OnUpdate()
        {
            var game = Core.Game;
            var showInBgMatch = game != null
                                && game.IsBattlegroundsMatch
                                && game.IsRunning
                                && !game.IsInMenu;

            Visibility = showInBgMatch ? Visibility.Visible : Visibility.Collapsed;
            if (showInBgMatch)
                UpdatePosition();

            if (_breaker.Status == ConnectionBreaker.ConnectionStatus.Disconnected)
            {
                if (IsInMainOrBgMenu())
                {
                    Log.Info("Can't reconnect to the game");
                    _breaker.MarkConnected();
                    SetButtonText("reconnect");
                    return;
                }

                if (IsGameRestarted() || IsGameEnded())
                {
                    SetButtonText("reconnect");
                    _breaker.MarkConnected();
                    _disconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }

            if (game != null && game.IsRunning && !_logWatcherInitialized)
            {
                try
                {
                    var logPath = FindLatestLogFile();
                    _logWatcher = new LogWatcher(this, logPath);
                    _logWatcher.Start();
                    _logWatcherInitialized = true;
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to start log watcher: " + ex);
                }
            }

            if (game != null && !game.IsRunning && _logWatcherInitialized)
            {
                _logWatcher?.Stop();
                _logWatcher = null;
                _logWatcherInitialized = false;
            }
        }

        private void ReconnectButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (!CanReconnect())
                return;

            _disconnectTimer.Change(DisconnectTimeoutSeconds * 1000, Timeout.Infinite);
            _lastGameStartTime = Core.Game.CurrentGameStats?.StartTime ?? DateTime.MinValue;

            lock (this)
            {
                if (_breaker.Disconnect(RemoteAddr, RemotePort) == 0)
                    SetButtonText("disconnected");
                else
                    _disconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        private void ReconnectButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (CanReconnect())
                _reconnectButton.Background = new SolidColorBrush(Color.FromRgb(0x2E, 0x34, 0x38));
        }

        private void ReconnectButton_MouseLeave(object sender, MouseEventArgs e)
        {
            _reconnectButton.Background = _originalBrush;
        }

        private bool CanReconnect()
        {
            var game = Core.Game;
            return _breaker.Status == ConnectionBreaker.ConnectionStatus.Connected
                   && game != null
                   && game.IsBattlegroundsMatch
                   && game.IsRunning
                   && !game.IsInMenu
                   && !IsGameEnded();
        }

        private bool IsGameRestarted()
        {
            var stats = Core.Game?.CurrentGameStats;
            return Core.Game != null
                   && Core.Game.CurrentGameMode != GameMode.None
                   && stats != null
                   && stats.StartTime > _lastGameStartTime;
        }

        private static bool IsInMainOrBgMenu()
        {
            var mode = Core.Game?.CurrentMode;
            return mode == Mode.HUB || mode == Mode.BACON;
        }

        private static bool IsGameEnded()
        {
            var stats = Core.Game?.CurrentGameStats;
            return stats != null && stats.EndTime > stats.StartTime;
        }

        private void DisconnectedTimeout(object state)
        {
            Dispatcher.Invoke(() => SetButtonText("reconnect"));
            _breaker.MarkConnected();
        }

        private void SetButtonText(string text)
        {
            if (Dispatcher.CheckAccess())
                _reconnectText.Text = text;
            else
                Dispatcher.Invoke(() => _reconnectText.Text = text);
        }

        private static string FindLatestLogFile()
        {
            var logDirectory = Path.Combine(
                Config.Instance.HearthstoneDirectory,
                Config.Instance.HearthstoneLogsDirectoryName);

            var latestDir = new DirectoryInfo(logDirectory)
                .GetDirectories()
                .OrderByDescending(x => x.CreationTime)
                .First();

            var logFile = latestDir.GetFiles(LogSearchPattern).First();
            Log.Info("Using log file: " + logFile.FullName);
            return logFile.FullName;
        }
    }
}
