namespace Api.Endpoints.App.SaveStatEvent;

/// <summary>
/// Запрос на сохранение статистического события
/// </summary>
public class SaveStatEventRequest
{
    /// <summary>
    /// Тип события (максимум 30 символов)
    /// </summary>
    public string? Type { get; set; }
}
