using DeepSigma.Messaging.Teams.Subscriptions;
using Xunit;

namespace DeepSigma.Messaging.Teams.UnitTests;

public class ChangeTypesTests
{
    [Theory]
    [InlineData(ChangeTypes.Created, "created")]
    [InlineData(ChangeTypes.Updated, "updated")]
    [InlineData(ChangeTypes.Deleted, "deleted")]
    [InlineData(ChangeTypes.Created | ChangeTypes.Updated, "created,updated")]
    [InlineData(ChangeTypes.Created | ChangeTypes.Deleted, "created,deleted")]
    [InlineData(ChangeTypes.All, "created,updated,deleted")]
    public void ToGraphString_FormatsAsCommaSeparatedLowercase(ChangeTypes input, string expected)
    {
        Assert.Equal(expected, input.ToGraphString());
    }

    [Fact]
    public void ToGraphString_None_Throws()
    {
        Assert.Throws<ArgumentException>(() => ChangeTypes.None.ToGraphString());
    }
}
