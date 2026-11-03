using System.Diagnostics;

static class Perf {
    static readonly Dictionary<string, (long start, long ticks, long calls)> blocks = [];

    public static void Start(string block) {
        var value = blocks.GetValueOrDefault(block);
        value.start = Stopwatch.GetTimestamp();
        blocks[block] = value;
    }

    public static void Stop(string block) {
        var value = blocks.GetValueOrDefault(block);
        value.ticks += Stopwatch.GetTimestamp() - value.start;
        value.calls++;
        blocks[block] = value;
    }

    public static void Report() {
        foreach ((string block, (long _, long ticks, long calls)) in blocks) {
            var total = Stopwatch.GetElapsedTime(0, ticks);
            Console.Error.WriteLine(
                    $"{block}: {total.TotalMilliseconds:F1}ms total, {calls} calls, "
                    + $"{total.TotalMilliseconds * 1000 / calls:F2}µs/call");
        }
    }
}
