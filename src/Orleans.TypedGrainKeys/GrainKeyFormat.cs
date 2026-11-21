using System.Globalization;
using System.Text;

namespace Orleans;

/// <summary>
/// The single source of truth for how grain key parts are encoded and decoded.
/// Every value type has exactly one canonical string form, written and parsed with the invariant culture.
/// </summary>
internal static class GrainKeyFormat
{
    public const char Delimiter = '|';
    public const char Escape = '\\';

    private static CultureInfo Invariant => CultureInfo.InvariantCulture;

    public static void AppendEscaped(StringBuilder builder, string part)
    {
        foreach (var c in part)
        {
            if (c is Delimiter or Escape)
            {
                builder.Append(Escape);
            }

            builder.Append(c);
        }
    }

    /// <summary>Splits an encoded key into its unescaped parts. A key with N delimiters always has N + 1 parts.</summary>
    public static List<string> Split(string key, Type? keyType)
    {
        var parts = new List<string>();
        var current = new StringBuilder();

        for (var i = 0; i < key.Length; i++)
        {
            var c = key[i];

            if (c == Escape)
            {
                if (i + 1 >= key.Length)
                {
                    throw new GrainKeyFormatException($"it ends with a dangling escape character '{Escape}'", key, keyType, parts.Count);
                }

                var next = key[++i];
                if (next is not (Delimiter or Escape))
                {
                    throw new GrainKeyFormatException($"'{Escape}{next}' at position {i - 1} is not a valid escape sequence", key, keyType, parts.Count);
                }

                current.Append(next);
            }
            else if (c == Delimiter)
            {
                parts.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        parts.Add(current.ToString());
        return parts;
    }

    public static string Format(Guid value) => value.ToString("D");
    public static Guid ParseGuid(string value) => Guid.ParseExact(value, "D");

    public static string Format(int value) => value.ToString(Invariant);
    public static int ParseInt32(string value) => int.Parse(value, NumberStyles.AllowLeadingSign, Invariant);

    public static string Format(long value) => value.ToString(Invariant);
    public static long ParseInt64(string value) => long.Parse(value, NumberStyles.AllowLeadingSign, Invariant);

    public static string Format(bool value) => value ? "true" : "false";
    public static bool ParseBoolean(string value) => value switch
    {
        "true" => true,
        "false" => false,
        _ => throw new FormatException("Expected 'true' or 'false'.")
    };

    public static string Format(DateTime value) => value.ToString("O", Invariant);
    public static DateTime ParseDateTime(string value) => DateTime.ParseExact(value, "O", Invariant, DateTimeStyles.RoundtripKind);

    public static string Format(DateTimeOffset value) => value.ToString("O", Invariant);
    public static DateTimeOffset ParseDateTimeOffset(string value) => DateTimeOffset.ParseExact(value, "O", Invariant, DateTimeStyles.None);

    public static string Format(DateOnly value) => value.ToString("O", Invariant);
    public static DateOnly ParseDateOnly(string value) => DateOnly.ParseExact(value, "O", Invariant, DateTimeStyles.None);

    public static string Format(TimeOnly value) => value.ToString("O", Invariant);
    public static TimeOnly ParseTimeOnly(string value) => TimeOnly.ParseExact(value, "O", Invariant, DateTimeStyles.None);

    public static string Format(TimeSpan value) => value.ToString("c", Invariant);
    public static TimeSpan ParseTimeSpan(string value) => TimeSpan.ParseExact(value, "c", Invariant);

    public static string Format<TEnum>(TEnum value) where TEnum : struct, Enum => value.ToString();
    public static TEnum ParseEnum<TEnum>(string value) where TEnum : struct, Enum => Enum.Parse<TEnum>(value);
}
