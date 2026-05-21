namespace DeepSigma.Messaging.Teams.Querying;

public sealed class QueryOptions
{
    public int? Top { get; init; }

    public string? Filter { get; init; }

    public string? OrderBy { get; init; }

    public IReadOnlyList<string>? Select { get; init; }

    public IReadOnlyList<string>? Expand { get; init; }

    public string? Search { get; init; }

    public int? MaxPages { get; init; }
}
