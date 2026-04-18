namespace ConsoleTest.NicsellParser.Models;

/// <summary>
/// Пагинация списка доменов из HTML ответа (блок <c>ul.pagination</c> внизу списка).
/// </summary>
public sealed class NicsellPaginationInfo
{
    /// <summary>Текущая страница (из <c>li.active</c>).</summary>
    public int? CurrentPage { get; init; }

    /// <summary>Максимальный номер страницы по фильтру (из ссылок, обычно «»|» с <c>page=N</c>).</summary>
    public int? TotalPages { get; init; }

    /// <summary>Выбранный размер страницы из HTML (подчёркнутая ссылка maxperpage), если удалось определить.</summary>
    public int? EntriesPerPageFromHtml { get; init; }

    /// <summary>Приблизительное число записей: <see cref="TotalPages"/> × <see cref="EntriesPerPageFromHtml"/>.</summary>
    public int? ApproximateTotalEntries { get; init; }
}
