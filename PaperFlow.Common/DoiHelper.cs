namespace PaperFlow.Common
{
    public static class DoiHelper
    {
        public static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            string doi = value.Trim();

            if (doi.StartsWith("https://doi.org/", StringComparison.OrdinalIgnoreCase))
                doi = doi["https://doi.org/".Length..];
            else if (doi.StartsWith("doi:", StringComparison.OrdinalIgnoreCase))
                doi = doi[4..];

            doi = doi.Trim();
            return doi.Length == 0 ? null : doi.ToLowerInvariant();
        }
    }
}
