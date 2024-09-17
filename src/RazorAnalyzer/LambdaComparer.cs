using System;
using System.Collections.Generic;

namespace RazorAnalyzer;

internal sealed class LambdaComparer<T> : IEqualityComparer<T>
{
    private readonly Func<T?, T?, bool> _equal;

    public LambdaComparer(Func<T?, T?, bool> equal)
    {
        _equal = equal;
    }

    public bool Equals(T? x, T? y) => _equal(x, y);

    public int GetHashCode(T obj) => throw new InvalidOperationException();
}