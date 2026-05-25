using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BgPowerLog;
using BgPowerLog.Models;
using BgPowerLog.Parsing;

namespace HDT_BgReplay.UI
{
    public sealed class ReplayWindow : Window
    {
        private readonly TextBlock _statusText;
        private readonly TextBlock _turnLabel;
        private readonly Slider _turnSlider;
        private readonly StackPanel _friendlyHeroRow;
        private readonly WrapPanel _friendlyMinions;
        private readonly StackPanel _opponentHeroRow;
        private readonly WrapPanel _opponentMinions;
        private readonly TextBox _debugBox;
        private readonly ComboBox _logCombo;
        private readonly ComboBox _matchCombo;
        private readonly System.Windows.Controls.TextBox _logRootBox;
        private readonly Border _setupBanner;

        private ReplayParseResult _result;
        private int _turnIndex;

        public ReplayWindow()
        {
            Title = "BG Replay — Power.log";
            Width = 960;
            Height = 680;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var root = new DockPanel { Margin = new Thickness(10) };

            _setupBanner = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 248, 220)),
                BorderBrush = Brushes.Goldenrod,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 8),
                Child = BuildSetupBannerContent()
            };
            DockPanel.SetDock(_setupBanner, Dock.Top);
            root.Children.Add(_setupBanner);

            var top = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            DockPanel.SetDock(top, Dock.Top);

            var pickHsBtn = new Button
            {
                Content = "Указать, где установлен Hearthstone…",
                Width = 280,
                Margin = new Thickness(0, 0, 8, 0),
                FontWeight = FontWeights.SemiBold
            };
            pickHsBtn.Click += (_, __) => BrowseHearthstoneInstall();
            top.Children.Add(pickHsBtn);

            var parseDaysBtn = new Button
            {
                Content = "Парсить игры за 14 дней",
                Width = 170,
                Margin = new Thickness(0, 0, 8, 0)
            };
            parseDaysBtn.Click += (_, __) => ParseRecentDays(14);
            top.Children.Add(parseDaysBtn);

            var parseBtn = new Button { Content = "Parse Power.log", Width = 130, Margin = new Thickness(0, 0, 8, 0) };
            parseBtn.Click += (_, __) => ParseLog(fullFile: false);
            top.Children.Add(parseBtn);

            var parseFullBtn = new Button { Content = "Parse full file", Width = 110, Margin = new Thickness(0, 0, 8, 0) };
            parseFullBtn.Click += (_, __) => ParseLog(fullFile: true);
            top.Children.Add(parseFullBtn);

            var refreshLogsBtn = new Button { Content = "Refresh logs", Width = 100, Margin = new Thickness(0, 0, 8, 0) };
            refreshLogsBtn.Click += (_, __) => RefreshLogList();
            top.Children.Add(refreshLogsBtn);

            root.Children.Add(top);

            var folderRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            DockPanel.SetDock(folderRow, Dock.Top);
            folderRow.Children.Add(new TextBlock { Text = "Logs folder:", Width = 80, VerticalAlignment = VerticalAlignment.Center });
            _logRootBox = new System.Windows.Controls.TextBox
            {
                Width = 420,
                Margin = new Thickness(6, 0, 6, 0),
                Text = ReplayLogSettings.CustomLogRoot ?? ""
            };
            folderRow.Children.Add(_logRootBox);
            var browseBtn = new Button { Content = "Browse…", Width = 80, Margin = new Thickness(0, 0, 6, 0) };
            browseBtn.Click += (_, __) => BrowseLogFolder();
            folderRow.Children.Add(browseBtn);
            var saveFolderBtn = new Button { Content = "Apply folder", Width = 90 };
            saveFolderBtn.Click += (_, __) => ApplyLogFolder();
            folderRow.Children.Add(saveFolderBtn);
            root.Children.Add(folderRow);

            var logRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            DockPanel.SetDock(logRow, Dock.Top);
            logRow.Children.Add(new TextBlock { Text = "Log file:", Width = 60, VerticalAlignment = VerticalAlignment.Center });
            _logCombo = new ComboBox { Width = 700, Margin = new Thickness(6, 0, 0, 0) };
            logRow.Children.Add(_logCombo);
            root.Children.Add(logRow);

            var matchRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            DockPanel.SetDock(matchRow, Dock.Top);
            matchRow.Children.Add(new TextBlock { Text = "Match:", Width = 60, VerticalAlignment = VerticalAlignment.Center });
            _matchCombo = new ComboBox { Width = 700, Margin = new Thickness(6, 0, 0, 0) };
            _matchCombo.SelectionChanged += (_, __) => SelectMatch();
            matchRow.Children.Add(_matchCombo);
            root.Children.Add(matchRow);

            _statusText = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = Brushes.DimGray
            };
            DockPanel.SetDock(_statusText, Dock.Top);
            root.Children.Add(_statusText);

            var turnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            DockPanel.SetDock(turnRow, Dock.Top);
            _turnLabel = new TextBlock { Width = 120, VerticalAlignment = VerticalAlignment.Center };
            turnRow.Children.Add(_turnLabel);
            _turnSlider = new Slider { Minimum = 0, Maximum = 0, Width = 400, Margin = new Thickness(8, 0, 0, 0) };
            _turnSlider.ValueChanged += (_, __) =>
            {
                if (_result?.Match?.Turns == null || _result.Match.Turns.Count == 0)
                    return;
                _turnIndex = (int)_turnSlider.Value;
                RenderTurn();
            };
            turnRow.Children.Add(_turnSlider);
            root.Children.Add(turnRow);

            var boards = new Grid();
            boards.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            boards.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            DockPanel.SetDock(boards, Dock.Top);

            var friendlyBox = CreateBoardBox("Your board (friendly)", out _friendlyHeroRow, out _friendlyMinions);
            Grid.SetColumn(friendlyBox, 0);
            boards.Children.Add(friendlyBox);

            var opponentBox = CreateBoardBox("Opponent (combat / dummy)", out _opponentHeroRow, out _opponentMinions);
            Grid.SetColumn(opponentBox, 1);
            boards.Children.Add(opponentBox);

            root.Children.Add(boards);

            _debugBox = new TextBox
            {
                Margin = new Thickness(0, 10, 0, 0),
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                IsReadOnly = true
            };
            root.Children.Add(_debugBox);

            Content = root;
            TryAutoDetectOnFirstOpen();
            RefreshLogList();
            ApplyLogFolder(silent: true);
        }

        private UIElement BuildSetupBannerContent()
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = "Логи не найдены автоматически. Укажите папку, где установлен Hearthstone (там должен быть Hearthstone.exe).",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 6)
            });
            var btn = new Button
            {
                Content = "Выбрать папку установки Hearthstone…",
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(12, 6, 12, 6)
            };
            btn.Click += (_, __) => BrowseHearthstoneInstall();
            panel.Children.Add(btn);
            return panel;
        }

        private void UpdateSetupBannerVisibility()
        {
            var root = _logRootBox.Text?.Trim();
            var hasLogs = !string.IsNullOrEmpty(root) && PowerLogPaths.DiscoverLogFiles(root).Count > 0;
            _setupBanner.Visibility = hasLogs ? Visibility.Collapsed : Visibility.Visible;
        }

        private void TryAutoDetectOnFirstOpen()
        {
            if (!string.IsNullOrWhiteSpace(ReplayLogSettings.CustomLogRoot) &&
                Directory.Exists(ReplayLogSettings.CustomLogRoot))
                return;

            var install = ReplayLogSettings.InstallPath ?? HearthstoneInstallLocator.TryAutoDetectInstallDirectory();
            if (string.IsNullOrEmpty(install))
            {
                UpdateSetupBannerVisibility();
                return;
            }

            if (!HearthstoneInstallLocator.TryResolveLogsDirectory(install, out var logs, out _))
                return;

            _logRootBox.Text = logs;
            ReplayLogSettings.InstallPath = install;
            ReplayLogSettings.CustomLogRoot = logs;
            UpdateStatus("Авто: " + logs);
            UpdateSetupBannerVisibility();
        }

        private void BrowseHearthstoneInstall()
        {
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Папка установки Hearthstone (где лежит Hearthstone.exe) или папка Logs",
                ShowNewFolderButton = false
            })
            {
                var start = ReplayLogSettings.InstallPath
                            ?? HearthstoneInstallLocator.TryAutoDetectInstallDirectory()
                            ?? @"C:\Program Files (x86)";
                if (Directory.Exists(start))
                    dlg.SelectedPath = start;

                if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                ApplySelectedHearthstonePath(dlg.SelectedPath);
            }
        }

        private void ApplySelectedHearthstonePath(string selectedPath)
        {
            if (!HearthstoneInstallLocator.TryResolveLogsDirectory(selectedPath, out var logsDir, out var message))
            {
                UpdateStatus(message);
                return;
            }

            string installRoot = null;
            if (File.Exists(Path.Combine(selectedPath, "Hearthstone.exe")))
                installRoot = selectedPath;
            else if (string.Equals(Path.GetFileName(logsDir), "Logs", StringComparison.OrdinalIgnoreCase))
                installRoot = Path.GetDirectoryName(logsDir);

            if (!string.IsNullOrEmpty(installRoot))
                ReplayLogSettings.InstallPath = installRoot;
            ReplayLogSettings.CustomLogRoot = logsDir;
            _logRootBox.Text = logsDir;
            RefreshLogList();
            UpdateSetupBannerVisibility();
            UpdateStatus(message + " → " + logsDir);
        }

        private void BrowseLogFolder()
        {
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Папка Logs Hearthstone (…\\Hearthstone\\Logs с подпапками Hearthstone_YYYY…)",
                ShowNewFolderButton = false
            })
            {
                if (Directory.Exists(_logRootBox.Text))
                    dlg.SelectedPath = _logRootBox.Text;

                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    _logRootBox.Text = dlg.SelectedPath;
            }
        }

        private void ApplyLogFolder(bool silent = false)
        {
            var root = _logRootBox.Text?.Trim();
            if (!string.IsNullOrEmpty(root) && !Directory.Exists(root))
            {
                UpdateStatus("Папка не существует: " + root);
                UpdateSetupBannerVisibility();
                return;
            }

            ReplayLogSettings.CustomLogRoot = root;
            RefreshLogList();
            UpdateSetupBannerVisibility();

            if (!silent)
                UpdateStatus(string.IsNullOrEmpty(root)
                    ? "Путь сброшен."
                    : "Сканируем: " + root);
        }

        private void RefreshLogList()
        {
            _logCombo.Items.Clear();
            var root = _logRootBox.Text?.Trim();
            var logs = PowerLogPaths.DiscoverLogFiles(root);
            if (logs.Count == 0)
            {
                _logCombo.Items.Add("(нет логов — нажмите «Указать, где установлен Hearthstone…»)");
                _logCombo.SelectedIndex = 0;
                UpdateStatus("Нет Power.log / Power_old.log. Обычно: C:\\Program Files (x86)\\Hearthstone\\Logs\\Hearthstone_…\\");
                UpdateSetupBannerVisibility();
                return;
            }

            foreach (var log in logs)
                _logCombo.Items.Add($"{log.DisplayLabel} — {log.Path}");

            _logCombo.SelectedIndex = 0;
            var powerCount = logs.Count(x => x.Name.StartsWith("Power", StringComparison.OrdinalIgnoreCase));
            UpdateStatus($"Найдено {logs.Count} файл(ов) ({powerCount} Power*). Или «Парсить игры за 14 дней».");
            UpdateSetupBannerVisibility();
        }

        private void ParseRecentDays(int daysBack)
        {
            try
            {
                var root = _logRootBox.Text?.Trim();
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                {
                    UpdateStatus("Сначала укажите папку Logs (кнопка Hearthstone…).");
                    BrowseHearthstoneInstall();
                    root = _logRootBox.Text?.Trim();
                    if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                        return;
                }

                UpdateStatus($"Парсим сессии за {daysBack} дн. (может занять минуту)…");
                _result = BgReplayBuilder.BuildFromRecentSessions(root, daysBack);

                var sb = new StringBuilder();
                sb.AppendLine($"Logs root: {root}");
                sb.AppendLine($"Days: {daysBack}");
                sb.AppendLine($"Success: {_result.Success}");
                sb.AppendLine($"Matches total: {_result.MatchCount}");
                sb.AppendLine($"Lines read: {_result.LinesRead}");
                if (!string.IsNullOrEmpty(_result.Error))
                    sb.AppendLine($"Error: {_result.Error}");

                _debugBox.Text = sb.ToString();
                PopulateMatchCombo();

                if (_result.Match?.Turns == null || _result.Match.Turns.Count == 0)
                {
                    _turnSlider.Maximum = 0;
                    ClearBoards();
                    UpdateStatus(_result.Error ?? "Игры не найдены. Выберите другой Match или один файл → Parse full file.");
                    return;
                }

                _turnIndex = 0;
                _turnSlider.Maximum = _result.Match.Turns.Count - 1;
                _turnSlider.Value = 0;
                UpdateStatus($"Всего игр: {_result.MatchCount}. Текущая: #{_result.Match.Index} ({_result.Match.SessionLabel}), {_result.Match.Turns.Count} ходов.");
                RenderTurn();
            }
            catch (Exception ex)
            {
                UpdateStatus("Ошибка: " + ex.Message);
            }
        }

        private string GetSelectedLogPath()
        {
            if (_logCombo.SelectedItem == null)
                return null;

            var text = _logCombo.SelectedItem.ToString();
            var idx = text.LastIndexOf(" — ", StringComparison.Ordinal);
            return idx >= 0 ? text.Substring(idx + 3).Trim() : null;
        }

        private void PopulateMatchCombo()
        {
            _matchCombo.Items.Clear();
            if (_result?.Matches == null || _result.Matches.Count == 0)
            {
                _matchCombo.Items.Add("(no matches)");
                _matchCombo.SelectedIndex = 0;
                return;
            }

            foreach (var m in _result.Matches)
            {
                var session = string.IsNullOrEmpty(m.SessionLabel) ? "" : $" [{m.SessionLabel}]";
                _matchCombo.Items.Add(
                    $"#{m.Index}{session}: {m.Turns.Count} turns, BG={m.IsBattlegrounds}, friendly={m.FriendlyPlayerId}");
            }

            _matchCombo.SelectedIndex = _result.Matches.Count - 1;
        }

        private void SelectMatch()
        {
            if (_result?.Matches == null || _matchCombo.SelectedIndex < 0 || _matchCombo.SelectedIndex >= _result.Matches.Count)
                return;

            _result.Match = _result.Matches[_matchCombo.SelectedIndex];
            _turnIndex = 0;
            if (_result.Match.Turns.Count == 0)
            {
                ClearBoards();
                return;
            }

            _turnSlider.Maximum = _result.Match.Turns.Count - 1;
            _turnSlider.Value = 0;
            RenderTurn();
        }

        private static Border CreateBoardBox(string title, out StackPanel heroRow, out WrapPanel minionRow)
        {
            var panel = new StackPanel { Margin = new Thickness(4) };
            var header = new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 6)
            };
            panel.Children.Add(header);

            heroRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            panel.Children.Add(heroRow);

            minionRow = new WrapPanel();
            panel.Children.Add(minionRow);

            return new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8),
                Child = panel,
                MinHeight = 280
            };
        }

        private void ParseLog(bool fullFile)
        {
            try
            {
                var path = GetSelectedLogPath();
                if (string.IsNullOrEmpty(path) || path.StartsWith("("))
                {
                    UpdateStatus("Файл не выбран. Укажите Hearthstone или выберите лог в списке.");
                    return;
                }

                _result = fullFile
                    ? BgReplayBuilder.BuildFromPath(path, tailOnly: false)
                    : BgReplayBuilder.BuildFromPath(path, tailOnly: true, tailLines: 50000);

                var sb = new StringBuilder();
                sb.AppendLine($"Path: {path}");
                sb.AppendLine($"Success: {_result.Success}");
                sb.AppendLine($"BG in log: {_result.IsBattlegrounds}");
                sb.AppendLine($"Lines: {_result.LinesRead}, GameState: {_result.GameStateLines}");
                sb.AppendLine($"Matches in file: {_result.MatchCount}");
                sb.AppendLine($"Selected match turns: {_result.Match?.Turns?.Count ?? 0}");
                if (!string.IsNullOrEmpty(_result.Error))
                    sb.AppendLine($"Error: {_result.Error}");

                _debugBox.Text = sb.ToString();
                PopulateMatchCombo();

                if (_result.Match?.Turns == null || _result.Match.Turns.Count == 0)
                {
                    _turnSlider.Maximum = 0;
                    _turnSlider.Value = 0;
                    UpdateStatus(_result.MatchCount > 1
                        ? $"Найдено {_result.MatchCount} игр, у последней нет ходов — выберите другой Match."
                        : _result.Error ?? "Нет ходов. Попробуйте Parse full file.");
                    ClearBoards();
                    return;
                }

                _turnIndex = 0;
                _turnSlider.Maximum = _result.Match.Turns.Count - 1;
                _turnSlider.Value = 0;
                UpdateStatus($"Игра #{_result.Match.Index}: {_result.Match.Turns.Count} ходов.");
                RenderTurn();
            }
            catch (Exception ex)
            {
                UpdateStatus("Ошибка: " + ex.Message);
            }
        }

        private void RenderTurn()
        {
            var turns = _result.Match.Turns;
            if (_turnIndex < 0 || _turnIndex >= turns.Count)
                return;

            var turn = turns[_turnIndex];
            _turnLabel.Text = $"Turn {turn.TurnNumber} ({turn.Phase})";

            RenderBoard(_friendlyHeroRow, _friendlyMinions, turn.Friendly, Brushes.DarkGreen);
            RenderBoard(_opponentHeroRow, _opponentMinions, turn.Opponent, Brushes.DarkRed);
        }

        private static void RenderBoard(StackPanel heroRow, WrapPanel minionRow, ReplayBoard board, Brush accent)
        {
            heroRow.Children.Clear();
            minionRow.Children.Clear();

            if (board?.Hero != null && !string.IsNullOrEmpty(board.Hero.CardId))
                heroRow.Children.Add(BoardCardView.CreateHero(board.Hero, accent));
            else
                heroRow.Children.Add(new TextBlock { Text = "(no hero)", Foreground = Brushes.Gray });

            if (board?.Minions == null || board.Minions.Count == 0)
            {
                minionRow.Children.Add(new TextBlock
                {
                    Text = "(no minions on board)",
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(4)
                });
                return;
            }

            foreach (var m in board.Minions.OrderBy(x => x.ZonePosition))
                minionRow.Children.Add(BoardCardView.CreateMinion(m));
        }

        private void ClearBoards()
        {
            RenderBoard(_friendlyHeroRow, _friendlyMinions, new ReplayBoard(), Brushes.Gray);
            RenderBoard(_opponentHeroRow, _opponentMinions, new ReplayBoard(), Brushes.Gray);
        }

        private void UpdateStatus(string text) => _statusText.Text = text;
    }
}
