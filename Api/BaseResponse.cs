namespace Api;

/// <summary>
/// Базовый класс ответа API с типизированными данными
/// </summary>
/// <typeparam name="T">Тип данных ответа</typeparam>
public class BaseResponse<T>
{
    /// <summary>
    /// Признак успешности запроса.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Данные ответа
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Описание ответа или сообщение об ошибке
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Дополнительные метаданные (например, пагинация).
    /// </summary>
    public object? Meta { get; set; }

    public static BaseResponse<T> Ok(T data, string? description = null, object? meta = null)
    {
        return new BaseResponse<T>
        {
            Success = true,
            Data = data,
            Description = description,
            Meta = meta
        };
    }

    public static BaseResponse<T> Fail(string description)
    {
        return new BaseResponse<T>
        {
            Success = false,
            Data = default,
            Description = description
        };
    }
}