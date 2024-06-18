---
theme: Unit Testing
title: Side effects
visible: true
---

Let's suppose we have a class that implements the following interface:

```csharp
public interface IInterface 
{
    Task DoSomethingAsync(string input);
}
```

Unit testing that class by giving it some input and verifying whether it gives a `Task` back is pretty useless. That method could be implemented in a thousand different ways, which will all pass. [Black box testing](https://en.wikipedia.org/wiki/Black-box_testing) is not feasible or useful so, in this case, we have to check the implementation based on what that method does to it's dependencies. 

```csharp
public class Class : IInterface
{
    private readonly Func<string, Task> _dependency;

    public Class(Func<string, Task> dependency)
    {
        _dependency = dependency;
    }

    public async Task DoSomethingAsync(string input)
    {
        await _dependency.Invoke(input);
    }
}
```

The `Class` implementation is pretty simple, but it allows me to demonstrate several things. First, what should we test here? If we look at the implementation I suggest we should verify 2 things. First is that we pass the input into the dependency without any modification. Tracking where the input goes and if each dependency is invoked correctly is important. It pins down the implementation, and can prevent bugs where the wrong string is passed down into a dependency. Send thing is to test what happens if an exception comes out of the the dependency. There is no `try-catch`, so a test can reflect that so its behavior is validated and documented that way.

Test 1 can be implemented as:

```csharp
[Test]
public async Task Test1()
{
    // arrange
    var dependency = Substitute.For<Func<string, Task>>();
    dependency.Invoke(Arg.Any<string>()).Returns(Task.CompletedTask);

    var subject = new Class(dependency);

    // act
    await subject.DoSomethingAsync("input");

    // assert
    await dependency.Received().Invoke("input");
}
```

We setup the `Func<string, Task>` to return a completed task for any input, invoke the method we're testing and then check that we've received the correct invocation.

Test 2 can be implemented as:

```csharp
[Test]
public async Task Test2()
{
    // arrange
    var dependency = Substitute.For<Func<string, Task>>();
    dependency.Invoke(Arg.Any<string>()).Returns(Task.FromException(new Exception()));

    var subject = new Class(dependency);

    // act
    var act = () => subject.DoSomethingAsync("input");
    await act.Should().ThrowAsync<Exception>();

    // assert
    await dependency.Received().Invoke("input");
}
```

Few things to consider:

It can be tempting to skip the check under `assert` in the second test. The fact that the exception is thrown is only possible because the dependency has been setup the way it is. It being invoked can therefore be inferred by the fact that the dependency is called. This might seem very logical, but consider this test:

```csharp
[Test]
public async Task Test2Bad()
{
    // arrange
    var subject = new Class(null);

    // act
    var act = () => subject.DoSomethingAsync("input");
    await act.Should().ThrowAsync<Exception>();
}
```

This test also passes, but this time because a `NullReferenceException` fits onto a `Exception`. The method under test breaks in a way that is completely unexpected during normal operation, dependencies are never null ([except sometimes](https://github.com/Azure/azure-functions-host/issues/6587)) making it a wrong test. I've seen such fails in the wild so this is not a hypothetical. Having an assert that checks whether the dependency was invoked could have helped, although it can become incredibly tedious to verify multiple dependencies in every test. 
