---
theme: Unit Testing
title: WIP
---

Thoughts:

- Unit testing is hard
- Pure functions are best to test - no state, inputs dictate output.
- Regular functions have inputs + state (side effects that return data) + side effects.
- Branching code make issue more worse
- Problem space gets giant pretty big fast
- Cyclomatic complexity can be reduced by making private methods to split it off but test complexity remains
- Social testing with sub classes keep complexity high 
    - Relying on knowing what to test and what not feels like cheating (knowing what the sub class does is problematic)
- Moving stuff into separate methods with interfaces only option
    - Use strategy pattern / providers to move away some obvious stuff
    - Use facades to group dependencies together and do simple stuff in facade
    - Use handlers to handle stuff
    - Use services to mash things in

