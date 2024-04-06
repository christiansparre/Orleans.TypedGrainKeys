using Orleans.TypedGrainKeys.Tests.Keys;

namespace Orleans.TypedGrainKeys.Tests;

public class TypedGrainKeyTests
{
    [Fact]
    public void Can_Write_TypedGrainKey()
    {
        var grainKey = new TestGrainKey(
            One: "1",
            Two: 2,
            Three: long.MaxValue,
            Four: new DateTime(1992, 6, 26),
            Five: new DateTimeOffset(1994, 6, 12, 14, 00, 00, TimeSpan.FromHours(1)),
            Six: Guid.Parse("a8dba878-3fb1-49e5-9235-b331336db282"),
            Seven: TestEnum.Baz,
            Eight: new FullName("John", "McClane"));


        Assert.Equal(
            expected: "1|2|9223372036854775807|1992-06-26T00:00:00.0000000|1994-06-12T14:00:00.0000000+01:00|a8dba878-3fb1-49e5-9235-b331336db282|Baz|John|McClane",
            actual: grainKey
        );
    }

    [Fact]
    public void Can_Read_TypedGrainKey()
    {
        var input = "1|2|9223372036854775807|1992-06-26T00:00:00.0000000|1994-06-12T14:00:00.0000000+01:00|a8dba878-3fb1-49e5-9235-b331336db282|Baz|John|McClane";

        TestGrainKey grainKey = input;

        Assert.Equal(
            expected: grainKey,
            actual: input
        );
    }

    [Fact]
    public void Throws_GrainKeyReadException()
    {
        string encoded = new IncorrectReadTestGrainKey("One", 2, 3);

        Assert.Throws<GrainKeyReadException>(() => (IncorrectReadTestGrainKey)encoded);
    }
}