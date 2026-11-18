using System.IO;
using System.Text.RegularExpressions;
using NyxLaunch.App.Models;

namespace NyxLaunch.App.Services;

public static class SteamScanner
{
    public static List<Game> Scan()
    {
        var games = new List<Game>();
        var steam = FindSteamRoot();
        if (steam is null) return games;
        var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { steam };
        var vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
        if (File.Exists(vdf))
        {
            var content = File.ReadAllText(vdf);
            foreach (Match m in Regex.Matches(content, "\\\"path\\\"\\s*\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase))
                libraries.Add(m.Groups[1].Value.Replace("\\\\", "\\"));
            foreach (Match m in Regex.Matches(content, "\\\"\\d+\\\"\\s*\\\"([^\\\"]+steamapps)\\\"", RegexOptions.IgnoreCase))
                libraries.Add(Directory.GetParent(m.Groups[1].Value)?.FullName ?? m.Groups[1].Value);
        }
        foreach (var library in libraries)
        {
            var apps = Path.Combine(library, "steamapps");
            if (!Directory.Exists(apps)) continue;
            foreach (var manifest in Directory.EnumerateFiles(apps, "appmanifest_*.acf"))
            {
                try
                {
                    var text = File.ReadAllText(manifest);
                    string Read(string key) => Regex.Match(text, $"\\\"{key}\\\"\\s*\\\"([^\\\"]*)\\\"", RegexOptions.IgnoreCase).Groups[1].Value;
                    var id = Read("appid"); var name = Read("name"); var folder = Read("installdir");
                    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name)) continue;
                    var install = Path.Combine(apps, "common", folder);
                    if (games.Any(g => g.AppId == id)) continue;
                    games.Add(new Game { Name = name, AppId = id, Platform = "Steam", InstallPath = install, ExecutablePath = "" });
                }
                catch { /* Ignore malformed manifests and continue scanning. */ }
            }
        }
        return games.OrderBy(g => g.Name).ToList();
    }

    private static string? FindSteamRoot()
    {
        var candidates = new List<string>();
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (!string.IsNullOrWhiteSpace(pf)) candidates.Add(Path.Combine(pf, "Steam"));
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            if (key?.GetValue("SteamPath") is string path) candidates.Insert(0, path.Replace('/', '\\'));
        }
        catch { }
        return candidates.FirstOrDefault(p => Directory.Exists(Path.Combine(p, "steamapps")));
    }
}
