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
            FindFirstAndLast();
            if (first == shades.Count) {
                return false;
            }
            if (first > last) {
                break;
            }
            if (!TrySkip()) {
                Flip();
            }
        }
        return true;

        void FindFirstAndLast() {
            char shade = shades[shadeIndex];
            while (first < shades.Count && shades[first] == shade) {
                first++;
            }
            while (shades[last] != shade) {
                last--;
            }
        }
    }

    bool TrySkip() {
        char shade = shades[shadeIndex];
        int next = first;
        while (next < shades.Count && shades[next] != shade) {
            next++;
        }
        if (next >= last) {
            return false;
        }
        List<char> shadeGroup = [];
        for (int i = last; shades[i] == shade; i--) {
            shadeGroup.Add(shades[i]);
        }
        int groupStart = last - shadeGroup.Count + 1;
        if (next >= groupStart) {
            return false;
        }
        int length = last + 1 - first;
        Flipped += length;
        List<char> toFlip = [];
        for (int i = first; i < next; i++) {
            toFlip.Add(shades[i]);
        }
        shades.RemoveRange(groupStart, shadeGroup.Count);
        length = groupStart - next;
        Flipped += length;
        for (int i = toFlip.Count - 1, j = groupStart; i >= 0; i--, j++) {
            char flipped = toFlip[i];
            shades.Insert(j, flipped);
        }
        shades.RemoveRange(first, toFlip.Count);
        shades.InsertRange(first, shadeGroup);
        first += shadeGroup.Count;
        Flips += 2;
        return true;
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
