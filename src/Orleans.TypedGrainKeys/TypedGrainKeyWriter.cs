using System.Text;

namespace Orleans;

/// <summary>
/// Builds an encoded grain key from typed parts. Parts are separated by <c>|</c>;
/// any <c>|</c> or <c>\</c> inside a string part is escaped with <c>\</c>.
/// </summary>
public sealed class TypedGrainKeyWriter
{
    private readonly StringBuilder _builder = new();

    public static TypedGrainKeyWriter Create() => new();

    /// <summary>The number of parts written so far.</summary>
    public int PartCount { get; private set; }

    public TypedGrainKeyWriter Write(string part)
    {
        ArgumentNullException.ThrowIfNull(part);

        if (PartCount > 0)
        {
            _builder.Append(GrainKeyFormat.Delimiter);
        }

        GrainKeyFormat.AppendEscaped(_builder, part);
        PartCount++;
        return this;
    }

    public TypedGrainKeyWriter Write(Guid part) => Write(GrainKeyFormat.Format(part));

    public TypedGrainKeyWriter Write(int part) => Write(GrainKeyFormat.Format(part));

    public TypedGrainKeyWriter Write(long part) => Write(GrainKeyFormat.Format(part));

    public TypedGrainKeyWriter Write(bool part) => Write(GrainKeyFormat.Format(part));

    public TypedGrainKeyWriter Write(DateTime part) => Write(GrainKeyFormat.Format(part));

    public TypedGrainKeyWriter Write(DateTimeOffset part) => Write(GrainKeyFormat.Format(part));

    public TypedGrainKeyWriter Write(DateOnly part) => Write(GrainKeyFormat.Format(part));

    public TypedGrainKeyWriter Write(TimeOnly part) => Write(GrainKeyFormat.Format(part));

    public TypedGrainKeyWriter Write(TimeSpan part) => Write(GrainKeyFormat.Format(part));

    public TypedGrainKeyWriter Write<TEnum>(TEnum part) where TEnum : struct, Enum => Write(GrainKeyFormat.Format(part));

    /// <summary>Returns the encoded key. A key must have at least one part.</summary>
    public string Build()
    {
        if (PartCount == 0)
        {
            throw new InvalidOperationException("A grain key must contain at least one part.");
        }

        return _builder.ToString();
    }

    public override string ToString() => _builder.ToString();

    public static implicit operator string(TypedGrainKeyWriter writer) => writer.Build();
}
