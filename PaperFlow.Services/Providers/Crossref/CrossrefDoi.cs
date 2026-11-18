namespace PaperFlow.Services.Providers.Crossref
{
    using System.Text.RegularExpressions;


public static class CrossrefDoi
{
    private static readonly Regex DoiFormat = new(
        @"^10\.\d{4,9}/\S+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("A DOI is required.", nameof(input));

        string doi = input.Trim();
        if (doi.StartsWith("https://doi.org/", StringComparison.OrdinalIgnoreCase))
            doi = doi["https://doi.org/".Length..];
        else if (doi.StartsWith("http://dx.doi.org/", StringComparison.OrdinalIgnoreCase))
            doi = doi["http://dx.doi.org/".Length..];
        else if (doi.StartsWith("doi:", StringComparison.OrdinalIgnoreCase))
            doi = doi[4..];

        doi = doi.Trim().ToLowerInvariant();
        if (doi.Length > 255 || !DoiFormat.IsMatch(doi))
            throw new ArgumentException("Invalid DOI format.", nameof(input));

        return doi;
    }
}
}
