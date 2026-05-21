namespace DeepSigma.Messaging.Teams.Subscriptions;

[Flags]
public enum ChangeTypes
{
    None = 0,
    Created = 1 << 0,
    Updated = 1 << 1,
    Deleted = 1 << 2,
    All = Created | Updated | Deleted,
}

internal static class ChangeTypesExtensions
{
    public static string ToGraphString(this ChangeTypes value)
    {
        if (value == ChangeTypes.None)
        {
            throw new ArgumentException("At least one change type must be specified.", nameof(value));
        }

        var parts = new List<string>(3);
        if (value.HasFlag(ChangeTypes.Created))
        {
            parts.Add("created");
        }
        if (value.HasFlag(ChangeTypes.Updated))
        {
            parts.Add("updated");
        }
        if (value.HasFlag(ChangeTypes.Deleted))
        {
            parts.Add("deleted");
        }
        return string.Join(",", parts);
    }
}
