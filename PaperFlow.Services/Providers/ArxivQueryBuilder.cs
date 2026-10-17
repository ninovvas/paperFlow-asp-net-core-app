

namespace PaperFlow.Services.Providers
{

    using System.Globalization;
    using System.Text.RegularExpressions;

    using PaperFlow.Services.Models;
    public static class ArxivQueryBuilder
    {
        private const string ApiUrl =
            "https://export.arxiv.org/api/query";

        public static string BuildRequestUrl(
            PaperSearchCriteria criteria)
        {
            ArgumentNullException.ThrowIfNull(criteria);

            if (criteria.MaxResults < 1 ||
                criteria.MaxResults > 50)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(criteria.MaxResults));
            }

            if (criteria.Start < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(criteria.Start));
            }

            var expressions = new List<string>();

            if (!string.IsNullOrWhiteSpace(criteria.Keywords))
            {
                string field = criteria.SearchField switch
                {
                    "ti" => "ti",
                    "abs" => "abs",
                    _ => "all"
                };

                string keywords = Clean(criteria.Keywords);

                expressions.Add($"{field}:\"{keywords}\"");
            }

            if (!string.IsNullOrWhiteSpace(criteria.Category))
            {
                expressions.Add(
                    $"cat:{criteria.Category}");
            }

            if (!string.IsNullOrWhiteSpace(criteria.Author))
            {
                expressions.Add(
                    $"au:\"{Clean(criteria.Author)}\"");
            }

            if (criteria.SubmittedFrom.HasValue ||
                criteria.SubmittedTo.HasValue)
            {
                DateOnly from =
                    criteria.SubmittedFrom ??
                    new DateOnly(1991, 1, 1);

                DateOnly to =
                    criteria.SubmittedTo ??
                    DateOnly.FromDateTime(DateTime.UtcNow);

                if (from > to)
                {
                    throw new ArgumentException(
                        "Invalid submission date range.");
                }

                string start = from.ToString(
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture) + "0000";

                string end = to.ToString(
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture) + "2359";

                expressions.Add(
                    $"submittedDate:[{start} TO {end}]");
            }

            if (expressions.Count == 0)
            {
                throw new ArgumentException(
                    "At least one search criterion is required.");
            }

            string query = string.Join(" AND ", expressions);

            return ApiUrl +
                "?search_query=" +
                Uri.EscapeDataString(query) +
                $"&start={criteria.Start}" +
                $"&max_results={criteria.MaxResults}" +
                $"&sortBy={criteria.SortBy}" +
                $"&sortOrder={criteria.SortOrder}";
        }

        private static string Clean(string input)
        {
            string cleaned = Regex.Replace(
                input.Trim(),
                @"[""\[\]\(\)\\]",
                " ");

            return Regex.Replace(
                cleaned, @"\s+", " ").Trim();
        }
    }
}

