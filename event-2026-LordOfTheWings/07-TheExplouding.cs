// https://everybody.codes/event/2026/quests/7

const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
const int part = 3;
var cloudsOriginal = notes.Split(Environment.NewLine).Select(Cloud.From).ToDictionary(it => it.Id, it => it);
(int startForce, bool explode) = part switch {
    1 => (5, false),
    2 => (10, false),
    _ => (10, true)
};
Console.WriteLine(TotalEnergy());
return;

int TotalEnergy() {
    const int explosionEnergy = 1_000_000;
    const int explosionForce = 10;
    var maxEnergy = 0;
    Parallel.ForEach(cloudsOriginal.Keys, startId => {
        var clouds = cloudsOriginal.ToDictionary(it => it.Key, it => it.Value with { });
        Dictionary<(int id, int force), int> impulses = new() { { (startId, startForce), 1 } };
        Dictionary<(int id, int force), int> nextImpulses = [];
        while (impulses.Count > 0) {
            nextImpulses.Clear();
            foreach (((int id, int force), int count) in impulses) {
                if (!clouds.TryGetValue(id, out var cloud)) {
                    continue;
                }
                cloud.Energy += force * count;
                PropagateImpulse(cloud, force - 1, nextImpulses, count);
            }
            foreach (var cloud in clouds.Values.Where(it => explode && it.Energy > explosionEnergy)) {
                clouds.Remove(cloud.Id);
                PropagateImpulse(cloud, explosionForce, nextImpulses, 1);
            }
            (impulses, nextImpulses) = (nextImpulses, impulses);
        }
        int energy = clouds.Values.Sum(it => it.Energy);
        if (energy > maxEnergy) {
            maxEnergy = energy;
        }
    });
    return maxEnergy;
}

void PropagateImpulse(Cloud cloud, int force, Dictionary<(int, int), int> impulses, int count) {
    if (force == 0) {
        return;
    }
    foreach (int next in cloud.Connections) {
        var key = (next, force);
        int nextCount = impulses.GetValueOrDefault(key, 0);
        impulses[key] = nextCount + count;
    }
}

record Cloud(int Id, int[] Connections) {
    public int Energy { get; set; }

    public static Cloud From(string cloud) {
        string[] data = cloud.Split(", ");
        string id = data[0].Split('=').Last();
        string energy = data[1].Split('=').Last();
        int[] connections = [.. data[2].Split('=').Last().Split(',').Select(int.Parse)];
        return new Cloud(int.Parse(id), connections) { Energy = int.Parse(energy) };
    }
}
