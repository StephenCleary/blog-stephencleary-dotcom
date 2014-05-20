---
layout: post
title: "Task.Run vs BackgroundWorker: Intro"
series: "Task.Run vs. BackgroundWorker"
seriesTitle: "Intro"
---
This is an introductory post for a new series that I'll be doing comparing `BackgroundWorker` to `Task.Run` (in an `async` style). I always recommend `Task.Run`, and I have already written [a long post describing why]({% post_url 2010-08-16-various-implementations-of-asynchronous %}), but I still see some developers resisting the New Way of Doing Things (TM). So this will be a short series where I'll compare the code side-by-side in different scenarios and show why I think `Task.Run` is superior in every way.

To be clear, this series will show supported scenarios that both `BackgroundWorker` and `async Task.Run` were designed for. I won't be picking on any scenarios that `BackgroundWorker` doesn't support. Except today. :)

## Scenarios Not Supported by BackgroundWorker

One of the design problems of `BackgroundWorker` is that the semantics get surprising when nesting; if you create (and start) a `BackgroundWorker` from within another `BackgroundWorker`, the events on the inner `BackgroundWorker` are raised on the thread pool. I explain why this happens in my [SynchronizationContext article](http://msdn.microsoft.com/en-us/magazine/gg598924.aspx), complete with pretty pictures (don't laugh! I worked hard on those!).

A similar problem is that `BackgroundWorker` doesn't work well with `async` code. An `async DoWork` handler will exit early, causing the `RunWorkerCompleted` to fire before the method is completed.

Also, it's difficult to call `async` code from within a (properly synchronous) `DoWork`; you either have to call `Task.Wait` or establish your own `async`-friendly synchronization context (e.g., [AsyncContext](https://nitoasyncex.codeplex.com/wikipage?title=AsyncContext)).

On the other hand, `Task.Run` does support these scenarios: nesting, `async` delegates, and calling `async` code are all perfectly natural.

This is really just because the design of `BackgroundWorker` is dated. It was fine for its time, but it was obviously skipped over when Microsoft was updating the BCL with `async` support. That should tell us something.

As we go through this series, I hope to convince other developers that `BackgroundWorker` really is dead at this point and should not be used for new development. In every situation, a solution based on `Task.Run` will produce cleaner code.

