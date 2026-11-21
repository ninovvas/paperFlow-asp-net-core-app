namespace PaperFlow.Services.Admin
{

using Microsoft.EntityFrameworkCore;
using PaperFlow.Data;
using PaperFlow.Data.Models;
using PaperFlow.Services.Interfaces;
using PaperFlow.Services.Models;

/// <summary>
/// Manages search availability for the three configured research providers.
/// Existing saved papers are never changed when a provider is disabled.
/// </summary>
public sealed class ResearchSourceManagementService(PowerFlowDbContext context)
    : IResearchSourceManagementService
{
    private sealed record SourceDefinition(string Id, string Name, string Url);

    private static readonly SourceDefinition[] KnownSources =
    [
        new("arxiv", "arXiv", "https://arxiv.org"),
        new("crossref", "Crossref", "https://www.crossref.org"),
        new("openalex", "OpenAlex", "https://openalex.org")
    ];

    public async Task<IReadOnlyList<AdminResearchSourceDto>> GetSourcesAsync(
        CancellationToken cancellationToken = default)
    {
        // Populate only the known provider rows. Do not reset an existing IsEnabled value.
        var rows = await context.ResearchSources.ToListAsync(cancellationToken);
        bool createdAny = false;
        foreach (var definition in KnownSources)
        {
            if (rows.Any(row => row.Name.Equals(definition.Name,
                    StringComparison.OrdinalIgnoreCase)))
                continue;

            var source = new ResearchSource
            {
                Name = definition.Name,
                WebsiteUrl = definition.Url,
                IsEnabled = true
            };
            context.ResearchSources.Add(source);
            rows.Add(source);
            createdAny = true;
        }

        if (createdAny)
            await context.SaveChangesAsync(cancellationToken);

        var counts = await context.Papers.AsNoTracking()
            .Where(p => p.ResearchSourceId != null)
            .GroupBy(p => p.ResearchSourceId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        return KnownSources.Select(definition =>
        {
            var row = rows.First(x => x.Name.Equals(definition.Name,
                StringComparison.OrdinalIgnoreCase));
            return new AdminResearchSourceDto
            {
                ProviderId = definition.Id,
                Name = row.Name,
                WebsiteUrl = row.WebsiteUrl ?? definition.Url,
                IsEnabled = row.IsEnabled,
                SavedPaperCount = counts.GetValueOrDefault(row.Id)
            };
        }).ToArray();
    }

    public async Task<IReadOnlyList<string>> GetEnabledProviderIdsAsync(
        CancellationToken cancellationToken = default)
    {
        // Missing rows are treated as enabled, preserving existing behavior
        // until an administrator first opens the management page.
        var rows = await context.ResearchSources.AsNoTracking()
            .Select(x => new { x.Name, x.IsEnabled })
            .ToListAsync(cancellationToken);

        return KnownSources
            .Where(definition =>
            {
                var row = rows.FirstOrDefault(x => x.Name.Equals(
                    definition.Name, StringComparison.OrdinalIgnoreCase));
                return row is null || row.IsEnabled;
            })
            .Select(x => x.Id)
            .ToArray();
    }

    public async Task<bool> SetEnabledAsync(string providerId, bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        var definition = KnownSources.FirstOrDefault(x => x.Id.Equals(
            providerId?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (definition is null)
            return false;

        var source = await context.ResearchSources
            .FirstOrDefaultAsync(x => x.Name == definition.Name, cancellationToken);
        if (source is null)
        {
            source = new ResearchSource
            {
                Name = definition.Name,
                WebsiteUrl = definition.Url,
                IsEnabled = isEnabled
            };
            context.ResearchSources.Add(source);
        }
        else
        {
            source.IsEnabled = isEnabled;
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

}

