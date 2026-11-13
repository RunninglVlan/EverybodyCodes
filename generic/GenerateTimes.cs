// Initially generated with the help of Claude AI.
// Later modified manually and with the help of GitHub Copilot Fusion.
//
// Turns the raw results text into a Markdown table of local times and solution LOC.
// LOC excludes blank lines and lines whose first non-whitespace characters are //.
//
// Usage:
//   dotnet run Report.cs  -> reads the times file and writes the table next to it (see the paths below)
//
// The "Local Time" column in the raw data is cumulative within a quest, so the time
// of each part is the difference between it and the previous part.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

const string folder = "../event-2026-LordOfTheWings/";
const string inputPath = folder + "times.txt";
const string outputPath = folder + "README.md";

string[] partNames = ["I", "II", "III"];

// A part line starts with an optional quest number, then the part name (I, II or III).
var partLine = new Regex(@"^\s*(?:(?<quest>\d+)\s+)?(?<part>III|II|I)\s");

// A duration like "1h 06m 50s 199ms" (days, hours, minutes and seconds are optional).
var durationPattern = new Regex(@"(?:\d+d\s+)?(?:\d+h\s+)?(?:\d+m\s+)?(?:\d+s\s+)?\d+ms");

// Cumulative local time of every part, grouped by quest number.
var quests = new SortedDictionary<int, List<TimeSpan>>();
TimeSpan? reportedTotal = null;
var currentQuest = 0;

foreach (string line in File.ReadLines(inputPath)) {
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

int partCount = quests.Values.Max(times => times.Count);
var questLocs = quests.Keys.ToDictionary(questNumber => questNumber, CountLinesOfCode);
var table = new StringBuilder();

table.AppendLine("### Local times");
table.AppendLine();
table.AppendLine($"| Quest | {string.Join(" | ", partNames.Take(partCount))} | Total | Quest by Time | LOC |");
table.AppendLine($"|{string.Join("|", Enumerable.Repeat("---", partCount + 4))}|");

var questTotals = quests.Values.Select(times => times[^1]).OrderBy(it => it).ToList();
var orderedLocs = questLocs.Values.OrderBy(loc => loc).ToList();

var allQuestsTotal = TimeSpan.Zero;
foreach ((int questNumber, var cumulativeTimes) in quests) {
    var partTimes = cumulativeTimes
            .Select((time, index) => index == 0 ? time : time - cumulativeTimes[index - 1])
            .ToList();
    var orderedTimes = partTimes.OrderBy(it => it).ToList();
    var partCells = partTimes
            .Select(time => $"{MarkerFor(time, orderedTimes)} {Format(time)}".Trim())
            .Concat(Enumerable.Repeat("", partCount - partTimes.Count));
    var questTotal = cumulativeTimes[^1];
    allQuestsTotal += questTotal;
    string totalCell = Format(questTotal);
    int totalIndex = questTotals.IndexOf(questTotal);
    string rankCell = $"{MarkerFor(questTotal, questTotals)} {totalIndex + 1}".Trim();
    int loc = questLocs[questNumber];
    string locCell = $"{MarkerFor(loc, orderedLocs)} {loc}".Trim();
    table.AppendLine($"| {questNumber} | {string.Join(" | ", partCells)} | {totalCell} | {rankCell} | {locCell} |");
}

table.AppendLine("| **All** |"
        + string.Concat(Enumerable.Repeat(" |", partCount))
        + $" **{Format(allQuestsTotal)}** | | {questLocs.Values.Sum()} |");
table.AppendLine();
table.AppendLine(
        "🟢 lowest/fastest · 🟡 · 🟠 · 🔴 highest/slowest (colors show relative rank; parts are compared within their quest, totals and LOC across quests)");

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

int CountLinesOfCode(int questNumber) {
    string[] files = Directory.GetFiles(folder, $"{questNumber:00}-*.cs");
    if (files.Length != 1) {
        throw new InvalidOperationException(
                $"Expected one solution file for quest {questNumber}, found {files.Length}.");
    }
    return File.ReadLines(files[0])
            .Count(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("//"));
}

static TimeSpan ParseDuration(string text) {
    int days = 0, hours = 0, minutes = 0, seconds = 0, milliseconds = 0;
    foreach (Match unit in Regex.Matches(text, @"(\d+)(ms|d|h|m|s)")) {
        int amount = int.Parse(unit.Groups[1].Value);
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

// Marks a value against the other values it should be judged against (part times, quest totals,
// or LOC across quests).
// 🟢 fastest, 🔴 slowest, 🟠 everything in between (no marker if there is nothing to compare with).
[SuppressMessage("ReSharper", "ConvertIfStatementToReturnStatement")]
static string MarkerFor<T>(T value, List<T> orderedValues) where T : IComparable<T> {
    var distinctValues = orderedValues.Distinct().ToList();
    if (distinctValues.Count < 2) {
        return "";
    }
    string[] markers = ["🟢", "🟡", "🟠", "🔴"];
    int rank = distinctValues.FindIndex(it => value.CompareTo(it) == 0);
    var markerIndex = (int)Math.Round((double)rank * (markers.Length - 1) / (distinctValues.Count - 1));
    return markers[markerIndex];
}
