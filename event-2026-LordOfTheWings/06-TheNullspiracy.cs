// https://everybody.codes/event/2026/quests/6

const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
const int part = 3;
Dictionary<string, HashSet<string>> ingredients = [];
foreach (string[] parts in notes.Split(Environment.NewLine).Select(it => it.Split(':'))) {
    ingredients.Add(parts.First(), [.. parts.Last().Split(',')]);
}
int count = NullIngredients(part switch {
    1 => MadeFrom,
    _ => UsedIn
});
Console.WriteLine(count);
return;

IEnumerable<string> MadeFrom(string current) {
    if (!ingredients.TryGetValue(current, out var madeFrom)) {
        yield break;
    }
    foreach (string next in madeFrom) {
        yield return next;
    }
}

IEnumerable<string> UsedIn(string current) {
    foreach ((string next, _) in ingredients.Where(it => it.Value.Contains(current))) {
        yield return next;
    }
}

int NullIngredients(Func<string, IEnumerable<string>> nextIngredients) {
    var nullTypes = ingredients
            .SelectMany(it => new[] { it.Key }.Concat(it.Value))
            .Where(it => it.Contains("NULL"))
            .ToHashSet();
    var queue = new Queue<string>();
    var result = new HashSet<string>();
    Dictionary<string, int> nullIngredients = [];
    foreach (string nullType in nullTypes) {
        result.Clear();
        foreach (string next in nextIngredients(nullType)) {
            queue.Enqueue(next);
            result.Add(next);
        }
        while (queue.TryDequeue(out string? current)) {
            foreach (string next in nextIngredients(current)) {
                if (next != nullType && result.Add(next)) {
                    queue.Enqueue(next);
                }
            }
        }
        foreach (string ingredient in result) {
            nullIngredients[ingredient] = nullIngredients.GetValueOrDefault(ingredient) + 1;
        }
    }
    int min = nullTypes.Count == 1 ? 1 : 2;
    return nullIngredients.Count(it => it.Value >= min);
}
