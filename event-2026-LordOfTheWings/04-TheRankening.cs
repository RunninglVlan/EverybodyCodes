// https://everybody.codes/event/2026/quests/4

const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
{
    const int part = 3;
    var results = notes.Split(Environment.NewLine).Select(Result.From).ToArray();
    var ranking = part != 3 ? TimeRanking(results) : PointRanking(results);
    int checksum = 0;
    (int place, int score) last = (0, 0);
    int place = 1;
    foreach (int score in ranking) {
        bool isTiedWithPrevious = score == last.score;
        int currentPlace = isTiedWithPrevious ? last.place : place;
        checksum += score * currentPlace;
        place++;
        last = (currentPlace, score);
    }
    Console.WriteLine(checksum);
}
return;

IEnumerable<int> TimeRanking(Result[] allResults) {
    return allResults
            .GroupBy(result => result.Id)
            .Select(results => (parts: results.Count(), totalTime: results.Sum(it => it.Time)))
            .OrderByDescending(it => it.parts)
            .ThenBy(it => it.totalTime)
            .Select(it => it.totalTime);
}

IEnumerable<int> PointRanking(Result[] allResults) {
    const int pointCoefficient = 50;
    int ids = allResults.Select(it => it.Id).Max();
    int[] scores = new int[ids];
    int quests = allResults.Select(it => it.Quest).Max();
    int parts = allResults.Select(it => it.Part).Max();
    for (int quest = 1; quest <= quests; quest++) {
        var questResults = allResults.Where(it => it.Quest == quest).ToArray();
        for (int part = 1; part <= parts; part++) {
            var ranking = questResults
                    .Where(it => it.Part <= part)
                    .GroupBy(it => it.Id)
                    .Where(results => results.Any(it => it.Part == part))
                    .Select(results => (id: results.Key, totalTime: results.Sum(it => it.Time)))
                    .OrderBy(it => it.totalTime)
                    .ToArray();
            int lastPoints = 0;
            for (int i = 0, points = pointCoefficient * part; i < ranking.Length; i++, points--) {
                var result = ranking[i];
                bool isTiedWithPrevious = i > 0 && result.totalTime == ranking[i - 1].totalTime;
                int currentPoints = isTiedWithPrevious ? lastPoints : points;
                if (currentPoints <= 0) {
                    break;
                }
                scores[result.id - 1] += currentPoints;
                lastPoints = currentPoints;
            }
        }
    }
    return scores.OrderByDescending(it => it);
}

readonly record struct Result(int Id, int Quest, int Part, int Time) {
    public static Result From(string result) {
        string[] parts = result.Split(' ');
        string id = parts[0].Split('=').Last();
        string quest = parts[1].Split('=').Last();
        string part = parts[2].Split('=').Last();
        string time = parts[3].Split('=').Last();
        return new Result(int.Parse(id), int.Parse(quest), int.Parse(part), int.Parse(time));
    }
}
