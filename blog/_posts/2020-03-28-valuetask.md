---
layout: post
title: "ValueTask Restrictions"
description: "How to use ValueTask, specifically restrictions for consuming code."
---

## ValueTask Restrictions

`ValueTask<T>` is a type that was introduced waaaay back in .NET Core 1.0, almost four years ago (which is pretty much forever in Internet time). However, value tasks are just now becoming more commonly used. Language features including asynchronous disposal and asynchronous enumerables (a.k.a. asynchronous streams) are thrusting value tasks in front of many more developers.

## ValueTask: A Simple Description

Here's value tasks in a single sentence: a value task (`ValueTask<T>`) is a more efficient task than a reference task (`Task<T>`).

As the name implies, a *value* task is a value type rather than a reference type (like ordinary tasks). When using the reference type of task (e.g., `Task<T>`), even if the value is known synchronously (e.g., using `Task.FromResult`), the `Task<T>` wrapper object still needs to be allocated. Value tasks avoid this allocation because they are value types; when a value is known synchronously, code can create and return a value task without having to do any allocation. In addition to a clear performance win in the synchronous case, value tasks often produce more efficient code even in many common asynchronous cases.

However, value tasks come with two important restrictions. Before adopting value tasks everywhere as a replacement for reference tasks, your team needs to understand these restrictions - and if you're writing a library, you should make sure your consumers understand these restrictions, too.

## ValueTask Restriction #1: Only Consume Once (or "YOCO: You Only Consume Once")

Each value task can only be consumed once. This is because value tasks can be reused, so once a value task is consumed, that value task can then *change what it represents* so it now represents some other, unrelated operation. This is unusual for value types, so it can be surprising.

To clarify, by "consume", I mean use `await` to asynchronously wait for the value task to compete, *or* use `AsTask` to convert the value task into a regular task.

Most of the time, the calling code just calls `await` immediately after calling the function (like `await FuncAsync();`), and value tasks work perfectly fine with code like this. But if your code does an `await` more than once, or wants to use `Task.WhenAll` or `Task.WhenAny`, then it should *not* `await` the value task - it should convert the value task to a reference task (by calling `AsTask`) exactly once, and then only use that reference task from then on. Reference tasks may be safely `await`ed multiple times; a `Task<T>` never changes what it represents.

Code consuming a `ValueTask<T>` should only consume it once, and after that the `ValueTask<T>` should be completely ignored.

## ValueTask Restriction #2: Only Consume Asynchronously (or "No More Blocking Now, I Mean It!")

Blocking on asynchronous code has [never been the ideal solution](https://msdn.microsoft.com/en-us/magazine/jj991977.aspx?WT.mc_id=DT-MVP-5000058), but in some cases it is necessary. It is *possible* to block on reference tasks using `GetAwaiter().GetResult()` (or `Result` or `Wait()`).

However, this will not work for value tasks. You simply **cannot** block on value tasks. If you *must* block (again, this is never ideal), then you'll need to convert the value task to a reference task by calling `AsTask`, and then block on that reference task.

Unfortunately, value task does contain a `Result` property, and the code `GetAwaiter().GetResult()` will compile. So code that *attempts* to block on a value task will compile just fine. The problem is that these code patterns *do not always block* when used on a value task. The resulting code has undefined behavior. Just don't go there.

## ValueTask Restrictions and the Pitfall of Library Upgrades

With *both* of the restrictions mentioned above, there is an additional value task pitfall when it comes to library upgrades. To understand why, you need to understand a bit more about how value tasks are implemented.

There are actually three different kinds of value tasks (as of this writing): result value wrappers, reference task wrappers, and the more complex (and more efficient) value task source wrappers. If consuming code violates either of the restrictions above, it will have undefined behavior that can be different depending on what kind of value task is returned.

So one problem is this: consider a library that returns value tasks. For version 1.0.0, this library just uses simple reference task wrappers for its value tasks. If users of the library write code that consumes the value tasks multiple times *or* blocks on the value tasks, then (as of this writing) that code will just happen to work as the consumers expect it to. The undefined behavior *just happens* to be the desired behavior. However, if version 1.0.1 of that library switches to the more efficient value task source wrappers, then those consumers would suddenly break. Not at compile time, mind you - because the compiler will still happily compile this code - but at runtime. If the user code blocks, that could throw exceptions where there were none before. If the user code consumes the value task multiple times, that could cause very strange behavior that can be extremely difficult to debug (since it could be `await`ing operations it didn't even start).

I say all that to say this: before adopting value tasks, you need to be sure your consumers internalize these restrictions! Once value task restrictions are widely understood, then we can start to use value tasks as a more general and widespread replacement for reference tasks.

## TL;DR:

When adopting value tasks:

1. Only Consume Once (or "YOCO: You Only Consume Once")
2. Only Consume Asynchronously (or ["No More Blocking Now, I Mean It!"](https://www.youtube.com/watch?v=ury9eoLnb-0))