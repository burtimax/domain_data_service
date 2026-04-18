namespace ConsoleTest.NicsellParser.Models;

public sealed class NicsellFilter
{
    public string? Query { get; init; }
    public int? LengthFrom { get; init; }
    public int? LengthTo { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public string? Option { get; init; }
    public string? Sort { get; init; } = "bid_desc";
    public string Mode { get; init; } = "expert";

    public IReadOnlyCollection<string> Tlds { get; init; } = [];
    public IReadOnlyCollection<string> NoDictionaryLanguages { get; init; } = [];
}
