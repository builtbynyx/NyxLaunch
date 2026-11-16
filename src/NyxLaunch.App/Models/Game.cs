using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NyxLaunch.App.Models;

public sealed class Game : INotifyPropertyChanged
{
    private string _name = "";
    private string _executablePath = "";
    private string _installPath = "";
    private string _platform = "Manual";
    private string _appId = "";
    private string _arguments = "";
    private long _playtimeSeconds;
    private DateTime? _lastPlayed;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get => _name; set => Set(ref _name, value); }
    public string ExecutablePath { get => _executablePath; set => Set(ref _executablePath, value); }
    public string InstallPath { get => _installPath; set => Set(ref _installPath, value); }
    public string Platform { get => _platform; set { if (Set(ref _platform, value)) OnPropertyChanged(nameof(PlatformBadgeText)); } }
    public string PlatformBadgeText => string.IsNullOrWhiteSpace(Platform) ? "MANUAL" : Platform.ToUpperInvariant();
    public string AppId { get => _appId; set => Set(ref _appId, value); }
    public string Arguments { get => _arguments; set => Set(ref _arguments, value); }
    public List<string> Collections { get; set; } = [];
    public long PlaytimeSeconds { get => _playtimeSeconds; set { Set(ref _playtimeSeconds, value); OnPropertyChanged(nameof(PlaytimeText)); } }
    public DateTime? LastPlayed { get => _lastPlayed; set { Set(ref _lastPlayed, value); OnPropertyChanged(nameof(LastPlayedText)); } }
    public string PlaytimeText => PlaytimeSeconds < 60 ? "Less than a minute" : TimeSpan.FromSeconds(PlaytimeSeconds).TotalHours >= 1 ? $"{(int)TimeSpan.FromSeconds(PlaytimeSeconds).TotalHours}h {TimeSpan.FromSeconds(PlaytimeSeconds).Minutes}m" : $"{TimeSpan.FromSeconds(PlaytimeSeconds).Minutes}m";
    public string LastPlayedText => LastPlayed is null ? "Never played" : $"Last played {LastPlayed.Value.ToString("MMM d, yyyy")}";
    public string CoverImageUrl => string.IsNullOrWhiteSpace(AppId) ? "" : $"https://cdn.akamai.steamstatic.com/steam/apps/{AppId}/header.jpg";

    public event PropertyChangedEventHandler? PropertyChanged;
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? property = null)
    { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; OnPropertyChanged(property); return true; }
    private void OnPropertyChanged(string? property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
}
