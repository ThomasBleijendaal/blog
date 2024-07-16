---
theme: NSubstitute
title: Messing around with NSubstitute
visible: true
---

I've been messing around with NSubstitute, just to find something of a replacement for Moq. Not only because of that sponsorlink debacle, but also to find something with a better syntax. I'm trying to avoid stuff like `mock.Setup(x => x.Dread()).ReturnsAsync(new Horror())`, and replacing it with `mock.Dread().Returns(new Horror())` is pretty cool. 

There are just several things just as annoying or simply missing:

- When using `Arg.Is<int>(x => x == 0)` (Moq equivalent: `It.Is<int>(x => x == 0)`) the lambda is still an expression tree. 
- No `VerifyAll()` and friends.
