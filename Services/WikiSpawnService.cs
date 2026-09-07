using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ArkCommander.Models;

namespace ArkCommander.Services;

public class WikiSpawnService
{
    private static readonly HttpClient Http = new();

    static WikiSpawnService()
    {
        Http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) ArkCommander/1.0");
        Http.Timeout = TimeSpan.FromSeconds(30);
    }

    public static string CachePath => Path.Combine(AppContext.BaseDirectory, "spawn_cache.json");
    public static string DebugPath => Path.Combine(AppContext.BaseDirectory, "spawn_debug.txt");

    private static readonly Dictionary<string, ObeliskPoint[]> Obelisks = new()
    {
        ["Ragnarok"] = new[]
        {
            new ObeliskPoint { Name = "Красный обелиск", Lat = 34.0, Lon = 85.0, Color = "red" },
            new ObeliskPoint { Name = "Синий обелиск", Lat = 16.0, Lon = 15.0, Color = "blue" },
            new ObeliskPoint { Name = "Зелёный обелиск", Lat = 57.0, Lon = 37.0, Color = "green" },
        },
        ["TheIsland"] = new[]
        {
            new ObeliskPoint { Name = "Красный обелиск", Lat = 49.0, Lon = 56.0, Color = "red" },
            new ObeliskPoint { Name = "Синий обелиск", Lat = 25.0, Lon = 91.0, Color = "blue" },
            new ObeliskPoint { Name = "Зелёный обелиск", Lat = 79.0, Lon = 58.0, Color = "green" },
        },
    };

    public static ObeliskPoint[] GetObelisks(string mapName)
    {
        return Obelisks.TryGetValue(mapName, out var arr) ? arr : Array.Empty<ObeliskPoint>();
    }

    public async Task<List<SpawnPoint>> LoadAsync(string mapName, Action<string> status)
    {
        if (File.Exists(CachePath))
        {
            try
            {
                var info = new FileInfo(CachePath);

                if (DateTime.UtcNow - info.LastWriteTimeUtc < TimeSpan.FromDays(7))
                {
                    string json = await File.ReadAllTextAsync(CachePath);
                    var cached = JsonSerializer.Deserialize<CacheData>(json);

                    if (cached != null && cached.MapName == mapName && cached.Points.Count > 0)
                    {
                        status($"кэш: {cached.Points.Count} точек");
                        return cached.Points;
                    }
                }
            }
            catch
            {
            }
        }

        var log = new List<string> { $"[{DateTime.Now:HH:mm:ss}] map={mapName}" };

        try
        {
            // проб Cargo (на случай если появится)
            var cargoPoints = await TryCargoFallback(log);

            if (cargoPoints.Count > 0)
            {
                status($"ви́ки (cargo): {cargoPoints.Count} точек");
                log.Add($"cargo OK: {cargoPoints.Count}");
                File.WriteAllLines(DebugPath, log);
                await SaveCache(mapName, cargoPoints);
                return cargoPoints;
            }

            // фолбэк: ищем страницы с данными через обычные вики-запросы
            status("ищу данные через вики-страницы");

            var wikiPoints = await TryWikiPages(mapName, log, status);

            if (wikiPoints.Count > 0)
            {
                status($"ви́ки: {wikiPoints.Count} точек");
                log.Add($"wiki OK: {wikiPoints.Count}");
                File.WriteAllLines(DebugPath, log);
                await SaveCache(mapName, wikiPoints);
                return wikiPoints;
            }
        }
        catch (Exception ex)
        {
            log.Add("exception: " + ex);
        }

        var fallback = GetFallback(mapName);

        if (fallback.Count > 0)
        {
            status($"встроенные данные: {fallback.Count} зон");
            log.Add("fallback: " + fallback.Count);
            File.WriteAllLines(DebugPath, log);
            return fallback;
        }

        File.WriteAllLines(DebugPath, log);
        status("точки не получены — см. spawn_debug.txt");
        return new List<SpawnPoint>();
    }

    private async Task SaveCache(string mapName, List<SpawnPoint> points)
    {
        try
        {
            var cache = new CacheData { MapName = mapName, Points = points };
            await File.WriteAllTextAsync(CachePath,
                JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private async Task<List<SpawnPoint>> TryCargoFallback(List<string> log)
    {
        var result = new List<SpawnPoint>();

        try
        {
            string url = "https://ark.wiki.gg/api.php?action=cargoquery&tables=_tables&fields=_tableName&limit=10&format=json";
            var resp = await Http.GetAsync(url);

            log.Add($"cargo _tables -> {(int)resp.StatusCode}");

            if (!resp.IsSuccessStatusCode)
                return result;

            string json = await resp.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("error", out _))
            {
                log.Add("cargo API недоступен (нет расширения Cargo)");
                return result;
            }
        }
        catch (Exception ex)
        {
            log.Add($"cargo check: {ex.Message}");
        }

        return result;
    }

    private async Task<List<SpawnPoint>> TryWikiPages(string mapName, List<string> log, Action<string> status)
    {
        var result = new List<SpawnPoint>();

        // пробуем разные варианты страниц с данными
        string[] pageNames = new[]
        {
            $"Module:DataMaps/{mapName}",
            $"Module:Data/{mapName}/Spawn",
            $"Module:Data/{mapName}/CreatureSpawns",
            $"{mapName}/SpawnMap",
            $"{mapName} Spawn Map",
        };

        foreach (var pageName in pageNames)
        {
            status($"проверяю {pageName}");

            var points = await TryWikiPage(pageName, log);

            if (points.Count > 0)
            {
                log.Add($"{pageName}: {points.Count} точек");
                result.AddRange(points);
                break;
            }
        }

        return result;
    }

    private async Task<List<SpawnPoint>> TryWikiPage(string pageName, List<string> log)
    {
        var result = new List<SpawnPoint>();

        try
        {
            string url = $"https://ark.wiki.gg/api.php?action=query&prop=revisions&rvprop=content"
                + $"&titles={Uri.EscapeDataString(pageName)}&format=json";

            var resp = await Http.GetAsync(url);

            log.Add($"GET {pageName} -> {(int)resp.StatusCode}");

            if (!resp.IsSuccessStatusCode)
                return result;

            string json = await resp.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("query", out var query))
                return result;

            if (!query.TryGetProperty("pages", out var pages))
                return result;

            foreach (var page in pages.EnumerateObject())
            {
                if (page.Value.TryGetProperty("missing", out _))
                {
                    log.Add($"{pageName}: страница не найдена");
                    continue;
                }

                if (!page.Value.TryGetProperty("revisions", out var revisions))
                    continue;

                foreach (var rev in revisions.EnumerateArray())
                {
                    if (!rev.TryGetProperty("*", out var content))
                        continue;

                    string wikitext = content.GetString() ?? "";

                    log.Add($"{pageName}: {wikitext.Length} символов");

                    // ищем JSON-блоки в wikitext
                    var points = ParseWikitextForPoints(wikitext, log);

                    if (points.Count > 0)
                    {
                        result.AddRange(points);
                        return result;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log.Add($"{pageName}: {ex.Message}");
        }

        return result;
    }

    private List<SpawnPoint> ParseWikitextForPoints(string wikitext, List<string> log)
    {
        var result = new List<SpawnPoint>();

        // ищем JSON-блоки (обычно в {{#cargo: или в return {...}})
        var jsonMatches = Regex.Matches(wikitext, @"\{[^{}]*""lat""[^{}]*\}", RegexOptions.Singleline);

        log.Add($"найдено JSON-блоков: {jsonMatches.Count}");

        foreach (Match match in jsonMatches)
        {
            try
            {
                var point = JsonSerializer.Deserialize<SpawnPoint>(match.Value);

                if (point != null && point.Lat != 0 && point.Lon != 0)
                {
                    result.Add(point);
                }
            }
            catch
            {
                // пропускаем битые блоки
            }
        }

        // если JSON не нашёлся, пробуем табличные данные (wikitable)
        if (result.Count == 0)
        {
            result = ParseWikiTables(wikitext, log);
        }

        return result;
    }

    private List<SpawnPoint> ParseWikiTables(string wikitext, List<string> log)
    {
        var result = new List<SpawnPoint>();

        // ищем строки типа | creature = Rex | lat = 50.0 | lon = 30.0 | rarity = rare
        var lines = wikitext.Split('\n');

        var current = new SpawnPoint();

        foreach (var line in lines)
        {
            string trimmed = line.Trim();

            if (trimmed.StartsWith("|"))
            {
                var parts = trimmed.Substring(1).Split('=');

                if (parts.Length == 2)
                {
                    string key = parts[0].Trim().ToLowerInvariant();
                    string val = parts[1].Trim();

                    if (key == "creature" || key == "name" || key == "dino")
                        current.Creature = val;
                    else if (key == "lat" || key == "latitude")
                    {
                        if (double.TryParse(val.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out double d))
                            current.Lat = d;
                    }
                    else if (key == "lon" || key == "longitude")
                    {
                        if (double.TryParse(val.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out double d))
                            current.Lon = d;
                    }
                    else if (key == "rarity" || key == "frequency")
                        current.Rarity = NormalizeRarity(val);
                }
            }
            else if (trimmed.StartsWith("|-") || trimmed.StartsWith("|}"))
            {
                if (current.Lat != 0 && current.Lon != 0 && !string.IsNullOrEmpty(current.Creature))
                {
                    result.Add(current);
                }

                current = new SpawnPoint();
            }
        }

        log.Add($"из таблиц: {result.Count} точек");

        return result;
    }

    // встроенные данные: зоны Карчародонтазавра на Ragnarok (как на вики)
    private static List<SpawnPoint> GetFallback(string mapName)
    {
        if (!mapName.Equals("Ragnarok", StringComparison.OrdinalIgnoreCase))
            return new List<SpawnPoint>();

        return new List<SpawnPoint>
        {
            new SpawnPoint { Creature = "Carcharodontosaurus", Rarity = "veryrare", Lat = 13, Lon = 72, Lat2 = 24, Lon2 = 85 },
            new SpawnPoint { Creature = "Carcharodontosaurus", Rarity = "veryrare", Lat = 45, Lon = 35, Lat2 = 60, Lon2 = 40 },
            new SpawnPoint { Creature = "Carcharodontosaurus", Rarity = "veryrare", Lat = 60, Lon = 33, Lat2 = 66, Lon2 = 50 },
            new SpawnPoint { Creature = "Carcharodontosaurus", Rarity = "veryrare", Lat = 52, Lon = 70, Lat2 = 76, Lon2 = 80 },
        };
    }

    private static string NormalizeRarity(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "common";

        string r = raw.ToLowerInvariant();

        if (r.Contains("very common") || r.Contains("verycommon") || r.Contains("abundant"))
            return "verycommon";

        if (r.Contains("very uncommon") || r.Contains("veryuncommon") || r.Contains("scarce"))
            return "veryuncommon";

        if (r.Contains("uncommon"))
            return "uncommon";

        if (r.Contains("very rare") || r.Contains("veryrare"))
            return "veryrare";

        if (r.Contains("rare"))
            return "rare";

        return "common";
    }

    private class CacheData
    {
        public string MapName { get; set; } = "";
        public List<SpawnPoint> Points { get; set; } = new();
    }
}


