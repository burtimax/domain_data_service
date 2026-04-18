namespace ConsoleTest.NicsellParser.Models;

public sealed class NicsellParserOptions
{
    public required string CookieHeader { get; init; }
    public string BaseAddress { get; init; } = "https://nicsell.com";
    public string CulturePath { get; init; } = "en";
    public int MaxPerPage { get; init; } = 250;
    public int? MaxPages { get; init; }
    public TimeSpan RequestDelay { get; init; } = TimeSpan.FromMilliseconds(500);
}
