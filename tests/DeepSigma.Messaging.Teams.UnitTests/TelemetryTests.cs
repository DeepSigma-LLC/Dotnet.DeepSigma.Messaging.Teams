using System.Diagnostics;
using DeepSigma.Messaging.Teams.Diagnostics;
using Xunit;

namespace DeepSigma.Messaging.Teams.UnitTests;

public class TelemetryTests
{
    [Fact]
    public void ActivitySource_HasExpectedName()
    {
        Assert.Equal("DeepSigma.Messaging.Teams", TeamsTelemetry.ActivitySource.Name);
        Assert.Equal(TeamsTelemetry.ActivitySourceName, TeamsTelemetry.ActivitySource.Name);
    }

    [Fact]
    public void ActivitySource_EmitsActivity_WhenListenerSubscribes()
    {
        var captured = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == TeamsTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => captured.Add(activity),
        };
        ActivitySource.AddActivityListener(listener);

        using (var activity = TeamsTelemetry.ActivitySource.StartActivity("test.op", ActivityKind.Client))
        {
            activity?.SetTag(TeamsTelemetry.Tags.TeamId, "team-123");
        }

        var recorded = Assert.Single(captured);
        Assert.Equal("test.op", recorded.OperationName);
        Assert.Equal("team-123", recorded.GetTagItem(TeamsTelemetry.Tags.TeamId));
    }
}
