---
theme: NSubstitute
title: Predicate matcher
visible: true
---

Replacing the expression tree in `Arg.Is` is pretty easy. All it takes is:

```csharp
public static class Arg
{
    public static ref T Is<T>(Predicate<T?> predicate, [System.Runtime.CompilerServices.CallerArgumentExpression("predicate")] string predicateExpression = "")
    {
        return ref ArgumentMatcher.Enqueue<T>(new PredicateArgumentMatcher<T>(predicate, predicateExpression))!;
    }

    private sealed class PredicateArgumentMatcher<T>(Predicate<T?> predicate, string predicateExpression) : IArgumentMatcher<T>
    {
        private readonly string _predicateDescription = predicateExpression;
        private readonly Predicate<T?> _predicate = predicate;

        public bool IsSatisfiedBy(T? argument) => _predicate((T?)argument!);

        public override string ToString() => _predicateDescription;
    }
}
```

And now `Arg.Is` takes a real `Predicate<T>` (which is a `Func<T, bool>`) instead of a `Expression<Predicate<T>>`. Much better as all C# is allowed now, instead of some subset.
