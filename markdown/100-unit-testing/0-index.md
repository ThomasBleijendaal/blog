---
theme: Unit Testing
title: Introduction
visible: false
---

This section will contain some of my investigations and explorations into better unit testing strategies, to discover why unit testing, for me, always ends up at the same place: Too complicated, too much setup, too much dependencies, too much effort to expand the functionality a little bit.

The problem with my current way of working is that the setup works great until it doesn't. And once it doesn't, change it is quite difficult, or impossible without accepting losing some granularity or specificity. So probably the base is too simplistic, or the design of the subject under test is not good enough. I want to find a way of working that will be able to handle all things to test, and that won't give me the feeling of falling of a cliff once certain complexity is hit.

So first, let's start off with something simple.

:::note
Most of the examples in this section are quite simple and only use nUnit, FluentAssertions and NSubstitute.
:::
