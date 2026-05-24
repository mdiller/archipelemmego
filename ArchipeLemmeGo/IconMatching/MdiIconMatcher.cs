using SmartComponents.LocalEmbeddings;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchipeLemmeGo.IconMatching;

public record IconMatch(string IconName, float Similarity);

public sealed class MdiIconMatcherOptions
{
    public string? CacheDirectory { get; set; }
    public string? MetadataUrl { get; set; }
    public bool ForceRefresh { get; set; } = false;
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public Action<string>? Progress { get; set; }
}

public sealed partial class MdiIconMatcher : IDisposable
{
    private const string DefaultMetadataUrl =
        "https://cdn.jsdelivr.net/npm/@mdi/svg@7.4.47/meta.json";

    private readonly LocalEmbedder _embedder;
    private readonly string[] _iconNames;
    private readonly float[][] _iconEmbeddings;
    private readonly object _queryLock = new();

    private static readonly SemaphoreSlim _globalInitSem = new(1, 1);
    private static MdiIconMatcher? _singleton;

    private MdiIconMatcher(LocalEmbedder embedder, string[] names, float[][] embeddings)
    {
        _embedder = embedder;
        _iconNames = names;
        _iconEmbeddings = embeddings;
    }

    public static async Task<MdiIconMatcher> CreateAsync(
        MdiIconMatcherOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new MdiIconMatcherOptions();

        await _globalInitSem.WaitAsync(cancellationToken);
        try
        {
            if (_singleton != null && !options.ForceRefresh)
                return _singleton;

            _singleton = await BuildAsync(options, cancellationToken);
            return _singleton;
        }
        finally
        {
            _globalInitSem.Release();
        }
    }

    private static async Task<MdiIconMatcher> BuildAsync(
        MdiIconMatcherOptions options, CancellationToken ct)
    {
        var url = options.MetadataUrl ?? DefaultMetadataUrl;
        var urlHash = ComputeUrlHash(url);

        options.Progress?.Invoke("Checking icon embedding cache...");

        if (options.CacheDirectory != null && !options.ForceRefresh)
        {
            var (names, embeddings) = TryLoadCache(options.CacheDirectory, urlHash);
            if (names != null && embeddings != null)
            {
                options.Progress?.Invoke($"Loaded {names.Length} icon embeddings from cache.");
                return new MdiIconMatcher(new LocalEmbedder(), names, embeddings);
            }
        }

        options.Progress?.Invoke("Downloading MDI icon metadata...");
        var icons = await FetchIconsAsync(url, options.HttpTimeout, ct);
        options.Progress?.Invoke($"Downloaded {icons.Count} icons. Computing embeddings...");

        var embedder = new LocalEmbedder();
        var resultNames = new string[icons.Count];
        var resultEmbeddings = new float[icons.Count][];

        for (int i = 0; i < icons.Count; i++)
        {
            var emb = embedder.Embed(BuildSearchText(icons[i]));
            resultNames[i] = icons[i].Name;
            resultEmbeddings[i] = emb.Values.ToArray();

            if (i > 0 && i % 1000 == 0)
                options.Progress?.Invoke($"  {i}/{icons.Count} embeddings computed...");
        }

        options.Progress?.Invoke($"All {icons.Count} embeddings done.");

        if (options.CacheDirectory != null)
        {
            options.Progress?.Invoke("Saving cache to disk...");
            SaveCache(options.CacheDirectory, urlHash, resultNames, resultEmbeddings);
            options.Progress?.Invoke("Cache saved.");
        }

        return new MdiIconMatcher(embedder, resultNames, resultEmbeddings);
    }

    public IReadOnlyList<IconMatch> FindMatches(string query, int topN = 5)
    {
        float[] queryValues;
        lock (_queryLock)
        {
            queryValues = _embedder.Embed(query).Values.ToArray();
        }

        var scored = new (float score, int idx)[_iconNames.Length];
        for (int i = 0; i < _iconEmbeddings.Length; i++)
        {
            float dot = 0f;
            var emb = _iconEmbeddings[i];
            for (int j = 0; j < queryValues.Length; j++)
                dot += queryValues[j] * emb[j];
            scored[i] = (dot, i);
        }

        Array.Sort(scored, (a, b) => b.score.CompareTo(a.score));

        int count = Math.Min(topN, scored.Length);
        var results = new IconMatch[count];
        for (int i = 0; i < count; i++)
            results[i] = new IconMatch(_iconNames[scored[i].idx], scored[i].score);
        return results;
    }

    public IconMatch FindBestMatch(string query) => FindMatches(query, 1)[0];

    public void Dispose() => _embedder.Dispose();

    private static string BuildSearchText(MdiIconEntry icon)
    {
        var sb = new StringBuilder(icon.Name);
        if (icon.Tags is { Length: > 0 })
        {
            sb.Append(' ');
            sb.Append(string.Join(' ', icon.Tags));
        }
        if (icon.Aliases is { Length: > 0 })
        {
            sb.Append(' ');
            sb.Append(string.Join(' ', icon.Aliases));
        }
        return sb.ToString();
    }

    private static string ComputeUrlHash(string url)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    // Binary cache format:
    //   [uint32 magic=0x4D444943] [byte version=1] [int32 count]
    //   per icon: [string name (BinaryWriter length-prefixed)] [int32 floatCount] [float32 * floatCount]
    private static (string[]? names, float[][]? embeddings) TryLoadCache(
        string cacheDir, string urlHash)
    {
        var path = Path.Combine(cacheDir, $"{urlHash}.bin");
        if (!File.Exists(path)) return (null, null);
        try
        {
            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs, Encoding.UTF8, leaveOpen: false);

            if (br.ReadUInt32() != 0x4D444943u) return (null, null);
            if (br.ReadByte() != 1) return (null, null);

            int count = br.ReadInt32();
            var names = new string[count];
            var embeddings = new float[count][];

            for (int i = 0; i < count; i++)
            {
                names[i] = br.ReadString();
                int floatCount = br.ReadInt32();
                embeddings[i] = new float[floatCount];
                for (int j = 0; j < floatCount; j++)
                    embeddings[i][j] = br.ReadSingle();
            }

            return (names, embeddings);
        }
        catch
        {
            return (null, null);
        }
    }

    private static void SaveCache(
        string cacheDir, string urlHash, string[] names, float[][] embeddings)
    {
        Directory.CreateDirectory(cacheDir);
        var path = Path.Combine(cacheDir, $"{urlHash}.bin");
        var tmp = path + ".tmp";

        try
        {
            using (var fs = File.Create(tmp))
            using (var bw = new BinaryWriter(fs, Encoding.UTF8, leaveOpen: false))
            {
                bw.Write(0x4D444943u);
                bw.Write((byte)1);
                bw.Write(names.Length);
                for (int i = 0; i < names.Length; i++)
                {
                    bw.Write(names[i]);
                    bw.Write(embeddings[i].Length);
                    foreach (var f in embeddings[i])
                        bw.Write(f);
                }
            }
            File.Move(tmp, path, overwrite: true);
        }
        catch
        {
            try { File.Delete(tmp); } catch { }
            throw;
        }
    }

    private static async Task<List<MdiIconEntry>> FetchIconsAsync(
        string url, TimeSpan timeout, CancellationToken ct)
    {
        using var client = new HttpClient { Timeout = timeout };
        var json = await client.GetStringAsync(url, ct);
        return JsonSerializer.Deserialize(json, MdiJsonContext.Default.ListMdiIconEntry)
               ?? throw new InvalidDataException("Failed to deserialize MDI icon metadata.");
    }

    private sealed record MdiIconEntry(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("tags")] string[]? Tags,
        [property: JsonPropertyName("aliases")] string[]? Aliases
    );

    [JsonSerializable(typeof(List<MdiIconEntry>))]
    private sealed partial class MdiJsonContext : JsonSerializerContext { }
}
