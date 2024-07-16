---
theme: NSubstitute
title: Verifying invocations
visible: true
---

Moqs `VerifyAll` and `VerifyNoOtherCalls` features are very useful in easily verifying all setups, and verifying whether some critical method was never invoked. Useful when testing stuff like payments, and wanting to be really sure some critical method is left alone in specific cases. NSubstitutes has something like it (`DidNotReceive`), but that is not completely similar. So let's see what it takes.

There is already a `Received.InOrder` static method that can serve as an example. `InOrder` check whether the given specification matches the observed calls. We can modify this to `NoOtherThan` and check whether other calls compared to the specification were observed. 

Ignoring some other details, the `NoOtherThan` method will be looking something like this:

```csharp
public void NoOtherThan(Action calls)
{
    var query = new GetAllCallsQuery(SubstitutionContext.Current.CallSpecificationFactory);
    SubstitutionContext.Current.ThreadContext.RunInQueryContext(calls, query);
    ReceivedNoOtherAssertion.Assert(query.Result());
}
```

There are several moving parts here.

- `calls` is the specification, this is the action performing all the calls the test expects.
- `GetAllCallsQuery` is an object that receive all actual performed calls, and store the specification.
- `ReceivedNoOtherAssertion` is a static object that compares the performed calls versus the expected calls, and will fail if they do not match.

## GetAllCallsQuery

This object is based on [Query](https://github.com/nsubstitute/NSubstitute/blob/main/src/NSubstitute/Core/Query.cs):

```csharp
public sealed class GetAllCallsQuery(ICallSpecificationFactory callSpecificationFactory) : IQuery, IAllResults
{
    private readonly List<CallSpecAndTarget> _querySpec = [];
    private readonly HashSet<ICall> _allCalls = [];

    public void RegisterCall(ICall call)
    {
        var target = call.Target();
        var callSpecification = callSpecificationFactory.CreateFrom(call, MatchArgs.AsSpecifiedInCall);

        _querySpec.Add(new CallSpecAndTarget(callSpecification, target));

        var allMatchingCallsOnTarget = target.ReceivedCalls();
        _allCalls.UnionWith(allMatchingCallsOnTarget);
    }

    public IAllResults Result() => this;

    IEnumerable<ICall> IAllResults.AllCalls() => _allCalls;

    IEnumerable<CallSpecAndTarget> IAllResults.QuerySpecification() => _querySpec.Select(x => x);
}
```

This object simply receives specified calls from the `calls` Action, and grabs the received calls from the substitute when processing each call. 

## ReceivedNoOtherAssertion

This object is based on [SequenceInOrderAssertion](https://github.com/nsubstitute/NSubstitute/blob/main/src/NSubstitute/Core/SequenceChecking/SequenceInOrderAssertion.cs) albeit simpler, since it does have to check for a specific order.

```csharp
public static class ReceivedNoOtherAssertion
{
    public static void Assert(IAllResults queryResult)
    {
        var matchingCallsInOrder = queryResult
            .AllCalls()
            .Where(x => IsNotPropertyGetterCall(x.GetMethodInfo()))
            .ToArray();
        var querySpec = queryResult
            .QuerySpecification()
            .Where(x => IsNotPropertyGetterCall(x.CallSpecification.GetMethodInfo()))
            .ToArray();

        if (matchingCallsInOrder.Length != querySpec.Length)
        {
            throw new OtherCallFoundException(GetExceptionMessage(querySpec, matchingCallsInOrder));
        }

        var callsAndSpecs = matchingCallsInOrder
            .Select(call => new
            {
                Call = call,
                Specs = querySpec.Where(x => Matches(call, x)).ToArray()
            })
            .ToArray();

        if (Array.Exists(callsAndSpecs, x => x.Specs.Length == 0))
        {
            throw new OtherCallFoundException(GetExceptionMessage(querySpec, matchingCallsInOrder));
        }
    }

    private static bool Matches(ICall call, CallSpecAndTarget specAndTarget)
        => ReferenceEquals(call.Target(), specAndTarget.Target)
               && specAndTarget.CallSpecification.IsSatisfiedBy(call);

    private static bool IsNotPropertyGetterCall(MethodInfo methodInfo)
        => methodInfo.GetPropertyFromGetterCallOrNull() == null;

    private static string GetExceptionMessage(CallSpecAndTarget[] querySpec, ICall[] matchingCallsInOrder)
    {
        const string callDelimiter = "\n    ";
        var formatter = new SequenceFormatter(callDelimiter, querySpec, matchingCallsInOrder);
        return string.Format("\nExpected to receive only these calls:\n{0}{1}\n" +
                             "\nActually received matching calls:\n{0}{2}\n\n{3}",
                             callDelimiter,
                             formatter.FormatQuery(),
                             formatter.FormatActualCalls(),
                             "*** Note: calls to property getters are not considered part of the query. ***");
    }
}
```

I'm also using `SequenceFormatter`, which formats the error message. Although its meant for the `Received.InOrder` feature, the message it generates is usable for `NoOtherThan`.

## Usage

With this setup, you can write unit tests like:

```csharp
public void Test()
{
    // this should be in your SUT
    _foo.Start(1);
    _foo.Start(2);
    _foo.Start(3);

    // the actual test
    Received.NoOtherThan(() =>
    {
        _foo.Start(3);
        _foo.Start(1);
        _foo.Start(2);
    });
}
```

Although useful, this approach has some issues. One of those issues is that the following test passes, while it would be good if it were possible to make it fail:

```csharp
public void Test()
{
    _foo.Start();
    _bar.Begin();

    Received.NoOtherThan(() => _foo.Start());
}
```

The invocations on `_bar` are not tested. This is because `NoOtherThan` is opt-in, it only checks substitutes that are mentioned in the specification. This can be solved by adding `_bar` to `GetAllCallsQuery` using:

```csharp
public void Test()
{
    _foo.Start();
    _bar.Begin();

    Assert.Throws<OtherCallFoundException>(() =>
       Received.For(_foo, _bar).NoOtherThan(() =>
       {
           _foo.Start();
       }));
}
```

This adds `_foo` and `_bar` to the query, and works like this:

```csharp
public static class Received
{
    public static ReceivedForSubstitutes For(params object[] substitutes)
    {
        return new ReceivedForSubstitutes(substitutes);
    }

    public sealed class ReceivedForSubstitutes(object[] substitutes)
    {
        public void NoOtherThan(Action calls)
        {
            var query = new GetAllCallsQuery(SubstitutionContext.Current.CallSpecificationFactory);

            foreach (var substitute in substitutes)
            {
                query.RegisterSubstitute(substitute);
            }

            SubstitutionContext.Current.ThreadContext.RunInQueryContext(calls, query);
            ReceivedNoOtherAssertion.Assert(query.Result());
        }
    }
}
```

Which requires the following method to be added on `GetAllCallsQuery`:

```csharp
public void RegisterSubstitute(object substitute)
{
    _allCalls.UnionWith(substitute.ReceivedCalls());
}
```

This adds all received calls to the `_allCalls` properties of the query, so the query is also aware of the calls performed on substitutes that are not part of the specification. In some cases they cannot be part of the specification, since no calls were actually expected. In case no other substitutes need to be added, I've added `ForMentioned`.

The complete implementation will look something like this:

```csharp
public static class Received
{
    public static ReceivedForSubstitutes For(params object[] substitutes)
    {
        return new ReceivedForSubstitutes(substitutes);
    }

    public static ReceivedForSubstitutes ForMentioned()
    {
        return new ReceivedForSubstitutes([]);
    }

    public sealed class ReceivedForSubstitutes(object[] substitutes)
    {
        public void NoOtherThan(Action calls)
        {
            var query = new GetAllCallsQuery(SubstitutionContext.Current.CallSpecificationFactory);

            foreach (var substitute in substitutes)
            {
                query.RegisterSubstitute(substitute);
            }

            SubstitutionContext.Current.ThreadContext.RunInQueryContext(calls, query);
            ReceivedNoOtherAssertion.Assert(query.Result());
        }
    }

    public sealed class GetAllCallsQuery(ICallSpecificationFactory callSpecificationFactory) : IQuery, IAllResults
    {
        private readonly List<CallSpecAndTarget> _querySpec = [];
        private readonly HashSet<ICall> _allCalls = [];

        public void RegisterSubstitute(object substitute)
        {
            _allCalls.UnionWith(substitute.ReceivedCalls());
        }

        public void RegisterCall(ICall call)
        {
            var target = call.Target();
            var callSpecification = callSpecificationFactory.CreateFrom(call, MatchArgs.AsSpecifiedInCall);

            _querySpec.Add(new CallSpecAndTarget(callSpecification, target));

            var allMatchingCallsOnTarget = target.ReceivedCalls();
            _allCalls.UnionWith(allMatchingCallsOnTarget);
        }

        public IAllResults Result() => this;

        IEnumerable<ICall> IAllResults.AllCalls() => _allCalls;

        IEnumerable<CallSpecAndTarget> IAllResults.QuerySpecification() => _querySpec.Select(x => x);
    }

    public static class ReceivedNoOtherAssertion
    {
        public static void Assert(IAllResults queryResult)
        {
            var matchingCallsInOrder = queryResult
                .AllCalls()
                .Where(x => IsNotPropertyGetterCall(x.GetMethodInfo()))
                .ToArray();
            var querySpec = queryResult
                .QuerySpecification()
                .Where(x => IsNotPropertyGetterCall(x.CallSpecification.GetMethodInfo()))
                .ToArray();

            if (matchingCallsInOrder.Length != querySpec.Length)
            {
                throw new OtherCallFoundException(GetExceptionMessage(querySpec, matchingCallsInOrder));
            }

            var callsAndSpecs = matchingCallsInOrder
                .Select(call => new
                {
                    Call = call,
                    Specs = querySpec.Where(x => Matches(call, x)).ToArray()
                })
                .ToArray();

            if (Array.Exists(callsAndSpecs, x => x.Specs.Length == 0))
            {
                throw new OtherCallFoundException(GetExceptionMessage(querySpec, matchingCallsInOrder));
            }
        }

        private static bool Matches(ICall call, CallSpecAndTarget specAndTarget)
            => ReferenceEquals(call.Target(), specAndTarget.Target)
                   && specAndTarget.CallSpecification.IsSatisfiedBy(call);

        private static bool IsNotPropertyGetterCall(MethodInfo methodInfo)
            => methodInfo.GetPropertyFromGetterCallOrNull() == null;

        private static string GetExceptionMessage(CallSpecAndTarget[] querySpec, ICall[] matchingCallsInOrder)
        {
            const string callDelimiter = "\n    ";
            var formatter = new SequenceFormatter(callDelimiter, querySpec, matchingCallsInOrder);
            return string.Format("\nExpected to receive only these calls:\n{0}{1}\n" +
                                 "\nActually received matching calls:\n{0}{2}\n\n{3}",
                                 callDelimiter,
                                 formatter.FormatQuery(),
                                 formatter.FormatActualCalls(),
                                 "*** Note: calls to property getters are not considered part of the query. ***");
        }
    }
}
```

