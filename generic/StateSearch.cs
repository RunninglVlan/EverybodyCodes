using System.Diagnostics.CodeAnalysis;

class StateSearch<T>(IGame<T> game, int depthMax = 100) where T : IState<T>, IEquatable<T> {
    readonly HashSet<T> visited = [];

    /// DFS/Backtracking
    public bool TryFind(T start, out T end) {
        visited.Clear();
        visited.Add(start);
        return TryFind(start, 0, out end);
    }

    [SuppressMessage("ReSharper", "InvertIf")]
    bool TryFind(T current, int depth, out T end) {
        end = default!;
        if (game.HasReachedEnd(current)) {
            end = current;
            return true;
        }
        if (depth > depthMax) {
            return false;
        }
        var found = false;
        foreach (var next in game.Next(current)) {
            if (!visited.Add(next)) {
                continue;
            }
            if (!TryFind(next, depth + 1, out var nextBest)) {
                continue;
            }
            if (!found || nextBest.IsBetterThan(end)) {
                end = nextBest;
                found = true;
            }
        }
        return found;
    }
}

interface IGoal<in TState> {
    bool HasReachedEnd(TState state);
}

interface IGame<TState> : IGoal<TState> {
    IEnumerable<TState> Next(TState current);
}

interface IState<in T> where T : IState<T> {
    bool IsBetterThan(T other);
}
