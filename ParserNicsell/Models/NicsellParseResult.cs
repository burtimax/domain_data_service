namespace ConsoleTest.NicsellParser.Models;

public sealed class NicsellParseResult
{
    public required IReadOnlyCollection<NicsellDomainRecord> Items { get; init; }
    public required int VisitedPages { get; init; }
    public required DateTimeOffset ParsedAtUtc { get; init; }

    /// <summary>Пагинация по первой загруженной странице (если блок найден в HTML).</summary>
    public NicsellPaginationInfo? Pagination { get; init; }
}
