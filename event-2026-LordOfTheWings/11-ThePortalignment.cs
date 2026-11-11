// https://everybody.codes/event/2026/quests/11

const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
const int part = 3;
(int sideNeighbors, int crystalConnections) = part switch {
    1 => (1, 0),
    2 => (3, 1),
    _ => (3, 4)
};
var portals = new Portals(sideNeighbors, crystalConnections);
var graph = portals.Build(notes);
int resonance = portals.Resonance(graph);
Console.WriteLine(resonance);

class Portals(int sideNeighbors, int crystalConnections) {
    readonly List<(int a, int b)> shortcuts = [];

    public Portal[] Build(string notes) {
        string[] data = notes.Split(Environment.NewLine);
        string[] portalsData = data[0].Split(' ');
        int portalCount = int.Parse(portalsData.Last());
        var graph = new Portal[portalCount];
        for (var id = 0; id < portalCount; id++) {
            var portal = new Portal([]);
            graph[id] = portal;
            for (var i = 1; i <= sideNeighbors; i++) {
                portal.Connections.Add(Wrap(id - i));
                portal.Connections.Add(Wrap(id + i));
            }
        }
        for (var i = 2; i < data.Length; i++) {
            AddShortcut(data[i]);
        }
        return graph;

        int Wrap(int id) => (id + portalCount) % portalCount;

        void AddShortcut(string shortcutData) {
            int[] shortcut = [.. shortcutData.Split(' ').Select(int.Parse).Select(it => it - 1)];
            shortcuts.Add((shortcut.First(), shortcut.Last()));
        }
    }

    public int Resonance(Portal[] graph) {
        int strain = Strain(graph);
        foreach ((int a, int b) in shortcuts) {
            Portal.Connect(graph, a, b);
        }
        int strainWithShortcuts = crystalConnections switch {
            0 => Strain(graph),
            1 => MinimizeStrainWith1Extra(graph),
            _ => MinimizeStrainWith4Extras(graph)
        };
        return strain - strainWithShortcuts;
    }

    static int Strain(Portal[] graph) {
        var result = 0;
        var distances = new int?[graph.Length];
        var queue = new Queue<int>();
        for (var start = 0; start < graph.Length; start++) {
            Array.Fill(distances, null);
            distances[start] = 0;
            queue.Enqueue(start);
            while (queue.TryDequeue(out int current)) {
                foreach (int next in graph[current].Connections.Where(next => distances[next] == null)) {
                    distances[next] = distances[current] + 1;
                    result += distances[next]!.Value;
                    queue.Enqueue(next);
                }
            }
        }
        return result;
    }

    static int MinimizeStrainWith1Extra(Portal[] graph) {
        var result = int.MaxValue;
        Parallel.For(0, graph.Length, i => {
            var portals = Portal.Copy(graph);
            result = Math.Min(Search(portals, i, 1, 1, []), result);
        });
        return result;
    }

    static int MinimizeStrainWith4Extras(Portal[] graph) {
        const int extras = 4;
        var result = int.MaxValue;
        int crystalDistance = graph.Length / (extras * 2);
        Parallel.For(0, crystalDistance, i => {
            var portals = Portal.Copy(graph);
            result = Math.Min(Search(portals, i, crystalDistance, extras, []), result);
        });
        return result;
    }

    static int Search(Portal[] graph, int start, int step, int pairsLeft, HashSet<int> used) {
        if (pairsLeft == 0) {
            return Strain(graph);
        }
        int first = start;
        while (used.Contains(first)) {
            first += step;
        }
        used.Add(first);
        var best = int.MaxValue;
        var firstPortal = graph[first];
        for (int second = first + step; second < graph.Length; second += step) {
            if (firstPortal.Connections.Contains(second) || !used.Add(second)) {
                continue;
            }
            Portal.Connect(graph, first, second);
            best = Math.Min(Search(graph, first, step, pairsLeft - 1, used), best);
            Portal.Disconnect(graph, first, second);
            used.Remove(second);
        }
        used.Remove(first);
        return best;
    }
}

readonly record struct Portal(List<int> Connections) {
    public static void Connect(Portal[] graph, int a, int b) {
        graph[a].Connections.Add(b);
        graph[b].Connections.Add(a);
    }

    public static void Disconnect(Portal[] graph, int a, int b) {
        graph[a].Connections.Remove(b);
        graph[b].Connections.Remove(a);
    }

    public static Portal[] Copy(Portal[] graph) {
        return [.. graph.Select(it => new Portal([.. it.Connections]))];
    }
}
