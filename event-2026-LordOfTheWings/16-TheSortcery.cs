#!/usr/bin/env dotnet
#:include ../generic/Test.cs

Test.Until = "Part III";
// https://everybody.codes/event/2026/quests/16
Test.Run((notes, part, expected) => {
    int copies = part switch {
        1 => 1,
        2 => 10,
        _ => 100
    };
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

    public void GroupByShade() {
        while (true) {
            if (!TryGroup()) {
                return;
            }
            FindNextShade();
        }

        void FindNextShade() {
            char shade = shades[shadeIndex];
            while (shades[shadeIndex] == shade) {
                shadeIndex++;
            }
        }
    }

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
            if (CanSkip(out int next, out var lastGroup)) {
                Skip(next, lastGroup);
            } else {
                Flip();
            }
        }
        return true;
    }

    /// Find edges of the range that will be flipped
    void FindEdges() {
        char shade = shades[shadeIndex];
        while (first < shades.Count && shades[first] == shade) {
            first++;
        }
        while (shades[last] != shade) {
            last--;
        }
    }

    /// We can skip big flip if next group isn't last group
    bool CanSkip(out int next, out List<char> lastGroup) {
        next = first;
        lastGroup = null!;
        char shade = shades[shadeIndex];
        while (next < shades.Count && shades[next] != shade) {
            next++;
        }
        if (next >= last) {
            return false;
        }
        lastGroup = [];
        for (int i = last; shades[i] == shade; i--) {
            lastGroup.Add(shades[i]);
        }
        int groupStart = last - lastGroup.Count + 1;
        return next < groupStart;
    }

    /// Skip big flip and instead do small flip, and move some elements
    void Skip(int next, List<char> lastGroup) {
        List<char> toFlip = [];
        for (int i = first; i < next; i++) {
            toFlip.Add(shades[i]);
        }
        int groupStart = last - lastGroup.Count + 1;
        shades.RemoveRange(groupStart, lastGroup.Count);
        for (int i = toFlip.Count - 1, j = groupStart; i >= 0; i--, j++) {
            char flipped = toFlip[i];
            shades.Insert(j, flipped);
        }
        shades.RemoveRange(first, toFlip.Count);
        shades.InsertRange(first, lastGroup);
        int length = last + 1 - first;
        Flipped += length;
        length = groupStart - next;
        Flipped += length;
        Flips += 2;
        first += lastGroup.Count;
    }

    void Flip() {
        for (int f = first, l = last; f < l; f++, l--) {
            (shades[f], shades[l]) = (shades[l], shades[f]);
        }
        Flips++;
        int length = last + 1 - first;
        Flipped += length;
    }
}
