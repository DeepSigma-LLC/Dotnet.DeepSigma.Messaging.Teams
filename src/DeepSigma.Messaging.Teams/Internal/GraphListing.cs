using System.Diagnostics;
using System.Runtime.CompilerServices;
using DeepSigma.Messaging.Teams.Diagnostics;
using Microsoft.Kiota.Abstractions.Serialization;

namespace DeepSigma.Messaging.Teams.Internal;

/// <summary>
/// Shared shape for "list operation that pages with $skiptoken." Wraps activity tracing,
/// the error-mapping wrapper around the first-page fetch, page iteration, and item mapping.
/// </summary>
internal static class GraphListing
{
    public static async IAsyncEnumerable<TDto> ListPagedAsync<TItem, TCollection, TDto>(
        TeamsClientContext context,
        string activityName,
        Action<Activity?>? configureActivity,
        Func<CancellationToken, Task<TCollection?>> fetchFirstPage,
        Func<TCollection, IList<TItem>?> selectItems,
        Func<TCollection, string?> selectNextLink,
        ParsableFactory<TCollection> collectionFactory,
        Func<TItem, TDto> mapItem,
        int? maxPages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
        where TCollection : class, IParsable
        where TItem : class
    {
        using var activity = TeamsTelemetry.ActivitySource.StartActivity(activityName, ActivityKind.Client);
        configureActivity?.Invoke(activity);

        var firstPage = await GraphErrorMapper.ExecuteAsync(() => fetchFirstPage(cancellationToken)).ConfigureAwait(false);

        await foreach (var item in GraphPaging.EnumerateAsync(
            context.GraphClient,
            firstPage,
            selectItems,
            selectNextLink,
            collectionFactory,
            maxPages,
            cancellationToken).ConfigureAwait(false))
        {
            yield return mapItem(item);
        }
    }
}
