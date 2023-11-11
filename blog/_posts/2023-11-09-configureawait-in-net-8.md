---
layout: post
title: "ConfigureAwait in .NET 8"
description: "Changes in ConfigureAwait that are new with .NET 8.0."
---

I don't often write "what's new in .NET" posts, but .NET 8.0 has an interesting addition that I haven't seen a lot of people talk about. `ConfigureAwait` is getting a pretty good overhaul/enhancement; let's take a look!

## ConfigureAwait(true) and ConfigureAwait(false)

First, let's review the semantics and history of the original `ConfigureAwait`, which takes a boolean argument named `continueOnCapturedContext`.

When `await` acts on a task (`Task`, `Task<T>`, `ValueTask`, or `ValueTask<T>`), its [default behavior]({% post_url 2012-02-02-async-and-await %}) is to capture a "context"; later, when the task completes, the `async` method resumes executing in that context. The "context" is `SynchronizationContext.Current` or `TaskScheduler.Current` (falling back on the thread pool context if none is provided). This default behavior of continuing on the captured context can be made explicit by using `ConfigureAwait(continueOnCapturedContext: true)`.

`ConfigureAwait(continueOnCapturedContext: false)` is useful if you *don't* want to resume on that context. When using `ConfigureAwait(false)`, the `async` method resumes on any available thread pool thread.

The history of `ConfigureAwait(false)` is interesting (at least to me). Originally, the community recommended using `ConfigureAwait(false)` everywhere you could, unless you *needed* the context. This is the position I [recommended in my Async Best Practices article](https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming?WT.mc_id=DT-MVP-5000058#configure-context). There were several discussions during that time frame over why the default was `true`, especially from frustrated library developers who had to use `ConfigureAwait(false)` a lot.

Over the years, though, the recommendation of "use `ConfigureAwait(false)` whenever you can" has been modified. The first (albeit minor) shift was instead of "use `ConfigureAwait(false)` whenever you can", a simpler guideline arose: use `ConfigureAwait(false)` in library code and *don't* use it in application code. This is an easier guideline to understand and follow. Still, the complaints about having to use `ConfigureAwait(false)` continued, with periodic requests to change the default on a project-wide level. These requests have always been rejected by the C# team for language consistency reasons.

More recently (specifically, since ASP.NET dropped their `SynchronizationContext` with ASP.NET Core and fixed all the places where sync-over-async was necessary), there has been a move away from `ConfigureAwait(false)`. As a library author, I fully understand how annoying it is to have `ConfigureAwait(false)` litter your codebase! Some library authors have just decided not to bother with `ConfigureAwait(false)`. For myself, I still use `ConfigureAwait(false)` in my libraries, but I understand the frustration.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

An earlier version of this post incorrectly claimed that the Entity Framework Core team had decided not to use `ConfigureAwait(false)`. This was only true in early versions of Entity Framework Core. Entity Framework Core [added `ConfigureAwait(false)` in version 5.0.0](https://github.com/dotnet/efcore/pull/21110){:.alert-link} and continues to use `ConfigureAwait(false)` as of this writing (2023-11-11).
</div>

Since we're on the topic of `ConfigureAwait(false)`, I'd like to note a few common misconceptions:

1. `ConfigureAwait(false)` is not a good way to avoid deadlocks. That's not its purpose, and it's a questionable solution at best. In order to avoid deadlocks when doing direct blocking, you'd have to make sure _all_ the asynchronous code uses `ConfigureAwait(false)`, including code in libraries and the runtime. It's just not a very maintainable solution. There are [better solutions available](https://learn.microsoft.com/en-us/archive/msdn-magazine/2015/july/async-programming-brownfield-async-development?WT.mc_id=DT-MVP-5000058).
1. `ConfigureAwait` configures the `await`, not the task. E.g., the `ConfigureAwait(false)` in `SomethingAsync().ConfigureAwait(false).GetAwaiter().GetResult()` does exactly nothing. Similarly, the `await` in `var task = SomethingAsync(); task.ConfigureAwait(false); await task;` still continues on the captured context, completely ignoring the `ConfigureAwait(false)`. I've seen both of these mistakes over the years.
1. `ConfigureAwait(false)` does not mean "run the rest of this method on a thread pool thread" or "run the rest of this method on a different thread". It only takes effect if the `await` yields control and then later resumes the `async` method. Specifically, `await` will *not* yield control if its task is already complete; in that case, the `ConfigureAwait` has no effect because the `await` continues synchronously.

OK, now that we've refreshed our understanding of `ConfigureAwait(false)`, let's take a look at how `ConfigureAwait` is getting some enhancements in .NET 8. None of the existing behavior is changed; `await` without any `ConfigureAwait` at all still has the default behavior of `ConfigureAwait(true)`, and `ConfigureAwait(false)` still has the same behavior, too. But there's a *new* `ConfigureAwait` coming into town!

## ConfigureAwait(ConfigureAwaitOptions)

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

The next thing I want to point out is that `ConfigureAwait(ConfigureAwaitOptions)` is only available on `Task` and `Task<T>`, at least for .NET 8. It wasn't added to `ValueTask` / `ValueTask<T>` yet. It's possible that a future release of .NET may add `ConfigureAwait(ConfigureAwaitOptions)` for value tasks, but as of now it's only available on reference tasks, so you'll need to call `AsTask` if you want to use these new options on value tasks.

Now, let's consider each of these options in turn.

### ConfigureAwaitOptions.None and ConfigureAwaitOptions.ContinueOnCapturedContext

These two are going to be pretty familiar, except with one twist.

`ConfigureAwaitOptions.ContinueOnCapturedContext` - as you might guess from the name - is the same as `ConfigureAwait(continueOnCapturedContext: true)`. In other words, the `await` will capture the context and resume executing the `async` method on that context.

{% highlight csharp %}
Task task = ...;

// These all do the same thing
await task;
await task.ConfigureAwait(continueOnCapturedContext: true);
await task.ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
{% endhighlight %}

`ConfigureAwaitOptions.None` is the same as `ConfigureAwait(continueOnCapturedContext: false)`. In other words, `await` will behave perfectly normally, except that it will *not* capture the context; assuming the `await` does yield (i.e, the task is not already complete), then the `async` method will resume executing on any available thread pool thread.

{% highlight csharp %}
Task task = ...;

// These do the same thing
await task.ConfigureAwait(continueOnCapturedContext: false);
await task.ConfigureAwait(ConfigureAwaitOptions.None);
{% endhighlight %}

Here's the twist: with the new options, the default is to *not* capture the context! Unless you explicitly include `ContinueOnCapturedContext` in your flags, the context will *not* be captured. Of course, the default behavior of `await` itself is unchanged: without any `ConfigureAwait` at all, `await` will behave as though `ConfigureAwait(true)` or `ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext)` was used.

{% highlight csharp %}
Task task = ...;

// Default behavior (no ConfigureAwait): continue on the captured context.
await task;

// Default flag option (None): do not continue on the captured context.
await task.ConfigureAwait(ConfigureAwaitOptions.None);
{% endhighlight %}

So, that's something to keep in mind as you start using this new `ConfigureAwaitOptions` enum.

### ConfigureAwaitOptions.SuppressThrowing

The `SuppressThrowing` flag suppresses exceptions that would otherwise occur when `await`ing a task. Under normal conditions, `await` will observe task exceptions by re-raising them at the point of the `await`. Normally, this is exactly the behavior you want, but there are some situations where you just want to wait for the task to complete and you don't care whether it completes successfully or with an exception. `SuppressThrowing` allows you to wait for the completion of a task without observing its result.

{% highlight csharp %}
Task task = ...;

// These do the same thing
await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
try { await task.ConfigureAwait(false); } catch { }
{% endhighlight %}

I expect this will be most useful alongside cancellation. There are some cases where some code needs to cancel a task and then wait for the existing task to complete before starting a replacement task. `SuppressThrowing` would be useful in that scenario: the code can `await` with `SuppressThrowing`, and the method will continue when the task completes, whether it was successful, canceled, or finished with an exception.

{% highlight csharp %}
// Cancel the old task and wait for it to complete, ignoring exceptions.
_cts.Cancel();
await _task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

// Start the new task.
_cts = new CancellationTokenSource();
_task = SomethingAsync(_cts.Token);
{% endhighlight %}

If you `await` with the `SuppressThrowing` flag, then the exception _is_ considered "observed", so `TaskScheduler.UnobservedTaskException` is not raised. The assumption is that you are awaiting the task and deliberately discarding the exception, so it's not considered unobserved.

{% highlight csharp %}
TaskScheduler.UnobservedTaskException += (_, __) => { Console.WriteLine("never printed"); };

Task task = Task.FromException(new InvalidOperationException());
await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
task = null;

GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();

Console.ReadKey();
{% endhighlight %}

There's another consideration for this flag as well. When used with a plain `Task`, the semantics are clear: if the task faults, the exception is just ignored. However, the same semantics don't quite work for `Task<T>`, because in that case the `await` expression needs to return a value (of type `T`). It's not clear what value of `T` would be appropriate to return in the case of an ignored exception, so the current behavior is to throw an `ArgumentOutOfRangeException` at runtime. To help catch this at compile time, a new warning [was added](https://github.com/dotnet/roslyn-analyzers/pull/6669): `CA2261` `The ConfigureAwaitOptions.SuppressThrowing is only supported with the non-generic Task`. This rule defaults to a warning, but I'd suggest making it an error, since it will always fail at runtime.

{% highlight csharp %}
Task<int> task = Task.FromResult(13);

// Causes CA2261 warning at build time and ArgumentOutOfRangeException at runtime.
await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
{% endhighlight %}

As a final note, this is one flag that also affects synchronous blocking in addition to `await`. Specifically, you can call `.GetAwaiter().GetResult()` to block on the awaiter returned from `ConfigureAwait`. The `SuppressThrowing` flag will cause exceptions to be ignored whether using `await` or `GetAwaiter().GetResult()`. Previously, when `ConfigureAwait` only took a boolean parameter, you could say "ConfigureAwait configures the await"; but now you have to be more specific: "ConfigureAwait returns a configured awaitable". And it is now possible that the configured awaitable modifies the behavior of blocking code in addition to the behavior of the `await`. `ConfigureAwait` is perhaps a slight misnomer now, but it is still _primarily_ intended for configuring `await`. Of course, blocking on asynchronous code still isn't recommended.

{% highlight csharp %}
Task task = Task.Run(() => throw new InvalidOperationException());

// Synchronously blocks on the task (not recommended). Does not throw an exception.
task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing).GetAwaiter().GetResult();
{% endhighlight %}

### ConfigureAwaitOptions.ForceYielding

The final flag is the `ForceYielding` flag. I expect this flag will be rarely used, but when you need it, you need it!

`ForceYielding` is similar to `Task.Yield`. `Yield` returns a special awaitable that always claims to be not completed, but schedules its continuations immediately. What this means is that the `await` always acts asynchronously, yielding to its caller, and then the `async` method continues executing as soon as possible. The [normal behavior for `await`](% post_url 2012-02-02-async-and-await %) is to check if its awaitable is complete, and if it is, then continue executing synchronously; `ForceYielding` prevents that synchronous behavior, forcing the `await` to behave asynchronously.

For myself, I find forcing asynchronous behavior most useful in unit testing. It can also be used to avoid stack dives in some cases. It may also be useful when implementing asynchronous coordination primitives, such as the ones in my AsyncEx library. Essentially, anywhere where you want to force `await` to behave asynchronously, you can use `ForceYielding` to accomplish that.

One point that I find interesting is that `await` with `ForceYielding` makes the `await` behave like it does in JavaScript. In JavaScript, `await` _always_ yields, even if you pass it a resolved promise. In C#, you can now `await` a completed task with `ForceYielding`, and `await` will behave as though it's not completed, just like JavaScript's `await`.

{% highlight csharp %}
static async Task Main()
{
  Console.WriteLine(Environment.CurrentManagedThreadId); // main thread
  await Task.CompletedTask;
  Console.WriteLine(Environment.CurrentManagedThreadId); // main thread
  await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
  Console.WriteLine(Environment.CurrentManagedThreadId); // thread pool thread
}
{% endhighlight %}

Note that `ForceYielding` by itself also implies _not_ continuing on the captured context, so it is the same as saying "schedule the rest of this method to the thread pool" or "switch to a thread pool thread".

{% highlight csharp %}
// ForceYielding forces await to behave asynchronously.
// Lack of ContinueOnCapturedContext means the method continues on a thread pool thread.
// Therefore, code after this statement will *always* run on a thread pool thread.
await task.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
{% endhighlight %}

`Task.Yield` _will_ resume on the captured context, so it's not _exactly_ like `ForceYielding` by itself. It's actually like `ForceYielding` with `ConinueOnCapturedContext`.

{% highlight csharp %}
// These do the same thing
await Task.Yield();
await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding | ConfigureAwaitOptions.ContinueOnCapturedContext);
{% endhighlight %}

Of course, the real value of `ForceYielding` is that it can be applied to any task at all. Previously, in the situations where yielding was required, you had to either add a _separate_ `await Task.Yield();` statement or create a custom awaitable. That's no longer necessary now that `ForceYielding` can be applied to any task.

## Further Reading

It's great to see the .NET team still making improvements in `async`/`await`, all these years later!

If you're interested in more of the history and design discussion behind `ConfigureAwaitOptions`, check out the [pull request](https://github.com/dotnet/runtime/pull/87067). At one point there [was](https://github.com/dotnet/runtime/issues/22144#issuecomment-1561983918) a `ForceAsynchronousContinuation` that was dropped before release. It had a more obscure use case, essentially overriding `await`'s [default behavior of scheduling the `async` method continuation with `ExecuteSynchronously`]({% post_url 2012-12-06-dont-block-in-asynchronous-code %}). Perhaps a future update will add that back in, or perhaps a future update will add `ConfigureAwaitOptions` support to value tasks. We'll just have to see what the future holds!
