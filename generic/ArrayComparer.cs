sealed class ArrayComparer<T> : IEqualityComparer<T[]> {
    public static readonly ArrayComparer<T> Instance = new();

    public bool Equals(T[]? a, T[]? b) {
        return a == b || a != null && b != null && a.SequenceEqual(b);
    }

    public int GetHashCode(T[] items) {
        var hash = new HashCode();
        foreach (var item in items) {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }
}
