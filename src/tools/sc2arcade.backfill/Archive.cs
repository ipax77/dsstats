using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace sc2arcade.backfill;

public sealed record ArchiveConfiguration(int Version, DateTimeOffset StartedAt, DateTimeOffset Cutoff,
    string ApiBase, int MinimumDelaySeconds, string ProfileHandle, int PageSize, Region[] Regions)
{
    public static ArchiveConfiguration Create(DateTimeOffset now, DateTimeOffset cutoff) => new(
        1, now, cutoff.ToUniversalTime(), "https://sc2arcade.com/api/", 3,
        "2-S2-1-226401", 200, [new(1, 208271), new(2, 140436)]);

    public void Validate()
    {
        var expected = Create(StartedAt, Cutoff);
        if (Version != 1 || Cutoff.Offset != TimeSpan.Zero || Cutoff > StartedAt
            || ApiBase != expected.ApiBase || MinimumDelaySeconds != 3
            || ProfileHandle != expected.ProfileHandle || PageSize != 200
            || Regions is null || !Regions.SequenceEqual(expected.Regions))
            throw new InvalidDataException("Unsupported or incompatible archive configuration.");
    }
}

public sealed record Region(int RegionId, int MapId);

public sealed record RequestRecord
{
    public required string Kind { get; init; }
    public required DateTimeOffset At { get; init; }
    public int RegionId { get; init; }
    public int MapId { get; init; }
    public string? Cursor { get; init; }
    public DateTimeOffset? RequestStartedAt { get; init; }
    public DateTimeOffset? NotBefore { get; init; }
    public int? StatusCode { get; init; }
    public Dictionary<string, string> Headers { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public string? Body { get; init; }
    public string? BodySha256 { get; init; }
    public bool BodyTruncated { get; init; }
    public string? Next { get; init; }
    public string? Outcome { get; init; }
    public ArchiveConfiguration? Configuration { get; init; }
}

/// <summary>
/// Immutable, atomically renamed records are both the page archive and authoritative checkpoint.
/// A hash chain and contiguous numbering detect changed/missing records. Uncommitted .tmp files are ignored.
/// </summary>
public sealed class Archive : IDisposable
{
    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private sealed record Envelope(string PreviousSha256, string Payload, string Sha256);
    private readonly string directory;
    private readonly FileStream? writerLock;
    private string previousHash = "";
    private int recordCount;
    private ArchiveConfiguration? configuration;
    public IEnumerable<RequestRecord> Records
    {
        get
        {
            for (int i = 0; i < recordCount; i++)
            {
                var envelope = JsonSerializer.Deserialize<Envelope>(File.ReadAllText(Path.Combine(directory, $"{i:D10}.json")), Json)!;
                yield return JsonSerializer.Deserialize<RequestRecord>(envelope.Payload, Json)!;
            }
        }
    }
    public ArchiveConfiguration Configuration => configuration
        ?? throw new InvalidDataException("Missing archive configuration.");

    private Archive(string directory, bool write, bool create)
    {
        this.directory = Path.GetFullPath(directory);
        if (create) Directory.CreateDirectory(this.directory);
        if (!Directory.Exists(this.directory)) throw new DirectoryNotFoundException(this.directory);
        if (write)
            writerLock = new FileStream(Path.Combine(this.directory, "writer.lock"), FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None);
        try
        {
            var files = Directory.GetFiles(this.directory, "*.json").Order(StringComparer.Ordinal).ToArray();
            for (int i = 0; i < files.Length; i++)
            {
                if (Path.GetFileName(files[i]) != $"{i:D10}.json")
                    throw new InvalidDataException("Missing or unexpected archive record.");
                var envelope = JsonSerializer.Deserialize<Envelope>(File.ReadAllText(files[i]), Json)
                    ?? throw new InvalidDataException("Empty archive record.");
                if (envelope.PreviousSha256 != previousHash || envelope.Sha256 != Hash(previousHash + envelope.Payload))
                    throw new InvalidDataException($"Checksum mismatch: {files[i]}");
                var record = JsonSerializer.Deserialize<RequestRecord>(envelope.Payload, Json)
                    ?? throw new InvalidDataException("Empty record payload.");
                if (record.Body != null && record.BodySha256 != Hash(record.Body))
                    throw new InvalidDataException($"Body checksum mismatch: {files[i]}");
                if (i == 0)
                {
                    if (record.Kind != "Configuration") throw new InvalidDataException("Missing configuration record.");
                    configuration = record.Configuration;
                }
                recordCount++;
                previousHash = envelope.Sha256;
            }
            if (!create || recordCount > 0) Configuration.Validate();
        }
        catch { writerLock?.Dispose(); throw; }
    }

    public static Archive Create(string directory, ArchiveConfiguration configuration)
    {
        configuration.Validate();
        var archive = new Archive(directory, true, true);
        try
        {
            if (archive.recordCount > 0) throw new InvalidOperationException("Archive exists. Use resume.");
            File.WriteAllText(Path.Combine(archive.directory, ".gitignore"), "*\n");
            archive.Append(new() { Kind = "Configuration", At = configuration.StartedAt, Configuration = configuration });
            return archive;
        }
        catch { archive.Dispose(); throw; }
    }

    public static Archive Open(string directory, bool write = false) => new(directory, write, false);

    public void Append(RequestRecord record)
    {
        if (writerLock is null) throw new InvalidOperationException("Archive is read-only.");
        string payload = JsonSerializer.Serialize(record, Json);
        string hash = Hash(previousHash + payload);
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new Envelope(previousHash, payload, hash), Json);
        string path = Path.Combine(directory, $"{recordCount:D10}.json");
        string temp = path + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes);
            stream.Flush(flushToDisk: true);
        }
        File.Move(temp, path, overwrite: false);
        // Only advance in memory after the complete record is committed.
        if (recordCount == 0) configuration = record.Configuration;
        recordCount++;
        previousHash = hash;
    }

    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public void Dispose() => writerLock?.Dispose();
}
