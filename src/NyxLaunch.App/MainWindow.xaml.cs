using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using NyxLaunch.App.Models;
using NyxLaunch.App.Services;

namespace NyxLaunch.App;

public partial class MainWindow : Window
{
    private readonly LibraryStore _store = new();
    private readonly ObservableCollection<Game> _games = [];
    private readonly ObservableCollection<string> _collections = [];
    private List<Game> _visibleGames = [];
    private string _filter = "All";
    private string? _activeCollection;
    private Game? Selected => GamesList.SelectedItem as Game;

    public MainWindow()
    {
        InitializeComponent();
        foreach (var game in _store.Load()) _games.Add(game);
        foreach (var collection in _store.LoadCollections().Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x)) _collections.Add(collection);
        CollectionsList.ItemsSource = _collections;
        GamesList.ItemsSource = _games;
        RefreshList();
        UpdateNavigationState();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Restore_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Save() => _store.Save(_games);
    private void RefreshList()
    {
        var query = SearchBox?.Text?.Trim() ?? "";
        GamesList.Tag = _filter;
        _visibleGames = _games.Where(g => (_filter == "All" || (_filter == "Steam" && g.Platform == "Steam") || (_filter == "Recent" && g.LastPlayed != null) || (_filter == "Collection" && _activeCollection is not null && g.Collections.Contains(_activeCollection, StringComparer.OrdinalIgnoreCase))) && (query.Length == 0 || g.Name.Contains(query, StringComparison.OrdinalIgnoreCase))).OrderBy(g => g.Name).ToList();
        GamesList.ItemsSource = _visibleGames;
        GameCount.Text = $"{_visibleGames.Count} TITLES";
        EmptyState.Visibility = _visibleGames.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (Selected is not null && !_visibleGames.Contains(Selected)) GamesList.SelectedItem = null;
        UpdateDetails();
        UpdateNavigationState();
    }

    private void UpdateDetails()
    {
        var game = Selected;
        DetailName.Text = game?.Name ?? "Select a game";
        DetailPlatform.Text = game is null ? "Your game details will appear here." : $"{game.Platform}  ·  {game.LastPlayedText}";
        DetailPlaytime.Text = game?.PlaytimeText ?? "—";
        DetailLastPlayed.Text = game?.LastPlayed is null ? "Never" : game.LastPlayed.Value.ToString("MMM d, yyyy");
        var cover = game?.CoverImageUrl;
        HeroArtwork.Source = null;
        DetailArtwork.Source = null;
        if (!string.IsNullOrWhiteSpace(cover) && Uri.TryCreate(cover, UriKind.Absolute, out var coverUri))
        {
            var hero = new System.Windows.Media.Imaging.BitmapImage();
            hero.BeginInit(); hero.UriSource = coverUri; hero.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; hero.EndInit();
            HeroArtwork.Source = hero;
            var detail = new System.Windows.Media.Imaging.BitmapImage();
            detail.BeginInit(); detail.UriSource = coverUri; detail.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; detail.EndInit();
            DetailArtwork.Source = detail;
            DetailGlyph.Visibility = Visibility.Collapsed;
        }
        else { DetailGlyph.Visibility = Visibility.Visible; }
        HeroTitle.Text = game?.Name ?? "Your next session starts here.";
        HeroSubtitle.Text = game is null ? "Choose a game from your library to see its details." : $"{game.Platform}  ·  {game.PlaytimeText} played  ·  {game.LastPlayedText}";
        HeroPlayButton.IsEnabled = game is not null;
        DetailPath.Text = game is null ? "—" : (string.IsNullOrWhiteSpace(game.ExecutablePath) ? game.InstallPath : game.ExecutablePath);
        PlayButton.IsEnabled = game is not null;
        ArgumentsButton.IsEnabled = game is not null;
        RemoveButton.IsEnabled = game is not null;
        AddToCollectionButton.IsEnabled = game is not null && _collections.Count > 0;
    }

    private void AllGames_Click(object sender, RoutedEventArgs e) { _activeCollection = null; _filter = "All"; PageTitle.Text = "Your library"; PageSubtitle.Text = "All your PC games, in one place."; RefreshList(); UpdateNavigationState(); }
    private void Recent_Click(object sender, RoutedEventArgs e) { _activeCollection = null; _filter = "Recent"; PageTitle.Text = "Recently played"; PageSubtitle.Text = "Pick up where you left off."; RefreshList(); UpdateNavigationState(); }
    private void Steam_Click(object sender, RoutedEventArgs e) { _activeCollection = null; _filter = "Steam"; PageTitle.Text = "Steam library"; PageSubtitle.Text = "Games detected from your Steam libraries."; RefreshList(); UpdateNavigationState(); }

    private void UpdateNavigationState()
    {
        SetNavActive(AllGamesButton, _filter == "All");
        SetNavActive(RecentButton, _filter == "Recent");
        SetNavActive(SteamButton, _filter == "Steam");
        // ItemsControl containers are generated asynchronously. Update after layout so
        // collection buttons receive the same persistent active state as workspace buttons.
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
        {
            for (var i = 0; i < CollectionsList.Items.Count; i++)
            {
                if (CollectionsList.ItemContainerGenerator.ContainerFromIndex(i) is DependencyObject container)
                {
                    var button = FindVisualChild<Button>(container);
                    if (button is not null)
                        SetNavActive(button, _filter == "Collection" && string.Equals(button.Tag as string, _activeCollection, StringComparison.OrdinalIgnoreCase));
                }
            }
        }));
    }

    private void CollectionNavButton_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
            SetNavActive(button, _filter == "Collection" && string.Equals(button.Tag as string, _activeCollection, StringComparison.OrdinalIgnoreCase));
    }

    private static void SetNavActive(Button button, bool active)
    {
        button.Background = active ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 29, 50)) : System.Windows.Media.Brushes.Transparent;
        button.Foreground = active ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(215, 206, 255)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(168, 166, 184));
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T match) return match;
            var nested = FindVisualChild<T>(child);
            if (nested is not null) return nested;
        }
        return null;
    }

    private void Collection_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string name }) return;
        _activeCollection = name; _filter = "Collection";
        PageTitle.Text = name; PageSubtitle.Text = "Your games in this collection.";
        RefreshList();
        UpdateNavigationState();
    }

    private void CreateCollection_Click(object sender, RoutedEventArgs e)
    {
        var name = PromptForText("New collection", "Give your collection a name:", "My collection");
        if (string.IsNullOrWhiteSpace(name)) return;
        name = name.Trim();
        if (_collections.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)))
        { MessageBox.Show("A collection with that name already exists.", "NyxLaunch", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        _collections.Add(name); _store.SaveCollections(_collections);
        _activeCollection = name; _filter = "Collection";
        PageTitle.Text = name; PageSubtitle.Text = "Your games in this collection.";
        RefreshList();
        this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(UpdateNavigationState));
    }

    private void AddToCollection_Click(object sender, RoutedEventArgs e)
    {
        var game = Selected; if (game is null) return;
        if (_collections.Count == 0) { CreateCollection_Click(sender, e); return; }
        var dialog = CreateNyxDialog("ADD TO COLLECTION", 460, 250);
        var panel = new StackPanel { Margin = new Thickness(24, 20, 24, 22) };
        panel.Children.Add(new TextBlock { Text = "Add to collection", FontSize = 19, FontWeight = FontWeights.SemiBold, Foreground = Brush("#F2F0FA"), Margin = new Thickness(0,0,0,6) });
        panel.Children.Add(new TextBlock { Text = game.Name, FontSize = 11, Foreground = Brush("#9693A8"), TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0,0,0,18) });
        panel.Children.Add(new TextBlock { Text = "SELECT COLLECTION", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = Brush("#9693A8"), Margin = new Thickness(0,0,0,7) });
        var combo = new ComboBox { ItemsSource = _collections, SelectedIndex = 0, Height = 36, Padding = new Thickness(9,7,9,7), Background = Brush("#15141F"), Foreground = Brush("#F2F0FA"), BorderBrush = Brush("#38344D"), Margin = new Thickness(0,0,0,22) };
        panel.Children.Add(combo);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = NyxDialogButton("Cancel", false); cancel.Width = 92; cancel.Margin = new Thickness(0,0,9,0);
        cancel.Click += (_, _) => dialog.DialogResult = false;
        var add = NyxDialogButton("Add to collection", true); add.Width = 142; add.IsDefault = true;
        add.Click += (_, _) => dialog.DialogResult = true;
        buttons.Children.Add(cancel); buttons.Children.Add(add); panel.Children.Add(buttons); SetNyxDialogBody(dialog, panel);
        if (dialog.ShowDialog() != true || combo.SelectedItem is not string selected) return;
        if (!game.Collections.Contains(selected, StringComparer.OrdinalIgnoreCase)) game.Collections.Add(selected);
        Save();
        if (_filter == "Collection") RefreshList();
        MessageBox.Show($"{game.Name} added to {selected}.", "NyxLaunch", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private string? PromptForText(string title, string prompt, string initialValue)
    {
        var dialog = CreateNyxDialog(title.ToUpperInvariant(), 460, 235);
        var panel = new StackPanel { Margin = new Thickness(24, 20, 24, 22) };
        panel.Children.Add(new TextBlock { Text = prompt, FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = Brush("#F2F0FA"), Margin = new Thickness(0,0,0,16) });
        var input = new TextBox { Text = initialValue, Height = 38, Padding = new Thickness(11,8,11,8), Background = Brush("#15141F"), Foreground = Brush("#F2F0FA"), BorderBrush = Brush("#38344D"), CaretBrush = Brush("#A99BFF"), Margin = new Thickness(0,0,0,22), VerticalContentAlignment = VerticalAlignment.Center };
        panel.Children.Add(input);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = NyxDialogButton("Cancel", false); cancel.Width = 92; cancel.Margin = new Thickness(0,0,9,0);
        cancel.Click += (_, _) => dialog.DialogResult = false;
        var create = NyxDialogButton("Create collection", true); create.Width = 142; create.IsDefault = true;
        create.Click += (_, _) => dialog.DialogResult = true;
        buttons.Children.Add(cancel); buttons.Children.Add(create); panel.Children.Add(buttons); SetNyxDialogBody(dialog, panel);
        input.SelectAll(); input.Focus();
        return dialog.ShowDialog() == true ? input.Text : null;
    }

    private Window CreateNyxDialog(string caption, double width, double height)
    {
        var dialog = new Window { Title = "NyxLaunch", Owner = this, Width = width, Height = height, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize, WindowStyle = WindowStyle.None, Background = Brush("#090A0F"), Foreground = Brush("#F2F3F8"), FontFamily = new System.Windows.Media.FontFamily("Segoe UI") };
        System.Windows.Shell.WindowChrome.SetWindowChrome(dialog, new System.Windows.Shell.WindowChrome { CaptionHeight = 40, ResizeBorderThickness = new Thickness(0), CornerRadius = new CornerRadius(9), GlassFrameThickness = new Thickness(0), UseAeroCaptionButtons = false });
        var root = new DockPanel { Background = Brush("#090A0F") };
        var bar = new Grid { Height = 40, Background = Brush("#11111A") };
        bar.Children.Add(new TextBlock { Text = "NYXLAUNCH  /  " + caption, FontSize = 10, FontWeight = FontWeights.Bold, Foreground = Brush("#C8C0F2"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16,0,0,0), IsHitTestVisible = false });
        var close = new Button { Content = "×", Width = 44, HorizontalAlignment = HorizontalAlignment.Right, Background = System.Windows.Media.Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = Brush("#AAA6B8"), FontSize = 19, Padding = new Thickness(0) };
        close.Click += (_, _) => dialog.DialogResult = false;
        System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(close, true);
        bar.Children.Add(close); DockPanel.SetDock(bar, Dock.Top); root.Children.Add(bar);
        dialog.Content = root;
        return dialog;
    }

    private static void SetNyxDialogBody(Window dialog, UIElement body)
    {
        if (dialog.Content is DockPanel root) root.Children.Add(body);
    }

    private static Button NyxDialogButton(string text, bool primary) => new()
    {
        Content = text, Height = 36, Padding = new Thickness(13,7,13,7), FontSize = 11, FontWeight = FontWeights.SemiBold,
        Foreground = Brush("#F5F3FF"), Background = primary ? Brush("#6555B4") : Brush("#191825"),
        BorderBrush = primary ? Brush("#8272D8") : Brush("#302D43"), BorderThickness = new Thickness(1), Cursor = System.Windows.Input.Cursors.Hand
    };

    private static System.Windows.Media.SolidColorBrush Brush(string color) => new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (SearchPlaceholder is not null)
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        RefreshList();
    }
    private void GamesList_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateDetails();

    private void ScanSteam_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var found = SteamScanner.Scan();
            var added = 0;
            foreach (var game in found)
            {
                var existing = _games.FirstOrDefault(g => g.Platform == "Steam" && g.AppId == game.AppId);
                if (existing is null) { _games.Add(game); added++; }
                else { existing.Name = game.Name; existing.InstallPath = game.InstallPath; }
            }
            Save(); _filter = "All"; RefreshList();
            MessageBox.Show(added == 0 ? $"Steam scan complete. No new games found. {found.Count} installed games detected." : $"Steam scan complete. Added {added} game(s).", "NyxLaunch", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { MessageBox.Show($"Steam scan failed: {ex.Message}", "NyxLaunch", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void AddGame_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Select a game's executable", Filter = "Applications (*.exe)|*.exe", CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        var path = dialog.FileName;
        var existing = _games.FirstOrDefault(g => string.Equals(g.ExecutablePath, path, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) { GamesList.SelectedItem = existing; return; }
        var game = new Game { Name = Path.GetFileNameWithoutExtension(path), ExecutablePath = path, InstallPath = Path.GetDirectoryName(path) ?? "", Platform = "Manual" };
        _games.Add(game); Save(); _filter = "All"; RefreshList(); GamesList.SelectedItem = game;
    }

    private async void Play_Click(object sender, RoutedEventArgs e)
    {
        var game = Selected; if (game is null) return;
        try
        {
            if (game.Platform == "Steam" && !string.IsNullOrWhiteSpace(game.AppId))
            {
                Process.Start(new ProcessStartInfo($"steam://run/{game.AppId}") { UseShellExecute = true });
                game.LastPlayed = DateTime.Now; Save(); UpdateDetails();
                MessageBox.Show("Steam was asked to launch this game. Playtime tracking for Steam titles will be added in a future release.", "NyxLaunch", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!File.Exists(game.ExecutablePath)) { MessageBox.Show("The executable could not be found. Remove and re-add this game using its current .exe location.", "Game not found", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var info = new ProcessStartInfo(game.ExecutablePath) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(game.ExecutablePath) ?? "" };
            if (!string.IsNullOrWhiteSpace(game.Arguments)) info.Arguments = game.Arguments;
            var process = Process.Start(info);
            if (process is null) return;
            game.LastPlayed = DateTime.Now; Save(); UpdateDetails();
            _ = Task.Run(async () =>
            {
                try { await process.WaitForExitAsync(); var elapsed = (long)(DateTime.Now - game.LastPlayed!.Value).TotalSeconds; await Dispatcher.InvokeAsync(() => { game.PlaytimeSeconds += Math.Max(0, elapsed); Save(); UpdateDetails(); }); }
                catch { }
            });
        }
        catch (Exception ex) { MessageBox.Show($"Could not launch {game.Name}: {ex.Message}", "Launch failed", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void Arguments_Click(object sender, RoutedEventArgs e)
    {
        var game = Selected; if (game is null) return;
        var dialog = new ArgumentWindow(game.Name, game.Arguments) { Owner = this };
        if (dialog.ShowDialog() == true) { game.Arguments = dialog.Arguments; Save(); }
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        var game = Selected; if (game is null) return;
        if (MessageBox.Show($"Remove {game.Name} from your NyxLaunch library? This will not uninstall the game.", "Remove game", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _games.Remove(game); Save(); RefreshList();
    }
}
