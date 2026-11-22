using Orleans.TypedGrainKeys.Tests.Keys;

namespace Orleans.TypedGrainKeys.Tests;

/// <summary>Runtime behaviour of keys produced by the generator in this project.</summary>
public class GeneratedGrainKeyTests
{
    private static readonly AllTypesGrainKey AllTypes = new(
        Text: @"a|b\c",
        Id: Guid.Parse("a8dba878-3fb1-49e5-9235-b331336db282"),
        Number: -1,
        BigNumber: long.MaxValue,
        Flag: true,
        At: new DateTime(2024, 2, 29, 13, 45, 10, DateTimeKind.Utc),
        AtOffset: new DateTimeOffset(2024, 2, 29, 13, 45, 10, TimeSpan.FromHours(2)),
        Day: new DateOnly(2024, 2, 29),
        Time: new TimeOnly(8, 30),
        Duration: TimeSpan.FromMinutes(90),
        Choice: TestEnum.Bar,
        Name: new FullName("John", "McClane"));

    [Test]
    public async Task Record_Struct_With_All_Types_Encodes_In_Declaration_Order()
    {
        // The nested FullName contributes its parts only, not its own name.
        await Assert.That(AllTypesGrainKey.Format(AllTypes)).IsEqualTo(
            @"all-types|a\|b\\c|a8dba878-3fb1-49e5-9235-b331336db282|-1|9223372036854775807|true|2024-02-29T13:45:10.0000000Z|2024-02-29T13:45:10.0000000+02:00|2024-02-29|08:30:00.0000000|01:30:00|Bar|John|McClane");
    }

    [Test]
    public async Task Record_Struct_With_All_Types_Round_Trips()
    {
        await Assert.That(AllTypesGrainKey.Parse(AllTypesGrainKey.Format(AllTypes))).IsEqualTo(AllTypes);
    }

    [Test]
    public async Task Parse_And_TryParse_Are_Generated_On_The_Key()
    {
        var key = new UserGrainKey(Guid.NewGuid(), 42);
        var encoded = UserGrainKey.Format(key);

        await Assert.That(UserGrainKey.Parse(encoded)).IsEqualTo(key);
        await Assert.That(UserGrainKey.TryParse(encoded, out var parsed)).IsTrue();
        await Assert.That(parsed).IsEqualTo(key);
        await Assert.That(UserGrainKey.TryParse("not-a-key", out _)).IsFalse();
        await Assert.That(() => UserGrainKey.Parse("not-a-key")).Throws<GrainKeyFormatException>();
    }

    [Test]
    public async Task Generated_Keys_Implement_IParsable()
    {
        var key = new UserGrainKey(Guid.NewGuid(), 42);

        await Assert.That(ParseWith<UserGrainKey>(UserGrainKey.Format(key))).IsEqualTo(key);
        await Assert.That(ParseWith<AllTypesGrainKey>(AllTypesGrainKey.Format(AllTypes))).IsEqualTo(AllTypes);
    }

    [Test]
    public async Task Formatting_A_Null_Key_Throws()
    {
        await Assert.That(() => UserGrainKey.Format(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => TestGrainKey.Format(new TestGrainKey("1", 2, 3, default, default, default, default, null!))).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Hand_Written_Keys_Keep_Working()
    {
        var key = new HandWrittenGrainKey(Guid.NewGuid(), "name|with|delimiters");

        await Assert.That(HandWrittenGrainKey.Format(key)).StartsWith("hand-written|");
        await Assert.That(TypedGrainKey.Parse<HandWrittenGrainKey>(HandWrittenGrainKey.Format(key))).IsEqualTo(key);
    }

    private static T ParseWith<T>(string s) where T : IParsable<T> => T.Parse(s, provider: null);
}
