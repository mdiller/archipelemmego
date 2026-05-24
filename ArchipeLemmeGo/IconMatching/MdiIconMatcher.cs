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
    // Bump this to force a full cache rebuild on next startup.
    public const int IconsVersion = 4;

    private const string DefaultMetadataUrl =
        "https://cdn.jsdelivr.net/npm/@mdi/svg@7.4.47/meta.json";

    private readonly LocalEmbedder _embedder;
    private readonly string[] _iconNames;
    private readonly string[] _iconSearchTexts;
    private readonly float[][] _iconEmbeddings;
    private readonly float[][] _iconNameEmbeddings;
    private readonly Dictionary<string, int> _iconIndex;
    private readonly object _queryLock = new();

    private static readonly SemaphoreSlim _globalInitSem = new(1, 1);
    private static MdiIconMatcher? _singleton;

    private MdiIconMatcher(LocalEmbedder embedder, string[] names, string[] searchTexts, float[][] embeddings, float[][] nameEmbeddings)
    {
        _embedder = embedder;
        _iconNames = names;
        _iconSearchTexts = searchTexts;
        _iconEmbeddings = embeddings;
        _iconNameEmbeddings = nameEmbeddings;
        _iconIndex = new Dictionary<string, int>(names.Length);
        for (int i = 0; i < names.Length; i++)
            _iconIndex[names[i]] = i;
    }

    public (string fullText, string nameText)? GetIconTexts(string iconName)
    {
        if (!_iconIndex.TryGetValue(iconName, out var idx)) return null;
        return (_iconSearchTexts[idx], BuildNameText(iconName));
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

        options.Progress?.Invoke($"Checking icon embedding cache (IconsVersion={IconsVersion})...");

        if (options.CacheDirectory != null && !options.ForceRefresh)
        {
            var (names, searchTexts, embeddings, nameEmbeddings) = TryLoadCache(options.CacheDirectory, urlHash);
            if (names != null && searchTexts != null && embeddings != null && nameEmbeddings != null)
            {
                options.Progress?.Invoke($"Loaded {names.Length} icon embeddings from cache.");
                return new MdiIconMatcher(new LocalEmbedder(), names, searchTexts, embeddings, nameEmbeddings);
            }
            options.Progress?.Invoke("Cache miss or version mismatch — rebuilding from scratch.");
        }

        options.Progress?.Invoke("Downloading MDI icon metadata...");
        var icons = await FetchIconsAsync(url, options.HttpTimeout, ct);
        options.Progress?.Invoke($"Downloaded {icons.Count} icons. Computing embeddings...");

        var embedder = new LocalEmbedder();
        var resultNames = new string[icons.Count];
        var resultSearchTexts = new string[icons.Count];
        var resultEmbeddings = new float[icons.Count][];
        var resultNameEmbeddings = new float[icons.Count][];

        for (int i = 0; i < icons.Count; i++)
        {
            var searchText = BuildSearchText(icons[i]);
            resultNames[i] = icons[i].Name;
            resultSearchTexts[i] = searchText;
            resultEmbeddings[i] = Normalize(embedder.Embed(searchText).Values.ToArray());
            resultNameEmbeddings[i] = Normalize(embedder.Embed(BuildNameText(icons[i].Name)).Values.ToArray());

            if (i > 0 && i % 1000 == 0)
                options.Progress?.Invoke($"  {i}/{icons.Count} embeddings computed...");
        }

        options.Progress?.Invoke($"All {icons.Count} embeddings done.");

        if (options.CacheDirectory != null)
        {
            options.Progress?.Invoke("Saving cache to disk...");
            SaveCache(options.CacheDirectory, urlHash, resultNames, resultSearchTexts, resultEmbeddings, resultNameEmbeddings);
            options.Progress?.Invoke("Cache saved.");
        }

        return new MdiIconMatcher(embedder, resultNames, resultSearchTexts, resultEmbeddings, resultNameEmbeddings);
    }

    public IReadOnlyList<IconMatch> FindMatches(string query, int topN = 5)
    {
        float[] queryValues;
        lock (_queryLock)
        {
            queryValues = Normalize(_embedder.Embed(query).Values.ToArray());
        }

        var scored = new (float score, int idx)[_iconNames.Length];
        for (int i = 0; i < _iconEmbeddings.Length; i++)
        {
            float fullDot = 0f, nameDot = 0f;
            var emb = _iconEmbeddings[i];
            var nameEmb = _iconNameEmbeddings[i];
            for (int j = 0; j < queryValues.Length; j++)
            {
                fullDot += queryValues[j] * emb[j];
                nameDot += queryValues[j] * nameEmb[j];
            }
            scored[i] = ((fullDot + nameDot * 2f) / 3f, i);
        }

        Array.Sort(scored, (a, b) => b.score.CompareTo(a.score));

        int count = Math.Min(topN, scored.Length);
        var results = new IconMatch[count];
        for (int i = 0; i < count; i++)
            results[i] = new IconMatch(_iconNames[scored[i].idx], scored[i].score);
        return results;
    }

    private static float[] Normalize(float[] v)
    {
        float mag = 0f;
        foreach (var f in v) mag += f * f;
        mag = MathF.Sqrt(mag);
        if (mag < 1e-9f) return v;
        for (int i = 0; i < v.Length; i++) v[i] /= mag;
        return v;
    }

    public IconMatch FindBestMatch(string query) => FindMatches(query, 1)[0];

    public int IconCount => _iconNames.Length;

    public void Dispose() => _embedder.Dispose();

    private static string BuildNameText(string iconName) => iconName.Replace('-', ' ');

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
    //   [uint32 magic=0x4D444943] [byte formatVersion=3] [int32 iconsVersion] [int32 count]
    //   per icon: [string name (BinaryWriter length-prefixed)]
    //             [string searchText]                          <- full (name+tags+aliases) text
    //             [int32 floatCount] [float32 * floatCount]   <- full embedding
    //             [int32 floatCount] [float32 * floatCount]   <- name-only embedding
    private static (string[]? names, string[]? searchTexts, float[][]? embeddings, float[][]? nameEmbeddings) TryLoadCache(
        string cacheDir, string urlHash)
    {
        var path = Path.Combine(cacheDir, $"{urlHash}.bin");
        if (!File.Exists(path)) return (null, null, null, null);
        try
        {
            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs, Encoding.UTF8, leaveOpen: false);

            if (br.ReadUInt32() != 0x4D444943u) return (null, null, null, null);
            if (br.ReadByte() != 3) return (null, null, null, null);
            if (br.ReadInt32() != IconsVersion) return (null, null, null, null);

            int count = br.ReadInt32();
            var names = new string[count];
            var searchTexts = new string[count];
            var embeddings = new float[count][];
            var nameEmbeddings = new float[count][];

            for (int i = 0; i < count; i++)
            {
                names[i] = br.ReadString();
                searchTexts[i] = br.ReadString();
                int floatCount = br.ReadInt32();
                embeddings[i] = new float[floatCount];
                for (int j = 0; j < floatCount; j++)
                    embeddings[i][j] = br.ReadSingle();
                int nameFloatCount = br.ReadInt32();
                nameEmbeddings[i] = new float[nameFloatCount];
                for (int j = 0; j < nameFloatCount; j++)
                    nameEmbeddings[i][j] = br.ReadSingle();
            }

            return (names, searchTexts, embeddings, nameEmbeddings);
        }
        catch
        {
            return (null, null, null, null);
        }
    }

    private static void SaveCache(
        string cacheDir, string urlHash, string[] names, string[] searchTexts, float[][] embeddings, float[][] nameEmbeddings)
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
                bw.Write((byte)3);
                bw.Write(IconsVersion);
                bw.Write(names.Length);
                for (int i = 0; i < names.Length; i++)
                {
                    bw.Write(names[i]);
                    bw.Write(searchTexts[i]);
                    bw.Write(embeddings[i].Length);
                    foreach (var f in embeddings[i])
                        bw.Write(f);
                    bw.Write(nameEmbeddings[i].Length);
                    foreach (var f in nameEmbeddings[i])
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
