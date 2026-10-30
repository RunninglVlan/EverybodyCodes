Test usage example:
```csharp
Test.From = "Part III";
var grid = new Grid();
Test.Run((notes, part, expected) => {
    grid.Read(notes);
    int steps = grid.CountSteps(part switch {
        1 => ReachVocalBone(),
        2 => SurroundVocalBone(),
        _ => SurroundVocalBones()
    });
    Assert.ActualEqualsExpected(steps, expected);
});
```
And this is how test cases are structured:
```
TEST_CASE_NAME
INT_PART
NOTES
        # empty line between notes and expected
INT_EXPECTED
        # empty line between cases
Part III
3
1 2 3

6
```
Test result:
```
Part III
✅ 6
Test took 15ms

All tests took 15ms
```