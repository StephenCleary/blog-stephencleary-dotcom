---
layout: post
title: "Happy Birthday Async"
description: "Async was officially released five years ago today."
---

Today I'd like to say happy birthday to the C# `async` and `await` keywords! They officially turn five years old today! C# and VB both officially adopted them on September 12, 2012.

The `async` and `await` keywords themselves have a slightly longer history; they were originally introduced in F# on April 12th, 2010. `async` slowly moved into C#/VB, and from there they spread to Python, TypeScript, Hack, and Dart. Python was a huge win for `async`; the Python community is quick to pick up (or invent) the best language features, and they have one of the best language-improvement processes out there. Also, I find the Hack adoption particularly amusing - I mean, even *PHP* has `async` these days!

<!-- TODO: async timeline, JS adoption -->

The jury is still out on whether C++ will pick up `async`, and Java just seems to move at a snail's pace these days.

## Remembering Ye Olde Async Days

My original [intro to `async`/`await` post]({% post_url 2012-02-02-async-and-await %}) went live on my blog about five and a half years ago! It's still fully relevant, which is unusual for a five-year-old blog post. And even today, it brings more traffic to my blog than any other post (about 15% of all my blog traffic is just for that one post).

My, how much has changed since that blog post was published! At that time, `async` was an experimental language modification that required you to install a Community Technology Preview (read: unsupported) package that changed the compilers and language features used by Visual Studio 2010. That is, unless you already installed updates to VS2010, in which case the Async CTP installation would just not work. Until they fixed it, and then it worked. Until the next VS2010 update, when it broke again. No, seriously, it was that bad! :)

Oh, and once (if) you managed to get the Async CTP *installed*, there were still [a number of bugs](https://blogs.msdn.microsoft.com/lucian/2011/04/17/async-ctp-refresh-what-bugs-remain-in-it/). And if you encountered them, there wasn't much you could do about it: the resolution was essentially "don't do that." Of particular note was *using multiple `await`s in expressions*, like Lucian's example `var x = await f() + await g();` Yeah, that didn't even work. Your rule-of-thumb was one `await` per statement, and it was best if that statement did nothing else. `var fx = await f(); var gx = await g(); var x = fx + gx;` FTW!

Also, overload resolution wasn't quite right, especially for asynchronous delegate types. But there were no `async` lambdas anyway. And there was no `dynamic` compatibility with `await` at all. And a lot of compiler safeguards were missing (e.g., `async void Main` was allowed). And the debugger support was *horrible* - really, there was no debugging support *at all* back then.

Today, `async` is truly a first-class citizen of C# and VB. With every release of Visual Studio, the debugger support for `async` code gets better and better. Tracing systems like Application Insights "just work" coordinating traces across asynchronous code. And new enhancements are on the horizon: better code generation around the `async` state machine, [value-type `Task<T>` equivalents](https://github.com/ljw1004/roslyn/blob/features/async-return/docs/specs/feature%20-%20arbitrary%20async%20returns.md), [`async` enumerators](https://github.com/dotnet/roslyn/issues/261), ... the future is bright!

## Why?

Why is async taking over the world? Cloud and mobile.
