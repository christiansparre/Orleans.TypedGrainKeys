using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Orleans.TypedGrainKeys.Generators;

internal enum PartKind
{
    String,
    Guid,
    Int32,
    Int64,
    Boolean,
    DateTime,
    DateTimeOffset,
    DateOnly,
    TimeOnly,
    TimeSpan,
    Enum,
    Nested
}

/// <summary>One positional parameter of a key, in declaration order.</summary>
/// <param name="MemberName">The property name, escaped with <c>@</c> when it is a C# keyword.</param>
/// <param name="TypeName">The fully qualified type name, used for enums and nested keys.</param>
internal sealed record PartModel(string MemberName, PartKind Kind, string TypeName);

internal sealed record KeyModel(
    string? Namespace,
    string Name,
    string FullyQualifiedName,
    bool IsValueType,
    bool IsReadOnly,
    EquatableArray<PartModel> Parts);

internal sealed record DiagnosticModel(DiagnosticDescriptor Descriptor, Location? Location, EquatableArray<string> Arguments)
{
    public Diagnostic ToDiagnostic() => Diagnostic.Create(Descriptor, Location, Arguments.Cast<object>().ToArray());
}

internal sealed record ParseResult(KeyModel? Key, EquatableArray<DiagnosticModel> Diagnostics);

/// <summary>An immutable array with value equality, so the incremental pipeline can cache its results.</summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T> where T : IEquatable<T>
{
    private readonly T[]? _items;

    public EquatableArray(IEnumerable<T> items) => _items = items.ToArray();

    public int Count => _items?.Length ?? 0;

    public bool Equals(EquatableArray<T> other)
    {
        if (Count != other.Count)
        {
            return false;
        }

        for (var i = 0; i < Count; i++)
        {
            if (!_items![i].Equals(other._items![i]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var item in this)
        {
            hash = unchecked(hash * 31 + item.GetHashCode());
        }

        return hash;
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)(_items ?? Array.Empty<T>())).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
