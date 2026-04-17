using System.Text;
using System.Text.Json;
using Shared.Configs;

namespace Api.Services;

public interface IFailedMessageArchiveStore
{
    Task AppendAsync(FailedMessageArchiveRecord record, CancellationToken ct);
    Task<IReadOnlyList<FailedMessageArchiveRecord>> ReadRecentAsync(int take, CancellationToken ct);
}

public sealed class FailedMessageArchiveStore : IFailedMessageArchiveStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _filePath;

    public FailedMessageArchiveStore(AppConfiguration config)
    {
        _filePath = Path.IsPathRooted(config.RabbitMq.DlqArchiveFilePath)
            ? config.RabbitMq.DlqArchiveFilePath
            : Path.Combine(AppContext.BaseDirectory, config.RabbitMq.DlqArchiveFilePath);
    }

    public async Task AppendAsync(FailedMessageArchiveRecord record, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var line = JsonSerializer.Serialize(record, JsonOptions);
        await File.AppendAllTextAsync(_filePath, line + Environment.NewLine, Encoding.UTF8, ct);
    }

    public async Task<IReadOnlyList<FailedMessageArchiveRecord>> ReadRecentAsync(int take, CancellationToken ct)
    {
        if (!File.Exists(_filePath) || take <= 0)
            return [];

        var lines = await File.ReadAllLinesAsync(_filePath, ct);
        var records = new List<FailedMessageArchiveRecord>();
        foreach (var line in lines.Reverse().Take(take))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var record = JsonSerializer.Deserialize<FailedMessageArchiveRecord>(line, JsonOptions);
            if (record is not null)
                records.Add(record);
        }

        records.Reverse();
        return records;
    }
}

public sealed class FailedMessageArchiveRecord
{
    public DateTimeOffset FailedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Reason { get; set; } = string.Empty;
    public string? MessageId { get; set; }
    public int RetryAttempt { get; set; }
    public string Payload { get; set; } = string.Empty;
}
