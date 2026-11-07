// https://everybody.codes/event/2026/quests/9

const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
const int part = 3;
GrowthCounter counter = part switch {
    1 => new CountElements(),
    _ => new CountLeaves()
};
long totalTime = notes.Split(Environment.NewLine).Select(Plant.From).Sum(counter.Grow);
Console.WriteLine(totalTime);

abstract class GrowthCounter {
    protected Dictionary<long, long> branchesBySize = null!;
    Dictionary<long, long> nextBranchesBySize = null!;
    protected long branchingPoints;
    protected abstract long Size { get; }

    public long Grow(Plant plant) {
        branchesBySize = new Dictionary<long, long> { { 1, 1 } };
        nextBranchesBySize = [];
        branchingPoints = 0;
        long seconds = 1;
        for (long delta = 1; Size < plant.Limit; delta = NextDelta(plant.Growth)) {
            foreach ((long size, long count) in branchesBySize) {
                long branchSize = size + delta;
                if (branchSize < plant.Growth) {
                    AddBranches(branchSize, count);
                    continue;
                }
                long splitSize = (plant.Growth - 1) / 2;
                AddBranches(splitSize, count * 2);
                AddBranches(1, count);
                branchingPoints += count;
            }
            (branchesBySize, nextBranchesBySize) = (nextBranchesBySize, branchesBySize);
            nextBranchesBySize.Clear();
            seconds += delta;
        }
        return seconds;
    }

    void AddBranches(long size, long countDelta) {
        long countExisting = nextBranchesBySize.GetValueOrDefault(size);
        nextBranchesBySize[size] = countExisting + countDelta;
    }

    protected virtual long NextDelta(long maxSize) => 1;
}

class CountElements : GrowthCounter {
    protected override long Size {
        get {
            long branchElements = branchesBySize.Sum(it => it.Key * it.Value);
            return branchElements + branchingPoints;
        }
    }
}

class CountLeaves : GrowthCounter {
    protected override long Size => branchesBySize.Values.Sum() - branchingPoints;

    protected override long NextDelta(long maxSize) => maxSize - branchesBySize.Keys.Max();
}

record Plant(long Growth, long Limit) {
    public static Plant From(string properties) {
        string[] data = properties.Split(", ");
        string growth = data[1].Split('=').Last();
        string limit = data[2].Split('=').Last();
        return new Plant(long.Parse(growth), long.Parse(limit));
    }
}
