#!/usr/bin/env dotnet
#:include ../generic/Test.cs

using System.Diagnostics;

Test.From = "Part III";
// Test.Until = "Part III";
// https://everybody.codes/event/2026/quests/16
Test.Run((notes, part, expected) => {
    // Console.Clear();
    int copies = part switch {
        1 => 1,
        2 => 10,
        _ => 100
    };
    copies = 3;
    var tablets = new Tablets(string.Concat(Enumerable.Repeat(notes, copies)));
    tablets.GroupByShade();
    long result = part switch {
        1 => tablets.Flips,
        _ => tablets.Flipped
    };
    Assert.ActualEqualsExpected(result, expected);
});

class Tablets(string notes) {
    readonly char[] shades = [.. notes];
    readonly char[] temp = new char[notes.Length];
    int shadeIndex, first;
    (int start, int end, int length) lastGroup;
    public int Flips { get; private set; }
    public long Flipped { get; private set; }

    Stopwatch stopwatch = null!;

    public void GroupByShade() {
        stopwatch = Stopwatch.StartNew();
        while (true) {
            if (!TryGroup()) {
                return;
            }
            // Test.Log($"grouped {shades[shadeIndex]}");
            FindNextShade();
        }

        void FindNextShade() {
            char shade = shades[shadeIndex];
            while (shades[shadeIndex] == shade) {
                shadeIndex++;
            }
        }
    }

    readonly Dictionary<char, ConsoleColor> colors = new() {
        { 'R', ConsoleColor.Red },
        { 'G', ConsoleColor.Green },
        { 'B', ConsoleColor.Blue },
        { 'Y', ConsoleColor.Yellow }
    };

    float progressLimit;
    const float progressIncrement = 0.01f;

    bool TryGroup() {
        first++;
        lastGroup.end = shades.Length - 1;
        while (true) {
            FindEdges();
            if (first == shades.Length) {
                return false;
            }
            if (first > lastGroup.end) {
                break;
            }
            if (CanSkip(out int next)) {
                Skip(next);
            } else {
                Flip();
            }
            float progress = first / (float)shades.Length;
            if (progress > progressLimit) {
                Console.WriteLine($"{progress * 100:F}%, took: {stopwatch.Elapsed}");
                progressLimit += progressIncrement;
                stopwatch = Stopwatch.StartNew();
            }
        }
        return true;
    }

    /// Find edges of the range that will be flipped
    void FindEdges() {
        char shade = shades[shadeIndex];
        // Log();
        // LogFL("before");
        while (first < shades.Length && shades[first] == shade) {
            first++;
        }
        while (shades[lastGroup.end] != shade) {
            lastGroup.end--;
        }
        // LogFL("after");
    }

    /// We can skip big flip if next group isn't last group
    bool CanSkip(out int next) {
        next = first;
        char shade = shades[shadeIndex];
        while (next < shades.Length && shades[next] != shade) {
            next++;
        }
        if (next >= lastGroup.end) {
            return false;
        }
        lastGroup.length = 0;
        for (int i = lastGroup.end; shades[i] == shade; i--) {
            lastGroup.length++;
        }
        lastGroup.start = lastGroup.end - lastGroup.length + 1;
        return next < lastGroup.start;
    }

    /// Skip big flip and instead do small flip, and move some elements
    void Skip(int next) {
        int flipLength = next - first;
        Array.Copy(shades, first, temp, 0, flipLength);
        Array.Reverse(temp, 0, flipLength);
        Array.Copy(shades, lastGroup.start, temp, flipLength, lastGroup.length);
        int middleLength = lastGroup.start - next;
        Array.Copy(shades, next, shades, first + lastGroup.length, middleLength);
        Array.Copy(temp, flipLength, shades, first, lastGroup.length);
        Array.Copy(temp, 0, shades, lastGroup.end + 1 - flipLength, flipLength);
        int length = lastGroup.end + 1 - first;
        Flipped += length;
        Flipped += middleLength;
        Flips += 2;
        first += lastGroup.length;
        // Test.Log((Flips, Flipped, "skipped"));
    }

    void Log() {
        char s = shades[0];
        Console.ForegroundColor = colors[s];
        Console.Write(s);
        for (var i = 1; i < shades.Length; i++) {
            if (shades[i] != s) {
                s = shades[i];
                Console.ForegroundColor = colors[s];
            }
            Console.Write(s);
        }
        Console.WriteLine();
        Test.Log(new string(shades.ToArray()), console: false);
    }

    void LogFL(string message) {
        try {
            Console.CursorLeft = first;
            Console.ForegroundColor = colors[shades[first]];
            Console.Write("f");
            Console.CursorLeft = lastGroup.end;
            Console.ForegroundColor = colors[shades[lastGroup.end]];
            Console.Write("l");
            Console.CursorLeft = shades.Length;
            Console.ResetColor();
            message = $"f: {first}, l: {lastGroup.end}, {message}";
            Console.WriteLine($" {message}");
            var log = new char[shades.Length + 1];
            Array.Fill(log, ' ');
            log[first] = 'f';
            log[lastGroup.end] = 'l';
            Test.Log(new string(log) + message, console: false);
        } catch { }
    }

    void Flip() {
        int length = lastGroup.end + 1 - first;
        Array.Reverse(shades, first, length);
        Flips++;
        Flipped += length;
        // Test.Log((Flips, Flipped, "flipped"));
    }
}
