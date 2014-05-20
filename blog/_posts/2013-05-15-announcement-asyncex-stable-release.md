---
layout: post
title: "Announcement: AsyncEx Stable Release"
---
A lot of my posts have to do with asynchronous code one way or another. Along the way, I've compiled a lot of useful helper classes into a [library called Nito.AsyncEx](https://nitoasyncex.codeplex.com/).



Today I am pleased to announce that the first public, official release of AsyncEx has gone live. [Try it out](http://nuget.org/packages/Nito.AsyncEx) and see what you think! (And go ahead and yell at me if it's broken).



## Release Notes

With this first stable release of AsyncEx, I restructured the DLL and NuGet packages slightly (details on the [library homepage](https://nitoasyncex.codeplex.com/)). I also had to make some difficult decisions about cutting some of the APIs that were most likely to change in the future.



### Redesigned Features

These types have gone through a redesign since the last prerelease:




- The old `AsyncWaitQueue` (which was possibly the worst API I've ever written) was completely reworked into a properly-designed `IAsyncWaitQueue`.
- `TaskCompletionNotifier` has also been redesigned and renamed to `NotifyTaskCompletion`. This was more of a minor correction in the design.


### Semantic Changes

The `AsyncFactory FromApm` methods now propagate exceptions directly from `Begin*` methods. This matches `TaskFactory.FromAsync` behavior, so it should be less surprising for new adopters.



### Moving Stuff Around


 - Moved synchronous task extensions (e.g., `Task.WaitAndUnwrapException`) into namespace `Nito.AsyncEx.Synchronous` because they are normally not needed.
 - Moved dataflow support into a separate [Nito.AsyncEx.Dataflow NuGet package](https://nuget.org/packages/Nito.AsyncEx.Dataflow/). So if you upgrade and all your dataflow support breaks, you'll need to download the new NuGet package.


### Removed and Probably Not Coming Back

The awaitable interfaces (`IAwaitable`, `IAwaiter`, and friends) have been removed. They are only helpful (but not required) in some advanced custom awaitable situations and they don't properly support `ICriticalNotifyCompletion`.



`TaskFactory.With` has also been removed. While this API makes sense, it just isn't that helpful.



### Removed but Will Probably Come Back after They Bake a Bit More


  - Custom types derived from `Task` (`TaskBase`, `TaskBaseWithCompletion`). These are useful conceptually but need some more API work and testing.
  - ETW tracing. All ETW tracing has been disabled for this release; I'm working on making the ETW tracing more consistent across all types. ETW tracing will definitely be supported in a future release.


### New features


   - Added several more CancellationTokenHelpers: `None`, `Canceled`, and `Timeout`.
   - Finished `Task.Then` implementations.
