using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ArchipeLemmeGo.IconMatching;

public sealed partial class IconAssignmentService : IDisposable
{
    private const string FallbackIcon = "help-circle-outline";
    private static readonly string CacheDir = Path.Combine("resources", "info", "IconCache");

    private readonly ILogger<IconAssignmentService> _logger;
    private Task<MdiIconMatcher>? _matcherTask;
    private readonly object _initLock = new();
    private readonly ConcurrentDictionary<string, GameIconCache> _gameCaches = new();

    public IconAssignmentService(ILogger<IconAssignmentService> logger)
    {
        _logger = logger;
    }

    // Call at startup to begin background initialization.
    public void WarmUp() => _ = GetMatcherAsync();

    public MdiIconMatcher? GetMatcherForDiag() =>
        _matcherTask?.IsCompletedSuccessfully == true ? _matcherTask.Result : null;

    private Task<MdiIconMatcher> GetMatcherAsync()
    {
        lock (_initLock)
        {
            _matcherTask ??= InitMatcherAsync();
            return _matcherTask;
        }
    }

    private async Task<MdiIconMatcher> InitMatcherAsync()
    {
        var cacheDir = Path.Combine(AppContext.BaseDirectory, ".mdi-cache");
        ClearGameCachesIfVersionChanged();
        _logger.LogInformation("[Icons] Initializing MDI icon matcher (may take a moment on first run)...");
        try
        {
            var matcher = await MdiIconMatcher.CreateAsync(new MdiIconMatcherOptions
            {
                CacheDirectory = cacheDir,
                Progress = msg => _logger.LogInformation("[Icons] {Msg}", msg)
            });
            _logger.LogInformation("[Icons] Icon matcher ready.");
            return matcher;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Icons] Failed to initialize icon matcher.");
            throw;
        }
    }

    private void ClearGameCachesIfVersionChanged()
    {
        var versionFile = Path.Combine(CacheDir, "icons_version.txt");
        var currentVersion = MdiIconMatcher.IconsVersion.ToString();

        try
        {
            if (File.Exists(versionFile) && File.ReadAllText(versionFile).Trim() == currentVersion)
                return;

            if (Directory.Exists(CacheDir))
            {
                var jsonFiles = Directory.GetFiles(CacheDir, "*.json");
                foreach (var f in jsonFiles)
                    File.Delete(f);
                _logger.LogInformation("[Icons] Cleared {Count} game icon cache file(s) due to version change (now v{Version}).", jsonFiles.Length, currentVersion);
            }

            Directory.CreateDirectory(CacheDir);
            File.WriteAllText(versionFile, currentVersion);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Icons] Failed to check/clear game icon caches.");
        }
    }

    // Returns the best MDI icon name for a given item or location name and game.
    // Returns fallback icon if the matcher is not ready yet or an error occurs.
    public string GetIcon(string gameName, string name, bool isLocation = false)
    {
        var cache = _gameCaches.GetOrAdd(gameName, LoadGameCache);
        var dict = isLocation ? cache.Locations : cache.Items;

        if (dict.TryGetValue(name, out var existing))
            return existing;

        if (_matcherTask is not { IsCompletedSuccessfully: true })
        {
            var matcherState = _matcherTask == null ? "not started"
                : _matcherTask.IsFaulted ? $"faulted: {_matcherTask.Exception?.GetBaseException().Message}"
                : _matcherTask.IsCanceled ? "canceled"
                : "still running";
            _logger.LogDebug("[Icons] Returning fallback for '{Name}' (game={Game}, isLocation={IsLoc}) — matcher not ready ({State})",
                name, gameName, isLocation, matcherState);
            return FallbackIcon;
        }

        string icon;
        try
        {
            icon = _matcherTask.Result.FindBestMatch(name).IconName;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Icons] FindBestMatch threw for '{Name}' (game={Game})", name, gameName);
            return FallbackIcon;
        }

        if (dict.TryAdd(name, icon))
            SaveGameCache(gameName, cache);

        return icon;
    }

    public void LogStats(ILogger logger, string context)
    {
        var matcherState = _matcherTask == null ? "not started"
            : _matcherTask.IsCompletedSuccessfully ? "ready"
            : _matcherTask.IsFaulted ? $"faulted: {_matcherTask.Exception?.GetBaseException().Message}"
            : _matcherTask.IsCanceled ? "canceled"
            : "still running";

        logger.LogInformation("[Icons:{Context}] Matcher state: {State}", context, matcherState);
        foreach (var (game, cache) in _gameCaches)
            logger.LogInformation("[Icons:{Context}] Cache for '{Game}': {Items} items, {Locs} locations",
                context, game, cache.Items.Count, cache.Locations.Count);

        if (_gameCaches.IsEmpty)
            logger.LogInformation("[Icons:{Context}] No game caches loaded yet", context);
    }

    private GameIconCache LoadGameCache(string gameName)
    {
        var path = GetCachePath(gameName);
        if (!File.Exists(path)) return new GameIconCache();
        try
        {
            var json = File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize(json, IconCacheContext.Default.GameIconCacheDto);
            if (dto == null) return new GameIconCache();

            var cache = new GameIconCache();
            foreach (var kv in dto.Items) cache.Items.TryAdd(kv.Key, kv.Value);
            foreach (var kv in dto.Locations) cache.Locations.TryAdd(kv.Key, kv.Value);
            return cache;
        }
        catch
        {
            return new GameIconCache();
        }
    }

    private void SaveGameCache(string gameName, GameIconCache cache)
    {
        try
        {
            Directory.CreateDirectory(CacheDir);
            var path = GetCachePath(gameName);
            var dto = new GameIconCacheDto
            {
                Items = new Dictionary<string, string>(cache.Items),
                Locations = new Dictionary<string, string>(cache.Locations)
            };
            var json = JsonSerializer.Serialize(dto, IconCacheContext.Default.GameIconCacheDto);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, path, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Icons] Failed to save icon cache for game '{Game}'", gameName);
        }
    }

    private static string GetCachePath(string gameName)
    {
        var safe = Regex.Replace(gameName, @"[^\w\-]", "_");
        if (safe.Length > 40) safe = safe[..40];
        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(gameName)))[..8].ToLowerInvariant();
        return Path.Combine(CacheDir, $"{safe}_{hash}.json");
    }

    public void Dispose()
    {
        if (_matcherTask?.IsCompletedSuccessfully == true)
            _matcherTask.Result.Dispose();
    }

    private sealed class GameIconCache
    {
        public ConcurrentDictionary<string, string> Items { get; } = new();
        public ConcurrentDictionary<string, string> Locations { get; } = new();
    }

    private sealed class GameIconCacheDto
    {
        [JsonPropertyName("items")]
        public Dictionary<string, string> Items { get; set; } = new();

        [JsonPropertyName("locations")]
        public Dictionary<string, string> Locations { get; set; } = new();
    }

    [JsonSerializable(typeof(GameIconCacheDto))]
    private sealed partial class IconCacheContext : JsonSerializerContext { }
}
