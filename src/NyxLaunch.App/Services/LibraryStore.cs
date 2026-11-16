using System.IO;
using System.Text.Json;
using NyxLaunch.App.Models;

namespace NyxLaunch.App.Services;

public sealed class LibraryStore
{
    private readonly string _directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BuiltByNyx", "NyxLaunch");
    private readonly string _file;
    private readonly string _collectionsFile;

    public LibraryStore()
    {
        _file = Path.Combine(_directory, "library.json");
        _collectionsFile = Path.Combine(_directory, "collections.json");
    }
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public List<Game> Load()
    {
        try { if (!File.Exists(_file)) return []; return JsonSerializer.Deserialize<List<Game>>(File.ReadAllText(_file), Options) ?? []; }
        catch { return []; }
    }

    public List<string> LoadCollections()
    {
        try { if (!File.Exists(_collectionsFile)) return []; return JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_collectionsFile), Options) ?? []; }
        catch { return []; }
    }

    public void SaveCollections(IEnumerable<string> collections)
    {
        Directory.CreateDirectory(_directory);
        var temp = _collectionsFile + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(collections, Options));
        File.Move(temp, _collectionsFile, true);
    }

    public void Save(IEnumerable<Game> games)
    {
        Directory.CreateDirectory(_directory);
        var temp = _file + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(games, Options));
        File.Move(temp, _file, true);
    }
}
