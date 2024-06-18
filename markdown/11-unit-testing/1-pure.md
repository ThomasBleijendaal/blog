---
theme: Unit Testing
title: Pure functions
visible: true
---

Pure functions are a unit testers dream, as only the input completely dictate the behavior of the function. There is no need for a complicated setup, as there is no state to simulate. Take the following function for example:

```csharp
public static class PureFunction
{
    public static int Add(int a, int b)
    {
        if (a < 0)
        {
            return b;
        }
        else if (b < 0)
        {
            return a;
        }
        else
        {
            return a + b;
        }
    }
}
```

This can easily be tested using:

```csharp
[TestCase(-1, 2, 2)]
[TestCase(1, -2, 1)]
[TestCase(1, 2, 3)]
public void Test1(int a, int b, int expectedOutput)
{
    PureFunction.Add(a, b).Should().Be(expectedOutput);
}
```

When testing these kind of functions it's good to be aware of what inputs you test and make you handle most expected cases. For example, `[TestCase(-1, -2, -2)]` should be added as that kind of test case was not present yet. `[TestCase(2147483647, 2, -2147483647)]` could also be added, just to indicate that integer rollover is allowed.

The only way to complicate the test cases when the behavior changes and some test cases start throwing exceptions. These exceptions are a bit more difficult to test, as compared to simple output testing. Replacing `return a + b` with `return checked(a + b)` will blow up a test case that causes a `OverflowException`, which requires some rework:

```csharp
[TestCase(2147483647, 2)]
[TestCase(1, 2147483647)]
public void Test2(int a, int b)
{
    var act = () => PureFunction.Add(a, b);
    act.Should().Throw<OverflowException>();
}
```

So pure functions are easy, but pure functions don't do much. Let's look at something marginally more interesting. 
