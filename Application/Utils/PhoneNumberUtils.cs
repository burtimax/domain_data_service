namespace Application.Utils;

public static class PhoneNumberUtils
{
    /// <summary>
    /// Унифицирует номер телефона в формат +7XXXXXXXXXX
    /// </summary>
    /// <param name="phoneNumber">Исходный номер телефона</param>
    /// <returns>Унифицированный номер телефона или null, если номер невалидный</returns>
    public static string? NormalizePhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        phoneNumber = phoneNumber.Trim('+', ' ', '.');
        
        // Удаляем все нецифровые символы
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // Если номер начинается с 8, заменяем на 7
        if (digits.StartsWith("8"))
        {
            digits = "7" + digits[1..];
        }

        // Если номер начинается с 9, добавляем 7
        if (digits.StartsWith("9") && digits.Length == 10)
        {
            digits = "7" + digits;
        }

        // Проверяем, что номер начинается с 7 и имеет правильную длину
        if (!digits.StartsWith("7") || digits.Length != 11)
        {
            return null;
        }

        return "+" + digits;
    }

    /// <summary>
    /// Проверяет, является ли номер телефона валидным
    /// </summary>
    /// <param name="phoneNumber">Номер телефона для проверки</param>
    /// <returns>true если номер валидный, false в противном случае</returns>
    public static bool IsValidPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        var normalized = NormalizePhoneNumber(phoneNumber);
        return normalized != null;
    }
} 