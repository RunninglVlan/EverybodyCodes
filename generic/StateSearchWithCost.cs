class StateSearchWithCost<T>(IGameWithCost<T> game) where T : IEquatable<T> {
    readonly PriorityQueue<T, long> queue = new();
    readonly Dictionary<T, long> visited = [];

    /// Dijkstra
    public bool TryFind(T start, out (T value, long cost) end) {
        end = default!;
        queue.Clear();
        queue.Enqueue(start, 0);
        visited.Clear();
        visited.Add(start, 0);
        while (queue.TryDequeue(out var current, out long currentCost)) {
            if (game.HasReachedEnd(current)) {
                end = (current, currentCost);
                return true;
            }
            foreach ((var next, long cost) in game.NextWithCost(current)) {
                long newCost = currentCost + cost;
                if (newCost >= visited.GetValueOrDefault(next, long.MaxValue)) {
                    continue;
                }
                queue.Enqueue(next, newCost);
                visited[next] = newCost;
            }
        }
        return false;
    }
}

interface IGameWithCost<TState> : IGoal<TState> {
    IEnumerable<(TState, long cost)> NextWithCost(TState current);
}
