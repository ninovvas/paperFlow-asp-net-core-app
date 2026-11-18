
namespace PaperFlow.Services.Providers
{
    using System.Text.RegularExpressions;


public static class OpenAlexWorkId
{
    private static readonly Regex ValidId = new(@"^W[0-9]{1,20}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("OpenAlex work ID is required.", nameof(value));
        string id = value.Trim();
        if (id.StartsWith("https://openalex.org/", StringComparison.OrdinalIgnoreCase))
            id = id["https://openalex.org/".Length..];
        if (!ValidId.IsMatch(id))
            throw new ArgumentException("Invalid OpenAlex work ID.", nameof(value));
        return id.ToUpperInvariant();
    }
}
}

