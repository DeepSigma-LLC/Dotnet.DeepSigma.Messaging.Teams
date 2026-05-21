using System.Runtime.CompilerServices;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;

namespace DeepSigma.Messaging.Teams.Internal;

internal static class GraphPaging
{
    public static async IAsyncEnumerable<TItem> EnumerateAsync<TItem, TCollection>(
        GraphServiceClient client,
        TCollection? firstPage,
        Func<TCollection, IList<TItem>?> selectItems,
        Func<TCollection, string?> selectNextLink,
        ParsableFactory<TCollection> collectionFactory,
        int? maxPages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
        where TCollection : class, IParsable
        where TItem : class
    {
        var page = firstPage;
        var pageIndex = 0;
        while (page is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var items = selectItems(page);
            if (items is not null)
            {
                foreach (var item in items)
                {
                    if (item is not null)
                    {
                        yield return item;
                    }
                }
            }

            pageIndex++;
            if (maxPages.HasValue && pageIndex >= maxPages.Value)
            {
                yield break;
            }

            var nextLink = selectNextLink(page);
            if (string.IsNullOrEmpty(nextLink))
            {
                yield break;
            }

            page = await FetchNextPageAsync(client, nextLink, collectionFactory, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<TCollection?> FetchNextPageAsync<TCollection>(
        GraphServiceClient client,
        string nextLink,
        ParsableFactory<TCollection> collectionFactory,
        CancellationToken cancellationToken)
        where TCollection : class, IParsable
    {
        var request = new RequestInformation
        {
            HttpMethod = Method.GET,
            URI = new Uri(nextLink),
        };

        return await GraphErrorMapper.ExecuteAsync(() =>
            client.RequestAdapter.SendAsync(request, collectionFactory, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }
}
