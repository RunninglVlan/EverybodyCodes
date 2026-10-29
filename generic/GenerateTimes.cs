// Generated with the help of Claude AI
//
// Turns the raw results text into a Markdown table of local times.
//
// Usage:
//   dotnet run Report.cs  -> reads the times file and writes the table next to it (see the paths below)
//
// The "Local Time" column in the raw data is cumulative within a quest, so the time
// of each part is the difference between it and the previous part.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

const string inputPath = "../event-2026-LordOfTheWings/times.txt";
const string outputPath = "../event-2026-LordOfTheWings/README.md";

string[] partNames = ["I", "II", "III"];

// A part line starts with an optional quest number, then the part name (I, II or III).
var partLine = new Regex(@"^\s*(?:(?<quest>\d+)\s+)?(?<part>III|II|I)\s");

// A duration like "1h 06m 50s 199ms" (days, hours, minutes and seconds are optional).
var durationPattern = new Regex(@"(?:\d+d\s+)?(?:\d+h\s+)?(?:\d+m\s+)?(?:\d+s\s+)?\d+ms");

// Cumulative local time of every part, grouped by quest number.
var quests = new SortedDictionary<int, List<TimeSpan>>();
TimeSpan? reportedTotal = null;
var currentQuest = 0;

foreach (var line in File.ReadLines(inputPath)) {
    if (line.TrimStart().StartsWith('-')) {
        // The summary line: its last duration is the total local time.
        reportedTotal = LastDurationIn(line);
        continue;
    }
    var match = partLine.Match(line);
    if (!match.Success) {
        continue; // header or empty line
    }
    if (match.Groups["quest"].Success) {
        currentQuest = int.Parse(match.Groups["quest"].Value);
    }
    if (!quests.TryGetValue(currentQuest, out var cumulativeTimes)) {
        quests[currentQuest] = cumulativeTimes = [];
    }
    cumulativeTimes.Add(LastDurationIn(line));
}

var partCount = quests.Values.Max(times => times.Count);
var table = new StringBuilder();

table.AppendLine($"| Quest | {string.Join(" | ", partNames.Take(partCount))} | Total |");
table.AppendLine($"|{string.Join("|", Enumerable.Repeat("---", partCount + 2))}|");

var questTotals = quests.Values.Select(times => times[^1]).ToList();

var allQuestsTotal = TimeSpan.Zero;
foreach (var (questNumber, cumulativeTimes) in quests) {
    var partTimes = cumulativeTimes
            .Select((time, index) => index == 0 ? time : time - cumulativeTimes[index - 1])
            .ToList();
    var partCells = partTimes
            .Select(time => $"{MarkerFor(time, partTimes)} {Format(time)}".Trim())
            .Concat(Enumerable.Repeat("", partCount - partTimes.Count));
    var questTotal = cumulativeTimes[^1];
    allQuestsTotal += questTotal;
    var totalCell = $"{MarkerFor(questTotal, questTotals)} {Format(questTotal)}".Trim();
    table.AppendLine($"| {questNumber} | {string.Join(" | ", partCells)} | {totalCell} |");
}

table.AppendLine("| **All** |"
        + string.Concat(Enumerable.Repeat(" |", partCount))
        + $" **{Format(allQuestsTotal)}** |");
table.AppendLine();
table.AppendLine(
        "🟢 fastest · 🟠 in between · 🔴 slowest (parts are compared within their quest, totals across all quests)");

if (reportedTotal is { } reported && reported != allQuestsTotal) {
    Console.Error.WriteLine(
            $"Warning: quests add up to {Format(allQuestsTotal)}, but the file says {Format(reported)}.");
}

File.WriteAllText(outputPath, table.ToString());
return;

TimeSpan LastDurationIn(string line) {
    var matches = durationPattern.Matches(line);
    return ParseDuration(matches[^1].Value);
}

static TimeSpan ParseDuration(string text) {
    int days = 0, hours = 0, minutes = 0, seconds = 0, milliseconds = 0;
    foreach (Match unit in Regex.Matches(text, @"(\d+)(ms|d|h|m|s)")) {
        var amount = int.Parse(unit.Groups[1].Value);
        switch (unit.Groups[2].Value) {
            case "d": days = amount; break;
            case "h": hours = amount; break;
            case "m": minutes = amount; break;
            case "s": seconds = amount; break;
            case "ms": milliseconds = amount; break;
        }
    }
    return new TimeSpan(days, hours, minutes, seconds, milliseconds);
}

// hh:mm:ss (milliseconds are dropped), with hours kept as total hours so long times never wrap around.
static string Format(TimeSpan time) =>
        $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";

// Compares a time with the other times it should be judged against (the parts of one quest,
// or the totals of all quests).
// 🟢 fastest, 🔴 slowest, 🟠 everything in between (no marker if there is nothing to compare with).
[SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
static string MarkerFor(TimeSpan time, List<TimeSpan> comparedTimes) {
    if (comparedTimes.Count < 2) {
        return "";
    }
    if (time == comparedTimes.Min()) {
        return "🟢";
    }
    if (time == comparedTimes.Max()) {
        return "🔴";
    }
    return "🟠";
}
