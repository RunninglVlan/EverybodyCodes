// https://everybody.codes/event/2026/quests/6

const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
const int part = 3;
Dictionary<string, HashSet<string>> ingredientsMadeFrom = [];
Dictionary<string, HashSet<string>> ingredientsUsedIn = [];
foreach (string[] parts in notes.Split(Environment.NewLine).Select(it => it.Split(':'))) {
    string usedInIngredient = parts.First();
    HashSet<string> madeFrom = [.. parts.Last().Split(',')];
    ingredientsMadeFrom.Add(usedInIngredient, madeFrom);
    foreach (string ingredient in madeFrom) {
        if (!ingredientsUsedIn.TryGetValue(ingredient, out var usedIn)) {
            usedIn = [];
            ingredientsUsedIn.Add(ingredient, usedIn);
        }
        usedIn.Add(usedInIngredient);
    }
}
int count = NullIngredients(part switch {
    1 => ingredientsMadeFrom,
    _ => ingredientsUsedIn
});
Console.WriteLine(count);
return;

int NullIngredients(Dictionary<string, HashSet<string>> ingredients) {
    var nullTypes = ingredients
            .SelectMany(it => new[] { it.Key }.Concat(it.Value))
            .Where(it => it.Contains("NULL"))
            .ToHashSet();
    var queue = new Queue<string>();
    var result = new HashSet<string>();
    Dictionary<string, int> nullIngredients = [];
    foreach (string nullType in nullTypes) {
        result.Clear();
        foreach (string next in ingredients[nullType]) {
            queue.Enqueue(next);
            result.Add(next);
        }
        while (queue.TryDequeue(out string? current)) {
            if (!ingredients.TryGetValue(current, out var nextIngredients)) {
                continue;
            }
            foreach (string next in nextIngredients.Where(it => it != nullType && result.Add(it))) {
                queue.Enqueue(next);
            }
        }
        foreach (string ingredient in result) {
            nullIngredients[ingredient] = nullIngredients.GetValueOrDefault(ingredient) + 1;
        }
    }
    int min = nullTypes.Count == 1 ? 1 : 2;
    return nullIngredients.Count(it => it.Value >= min);
}
