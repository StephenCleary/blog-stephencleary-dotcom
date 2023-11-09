---
layout: post
title: "ConfigureAwait in .NET 8"
description: "Changes in ConfigureAwait that are new with .NET 8.0."
---

I don't often write "what's new in .NET" posts, but .NET 8.0 has an interesting addition that I haven't seen a lot of people talk about. `ConfigureAwait` is getting a pretty good overhaul/enhancement; let's take a look!

## ConfigureAwait(true) and ConfigureAwait(false)

First, let's review the semantics and history of the original `ConfigureAwait`, which takes a boolean argument named `continueOnCapturedContext`.

When `await` acts on a task (`Task`, `Task<T>`, `ValueTask`, or `ValueTask<T>`), its [default behavior]({% post_url 2012-02-02-async-and-await %}) is to capture a "context"; later, when the task completes, the `async` method resumes executing in that context. This "context" is `SynchronizationContext.Current` or `TaskScheduler.Current`. This default behavior can be made explicit by using `ConfigureAwait(continueOnCapturedContext: true)`.

`ConfigureAwait(continueOnCapturedContext: false)` is useful if you *don't* want to resume on that context. When using `ConfigureAwait(false)`, the `async` method resumes on any available thread pool thread.

The history of `ConfigureAwait(false)` is interesting (at least to me). Originally, the community recommended using `ConfigureAwait(false)` everywhere you could, unless you *needed* the context. This is the position I [recommended in my Async Best Practices article](https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming?WT.mc_id=DT-MVP-5000058#configure-context). There were several discussions during that time frame over why the default was `true`, especially from frustrated library developers who had to use `ConfigureAwait(false)` a lot.

Over the years, though, the recommendation of "use `ConfigureAwait(false)` whenever you can" has been modified. The first (albeit minor) shift was instead of "use `ConfigureAwait(false)` whenever you can", a simpler guideline arose: use `ConfigureAwait(false)` in library code and *don't* use it in application code. This is an easier guideline to understand and follow. Still, the complaints about having to use `ConfigureAwait(false)` continued, with periodic requests to change the default on a project-wide level. These requests have always been rejected for language consistency reasons.

More recently (specifically, since ASP.NET Core dropped their `SynchronizationContext` and fixed all the places where sync-over-async was necessary), there has been a move away from `ConfigureAwait(false)`. As a library author, I fully understand how annoying it is to have `ConfigureAwait(false)` litter your codebase! Perhaps the most notable departure is the Entity Framework Core team, which just flat-out decided not to use `ConfigureAwait(false)` anymore. For myself, I still use `ConfigureAwait(false)` in my libraries, but I understand the frustration.

Since we're on the topic of `ConfigureAwait(false)`, I'd like to note a few common misconceptions:

1. `ConfigureAwait(false)` is not a good way to avoid deadlocks. That's not its purpose, and it's a questionable solution at best. In order to avoid deadlocks when doing direct blocking, you'd have to make sure _all_ the asynchronous code uses `ConfigureAwait(false)`, including code in libraries and the runtime. It's just not a very maintainable solution. There are [better solutions available](https://learn.microsoft.com/en-us/archive/msdn-magazine/2015/july/async-programming-brownfield-async-development?WT.mc_id=DT-MVP-5000058).
1. `ConfigureAwait` configures the `await`, not the task. E.g., the `ConfigureAwait(false)` in `SomethingAsync().ConfigureAwait(false).GetAwaiter().GetResult()` does exactly nothing. Similarly, the `await` in `var task = SomethingAsync(); task.ConfigureAwait(false); await task;` still has the default behavior, completely ignoring the `ConfigureAwait(false)`. I've seen developers make both of these mistakes.
1. `ConfigureAwait(false)` does not mean "run the rest of this method on a thread pool thread" or "run the rest of this method on a different thread". It only takes effect if the `await` yields control and then later resumes the `async` method. Specifically, `await` will *not* yield control if its task is already complete; in that case, the `ConfigureAwait` has no effect because the `await` continues synchronously.

OK, now that we've refreshed our understanding of `ConfigureAwait(false)`, let's take a look at how `ConfigureAwait` is getting some enhancements in .NET 8. None of the existing behavior is changed; `await` without any `ConfigureAwait` at all still has the default behavior of `ConfigureAwait(true)`, and `ConfigureAwait(false)` still has the same behavior, too. But there's a *new* `ConfigureAwait` coming into town!

## ConfigureAwaitOptions

There are several new options available for `ConfigureAwait`. [`ConfigureAwaitOptions`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.configureawaitoptions?view=net-8.0) is a new type that provides all the different ways to configure awaitables:

{% highlight csharp %}
namespace System.Threading.Tasks;
[Flags]
public enum ConfigureAwaitOptions
{
    None = 0x0,
    ContinueOnCapturedContext = 0x1,
    SuppressThrowing = 0x2,
    ForceYielding = 0x4,
}
{% endhighlight %}

First, a quick note: this is a `Flags` enum; any combination of these options can be used together.

The next thing I want to point out is that `ConfigureAwait(ConfigureAwaitOptions)` is only available on `Task` and `Task<T>`, at least for .NET 8. It wasn't added to `ValueTask` / `ValueTask<T>` because of the natural implementation of some of these behaviors are easier to express if they can retrieve results multiple times (which [isn't allowed for value tasks]({% post_url 2020-03-28-valuetask %})). It's possible that a future release of .NET may add `ConfigureAwait(ConfigureAwaitOptions)` for value tasks, but as of now it's only available on reference tasks, so you'll need to call `AsTask` if you want to use these new options on value tasks.

Now, let's consider each of these options in turn.

### ConfigureAwaitOptions.None and ConfigureAwaitOptions.ContinueOnCapturedContext

These two are going to be pretty familiar, except with one twist.

`ConfigureAwaitOptions.ContinueOnCapturedContext` - as you might guess from the name - is the same as `ConfigureAwait(continueOnCapturedContext: true)`. In other words, the `await` will capture the context and resume executing the `async` method on that context.

`ConfigureAwaitOptions.None` is the same as `ConfigureAwait(continueOnCapturedContext: false)`. In other words, `await` will behave perfectly normally, except that it will *not* capture the context; assuming the `await` does yield (i.e, the task is not already complete), then the `async` method will resume executing on any available thread pool thread.

Here's the twist: with the new options, the default is to *not* capture the context! Unless you explicitly include `ContinueOnCapturedContext` in your flags, the context will *not* be captured. Of course, the default behavior of `await` itself is unchanged: without any `ConfigureAwait` at all, `await` will behave as though `ConfigureAwait(true)` or `ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext)` was used.

So, that's something to keep in mind as you start using this new `ConfigureAwaitOptions` enum.

### SuppressThrowing

The `SuppressThrowing` flag suppresses exceptions that would otherwise occur when `await`ing a task. Under normal conditions, `await` will observe task exceptions by re-raising them at the point of the `await`. Normally, this is exactly the behavior you want, but there are some situations where you just want to wait for the task to complete and you don't care whether it completes successfully or with an exception. `SuppressThrowing` allows you to wait for the completion of a task without observing its result.

I expect this will be most useful alongside cancellation. There are some cases where some code needs to cancel a task and then wait for the existing task to complete before starting a replacement task. `SuppressThrowing` would be useful in that scenario: the code can `await` with `SuppressThrowing`, and the method will continue when the task completes, whether it was successful, canceled, or finished with an exception.

TODO: does it suppress `UnobservedException`?

https://github.com/dotnet/roslyn-analyzers/pull/6669 - CA2261

### ForceYielding

- ConfigureAwait can affect synchronous blocking in addition to await (e.g., `SuppressThrowing`). It is still primarily intended for configuring `await`, but the additional options can affect synchronous behavior, too.



## Future and Further Reading

https://github.com/dotnet/runtime/pull/87067

https://www.youtube.com/watch?v=cAbUh4CD0Qg&t=0h59m53s

It's great to see the .NET team still making improvements in `async`/`await`, all these years later!

- There was a `ForceAsynchronousContinuation` that was dropped before release. It has a more obscure use case, essentially overriding `await`'s default behavior of scheduling the `async` method continuation with `ExecuteSynchronously`. https://github.com/dotnet/runtime/issues/22144#issuecomment-1561983918

- `ConfigureAwait(ConfigureAwaitOptions)` for value tasks.