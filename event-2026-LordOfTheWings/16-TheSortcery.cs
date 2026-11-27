#!/usr/bin/env dotnet
#:include ../generic/Test.cs

Test.From = "Part III";
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
    readonly List<char> shades = [.. notes];
    int shadeIndex, first, last;
    public int Flips { get; private set; }
    public long Flipped { get; private set; }
    readonly List<char> toFlip = [];
    readonly List<char> lastGroup = [];

    public void GroupByShade() {
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

    bool TryGroup() {
        first++;
        last = shades.Count - 1;
        while (true) {
            FindEdges();
            if (first == shades.Count) {
                return false;
            }
            if (first > last) {
                break;
            }
            if (CanSkip(out int next)) {
                Skip(next);
            } else {
                Flip();
            }
            // Test.Log($"{first / (float)shades.Count * 100:F}%");
        }
        return true;
    }

    /// Find edges of the range that will be flipped
    void FindEdges() {
        char shade = shades[shadeIndex];
        // Log();
        // LogFL("before");
        while (first < shades.Count && shades[first] == shade) {
            first++;
        }
        while (shades[last] != shade) {
            last--;
        }
        // LogFL("after");
    }

    /// We can skip big flip if next group isn't last group
    bool CanSkip(out int next) {
        next = first;
        char shade = shades[shadeIndex];
        while (next < shades.Count && shades[next] != shade) {
            next++;
        }
        if (next >= last) {
            return false;
        }
        lastGroup.Clear();
        for (int i = last; shades[i] == shade; i--) {
            lastGroup.Add(shades[i]);
        }
        int groupStart = last - lastGroup.Count + 1;
        return next < groupStart;
    }

    /// Skip big flip and instead do small flip, and move some elements
    void Skip(int next) {
        toFlip.Clear();
        for (int i = next - 1; i >= first; i--) {
            toFlip.Add(shades[i]);
        }
        int groupStart = last - lastGroup.Count + 1;
        shades.RemoveRange(groupStart, lastGroup.Count);
        shades.InsertRange(groupStart, toFlip);
        shades.RemoveRange(first, toFlip.Count);
        shades.InsertRange(first, lastGroup);
        int length = last + 1 - first;
        Flipped += length;
        length = groupStart - next;
        Flipped += length;
        Flips += 2;
        first += lastGroup.Count;
        // Test.Log((Flips, Flipped, "skipped"));
    }

    void Log() {
        char s = shades[0];
        Console.ForegroundColor = colors[s];
        Console.Write(s);
        for (var i = 1; i < shades.Count; i++) {
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
            Console.CursorLeft = last;
            Console.ForegroundColor = colors[shades[last]];
            Console.Write("l");
            Console.CursorLeft = shades.Count;
            Console.ResetColor();
            message = $"f: {first}, l: {last}, {message}";
            Console.WriteLine($" {message}");
            var log = new char[shades.Count + 1];
            Array.Fill(log, ' ');
            log[first] = 'f';
            log[last] = 'l';
            Test.Log(new string(log) + message, console: false);
        } catch { }
    }

    void Flip() {
        int length = last + 1 - first;
        shades.Reverse(first, length);
        Flips++;
        Flipped += length;
        // Test.Log((Flips, Flipped, "flipped"));
    }
}
