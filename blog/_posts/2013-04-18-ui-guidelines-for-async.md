---
layout: post
title: "UI Guidelines for Async"
---
So far, a lot of attention has been paid on _how_ to use `async`, but there is little guidance on _when_ to push operations off the UI thread.

I've collected a couple of quotes here that are applicable. They're not much, but they're all we have in the form of official guidance.

## The 50ms Rule

Operations should be made asynchronous if they "could take longer than 50 milliseconds to execute" (Jason Olson, [Keeping apps fast and fluid with asynchrony in the Windows Runtime](https://web.archive.org/web/20120323020957/http://blogs.msdn.com/b/windowsappdev/archive/2012/03/20/keeping-apps-fast-and-fluid-with-asynchrony-in-the-windows-runtime.aspx)).

This is the rule that Microsoft followed with the WinRT APIs; anything taking less than 50ms is considered "fast" and close enough to "immediate" that they do not require an asynchronous API. I also recommend this rule be applied to any synchronous blocks of code; if they are likely to take more than 50ms, push them onto the thread pool via `Task.Run`.

## The Hundred Continuations Per Second Rule

It's usually a good idea to use `ConfigureAwait(false)` in your library code, and only run continuations on the UI thread if they actually need a UI context. But how many continuations can the UI thread really be expected to handle?

At least on the WinRT platform, "the guidance is that just a hundred or so awaits resuming on the UI thread per second will be fine, but a thousand per second will be bad" (Lucian Wischik, [Async Design Patterns](https://docs.microsoft.com/en-us/archive/blogs/lucian/talk-the-new-async-design-patterns?WT.mc_id=DT-MVP-5000058)). Naturally, this is assuming that the continuations do not block for any substantial period of time.

## Caveats

It's difficult to lay down any kind of firm guidance. It's easy to have a situation where the 4-core 8-GB developer's machine handles a thousand continuations on the UI thread easily, but that old XP laptop your client has isn't going to hack it. With WinRT, the hardware is at least somewhat constrained, so the above rules have a stronger meaning on that platform. In the real world, you'll have to test with realistic clients and derive your own numbers.

