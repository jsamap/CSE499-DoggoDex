using DoggoDex.Api.Common;
using DoggoDex.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DoggoDex.Api.Infrastructure;

/// <summary>
/// Fills <see cref="Owner.Slug"/> and <see cref="Dog.Slug"/> for newly added profiles so
/// existing controllers and the seeder do not have to know about slugs. If the generated
/// slug is already taken (in either table) the suffix is regenerated, up to three times;
/// the unique index on Slug remains the final guard.
/// </summary>
public sealed class SlugSaveChangesInterceptor : SaveChangesInterceptor
{
    private const int MaxGenerationAttempts = 4;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AssignMissingSlugs(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AssignMissingSlugs(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private static void AssignMissingSlugs(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var generatedInThisBatch = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in context.ChangeTracker.Entries<Owner>())
        {
            if (entry.State is not EntityState.Added || !string.IsNullOrWhiteSpace(entry.Entity.Slug))
            {
                continue;
            }

            entry.Entity.Slug = NextUniqueSlug(context, entry.Entity.Name, generatedInThisBatch);
        }

        foreach (var entry in context.ChangeTracker.Entries<Dog>())
        {
            if (entry.State is not EntityState.Added || !string.IsNullOrWhiteSpace(entry.Entity.Slug))
            {
                continue;
            }

            entry.Entity.Slug = NextUniqueSlug(context, entry.Entity.Name, generatedInThisBatch);
        }
    }

    private static string NextUniqueSlug(DbContext context, string name, HashSet<string> generatedInThisBatch)
    {
        var candidate = SlugGenerator.Generate(name);

        for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
        {
            var taken =
                generatedInThisBatch.Contains(candidate) ||
                context.Set<Owner>().Any(o => o.Slug == candidate) ||
                context.Set<Dog>().Any(d => d.Slug == candidate);

            if (!taken)
            {
                generatedInThisBatch.Add(candidate);
                return candidate;
            }

            candidate = SlugGenerator.Generate(name);
        }

        // Extremely unlikely: let the unique index reject the duplicate instead of looping forever.
        generatedInThisBatch.Add(candidate);
        return candidate;
    }
}
