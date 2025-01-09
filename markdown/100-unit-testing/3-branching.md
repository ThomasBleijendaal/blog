---
theme: Unit Testing
title: Branching code with side effects
visible: false
---

Most methods that we test are methods that accept some input, have some side effects, return some output, and do that with some logic that is based on the input and data from the side effects. This branching is the source of a lot of testing trouble. Let's consider the following:

```csharp
public class Subject
{
    private readonly IDependency1 _dependency1;
    private readonly IDependency2 _dependency2;

    public Subject(IDependency1 dependency1, IDependency2 dependency2)
    {
        _dependency1 = dependency1;
        _dependency2 = dependency2;
    }

    public async Task<string> MethodAsync(string someInput)
    {
        var result = await _dependency1.GetSomethingAsync(someInput)
            ?? throw new InvalidOperationException();

        Task? specialTask;

        if (result.Case is Case.One)
        {
            specialTask = _dependency2.SaveSomethingAsync("special", result.Case);
        }
        else
        {
            var subResult = await _dependency1.GetSomethingAsync(result.Output);
            specialTask = null;
            if (subResult is not null)
            {
                result = subResult;
            }
        }

        await _dependency2.SaveSomethingAsync(someInput, result.Case);

        if (specialTask is not null)
        {
            await specialTask;
        }

        return result.Output;
    }
}
```

If we simply test the input and output, we need to simulate `_dependency1.GetSomethingAsync` to return something, and have `_dependency2.SaveSomethingAsync` return a completed task. That can be done in a single test. 

Let's check how many tests we would need to test all the branches and outcomes.

- `_dependency1.GetSomethingAsync` throws.
- `_dependency1.GetSomethingAsync` returns null.
- `_dependency1.GetSomethingAsync` returns `Result(Case.One, "root")`, `_dependency2.SaveSomethingAsync` both invocations throw.
- `_dependency1.GetSomethingAsync` returns `Result(Case.One, "root")`, `_dependency2.SaveSomethingAsync` first invocation throws, second invocation completes.
- `_dependency1.GetSomethingAsync` returns `Result(Case.One, "root")`, `_dependency2.SaveSomethingAsync` both invocations complete. Method returns `"root"`.
- `_dependency1.GetSomethingAsync` first invocation returns `Result(not Case.One, "root")`, second throws.
- `_dependency1.GetSomethingAsync` first invocation returns `Result(not Case.One, "root")`, second returns null, `_dependency2.SaveSomethingAsync` throws.
- `_dependency1.GetSomethingAsync` first invocation returns `Result(not Case.One, "root")`, second returns null, `_dependency2.SaveSomethingAsync` completes. Method return `"root"`.
- `_dependency1.GetSomethingAsync` first invocation returns `Result(not Case.One, "root")`, second returns `Result(Any, "nested")`, `_dependency2.SaveSomethingAsync` completes. Method returns `"nested"`.

So a method of 40 lines result in 9 test cases resulting in probably 150 lines of tests. If we add a tiny extra `else if (result.Case is Case.Two)` branch the amount possible options increases easily, even if this example is pretty simple. We might also want to add some test cases just to verify if the correct data is passed around. Imagine if you have to do such an exercise for [this thing](https://github.com/ThomasBleijendaal/RapidCMS/blob/master/src/RapidCMS.Core/Dispatchers/Form/GetEntitiesDispatcher.cs#L45).

Furthermore, the problem with the example method from above is that developers will probably look a that method and think that they can improve it, move some parts into private methods and come up with something like this:

```csharp
public class Subject
{
    private readonly IDependency1 _dependency1;
    private readonly IDependency2 _dependency2;

    public Subject(IDependency1 dependency1, IDependency2 dependency2)
    {
        _dependency1 = dependency1;
        _dependency2 = dependency2;
    }

    public async Task<string> MethodAsync(string someInput)
    {
        var (specialTask, result) = await GetSomethingAsync(someInput) switch
        {
            { Case: Case.One } input => (_dependency2.SaveSomethingAsync("special", input.Case), input),
            { } input => (Task.CompletedTask, await GetSomethingNestedAsync(input))
        };

        await Task.WhenAll(
            _dependency2.SaveSomethingAsync(someInput, result.Case),
            specialTask);

        return result.Output;
    }

    private async Task<Result> GetSomethingAsync(string someInput)
        => await _dependency1.GetSomethingAsync(someInput)
            ?? throw new InvalidOperationException();

    private async Task<Result> GetSomethingNestedAsync(Result result) 
        => (await _dependency1.GetSomethingAsync(result.Output)) ?? result;
}
```

You could have some opinion about some of the choices here, like that whole `switch` expression, but that's beside the point. My point here is that, although this method looks nicer, the test complexity is exactly the same. We used some nifty pattern matching or some simple private methods to express some logic more cleanly, we haven't improved anything about how to test this. And this is the problem that I want to fix, how can I improve this method to make unit testing more easy?

There are several strategies:

#### Just test the happy flow
A lot of things become easy if I just ignore the details. This can be a strategy, but's hardly a solution.

#### Ignore the side effects
This simplifies the tests greatly, but in most .NET applications the interesting bits happen with the side effects. Ignoring those greatly reduces the quality of my tests, as I no longer validate whether the correct data is moved around inside those methods, or given in the correct way to the dependency.

#### Move logic somewhere else
If I make some of the logic no longer part of this method, testing could become simplified, but I have to be a bit careful about it. If I move it to a private method or a static helper, it doesn't improve. If I move it to another dependency I have to make sure I can mock that dependency. If I cannot mock it, its like a private method, and is still part of the test complexity.
