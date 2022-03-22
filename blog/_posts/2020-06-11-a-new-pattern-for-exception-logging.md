---
layout: post
title: "A New Pattern for Exception Logging"
description: "Logging from within an exception filter will preserve implicit log context."
---

## TL;DR

Your code should log exceptions from within an exception filter, not a `catch` block.

### Quick Examples

The old pattern of "log-and-propagate" looks like this:

{% highlight csharp %}
// Log-and-propagate, old pattern:
try
{
    ...
}
catch (Exception e)
{
    _logger.LogError(e, "Unexpected error.");
    throw;
}
{% endhighlight %}

The logging should be moved into an exception filter like this:

{% highlight csharp %}
// Log-and-propagate, new pattern:
try
{
    ...
}
catch (Exception e) when (False(() => _logger.LogError(e, "Unexpected error.")))
{
    throw;
}
{% endhighlight %}

Similarly, the old pattern of "log-and-handle" looks something like this:

{% highlight csharp %}
// Log-and-handle, old pattern:
try
{
    ...
}
catch (Exception e)
{
    _logger.LogError(e, "Unexpected error.");
    return null; // or some other handling code
}
{% endhighlight %}

The logging should be moved into an exception filter like this:

{% highlight csharp %}
// Log-and-handle, old pattern:
try
{
    ...
}
catch (Exception e) when (True(() => _logger.LogError(e, "Unexpected error.")))
{
    return null; // or some other handling code
}
{% endhighlight %}

Both of these examples assume the presence of a couple simple utility methods:

{% highlight csharp %}
// Use when you want to handle the exception
public static bool True(Action action)
{
    action();
    return true;
}

// Use when you want to propagate the exception
public static bool False(Action action)
{
    action();
    return false;
}
{% endhighlight %}

### Why?

The remainder of this blog post goes into the "why" behind the new pattern.

## Semantic Logging / Structured Logging

Long gone are the days of text file logging; modern logging systems support rich, contextual logs. This means you can add data fields to your log messages, and then use those additional pieces of data when debugging an issue. It's very satisfying to be able to filter by an HTTP status code range, or take the top three servers where user `Steve` had a `FileNotFound` exception.

Structured logging is so important that every modern logging system supports it. For example, .NET Core style logging uses [message templates](https://messagetemplates.org/), which looks something like this:

{% highlight csharp %}
private int Divide(int numerator, int denominator)
{
    var result = numerator / denominator;
    _logger.LogInformation("Result: {result}", result);
    return result;
}
{% endhighlight %}

The code above will create a log message like `"Result: 4"`. What's not immediately obvious is that the log message *also* has structured data attached to it: a data field called `result` has the (integer) value `4`. When this is consumed by a logging provider that understands structured data, the `result` field is stored *along with* the log message, and can be used for searching or filtering.

If you would like to follow along at home, create an ASP.NET Worker Service (which is really just a Console app with ASP.NET-style logging and dependency injection all set up for you). Then replace `Worker.ExecuteAsync` with this:

{% highlight csharp %}
Divide(13, 3);
{% endhighlight %}

When you run it, you should see this in the output:

{% highlight text %}
info: MyApp.Worker[0]
      Result: 4
{% endhighlight %}

### Logging Scopes

So far, so good, and hopefully that's nothing new.

In addition to adding structured data to a single log message, most modern logging frameworks also support logging *scopes* of structured data. So you can create a logging scope that attaches structured data to *every* log message within that scope:

{% highlight csharp %}
private int Divide(int numerator, int denominator)
{
    using var _ = _logger.BeginScope("Dividing {numerator} by {denominator}", numerator, denominator);
    var result = numerator / denominator;
    _logger.LogInformation("Result: {result}", result);
    return result;
}
{% endhighlight %}

Now, when the `Result: 4` message is logged, it will capture additional structured data items: `result` is `4`, `numerator` is `13`, and `denominator` is `3`. It should be clear that strategically placing data items in logging scopes can greatly assist debugging. Any time you've stared at an error message or unexpected result and wondered "what was the input that caused this?", that's the perfect place to add a logging scope.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Tip: The Console logger ignores logging scopes by default; they have to be manually enabled.
</div>

If you're following along at home, enable logging scopes for the Console logger by updating `CreateHostBuilder` in your `Program.cs`, adding a call to `ConfigureLogging` that removes the existing Console logger and adds a new one that sets `IncludeScopes` to `true`:

{% highlight csharp %}
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        // (begin code changes)
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole(console =>
            {
                console.IncludeScopes = true;
            });
        })
        // (end code changes)
        .ConfigureServices((hostContext, services) =>
        {
            services.AddHostedService<Worker>();
        });
{% endhighlight %}

Now, when you run the code, you'll see the logging scope written to the Console as a part of the information message:

{% highlight text %}
info: MyApp.Worker[0]
      => Dividing 13 by 3
      Result: 4
{% endhighlight %}

Now the logs have a clear context.

## Exception Logging

So, let's talk about logging exceptions. Most methods do not log their own exceptions; any exceptions are propagated out of the method, possibly through many other methods, and eventually logged at a much higher level in the code.

The problem with this approach is that the logging scope is lost once the stack is unwound.

Here's the kind of situation that causes this problem. `Worker.ExecuteAsync` is going to wrap its call in a `try`/`catch` and log the exception. This is a very common pattern in most code bases today:

{% highlight csharp %}
try
{
    Divide(13, 0);
}
catch (Exception e)
{
    _logger.LogError(e, "Unexpected error.");
    throw;
}
{% endhighlight %}

The problem, as stated above, is that the logging scope is already gone by the time `LogError` is called. So our logging output looks like this:

{% highlight text %}
fail: MyApp.Worker[0]
      Unexpected error.
System.DivideByZeroException: Attempted to divide by zero.
   at MyApp.Worker.Divide(Int32 numerator, Int32 denominator) in ...
   at MyApp.Worker.<ExecuteAsync>b__2_0() in ...
{% endhighlight %}

The logs have the exception details, including the stack trace, but they do not have the structured data from the logging scope. When sent to a logging backend, there is no `numerator` or `denominator` data that is attached to this log message. Losing that logging scope data is a problem.

To fix this, we first need a minor segue into how exceptions work.

## How Exceptions Work

When an exception is thrown, the runtime will search the stack for a matching handler. So the runtime walks up the stack looking at each `catch` block and evaluating whether it matches the exception (e.g., the exception type matches). When a matching handler is found, then the stack is unwound to that point and the `catch` block is executed.

The important part of this behavior is that there are two distinct steps: *find, then unwind*.

## Exception Filters

Exception filters have been around for a very long time; .NET 1.0 (2002) supported them, and [Structured Exception Handling](https://docs.microsoft.com/en-us/windows/win32/debug/structured-exception-handling?WT.mc_id=DT-MVP-5000058) existed even way before that. C# only got exception filter capabilities in C# 6.0 (2015), and so far they haven't really become common in most codebases. That may change now, though.

Exception filters allow you to hook into the "find" part of "find, then unwind". By providing an exception filter, you can control whether a specific `catch` block matches the exception.

The key thing to keep in mind about exception filters is that because they hook into the "find" part of the process, this means *they run where the exception is thrown, not where the exception is caught*. This is a little mind-bendy at first, but it makes sense: exception filters are run *before* the stack is unwound.

## Solution: Move Exception Logging into an Exception Filter

So, now we have the pieces necessary for fixing the problem. We just need to log exceptions from within an exception filter. Since the exception filter runs where there exception was thrown, the logging data scope is still present. The stack hasn't been unwound yet, so all that rich semantic data is still available.

There's just one quirk: the exception filter must return a boolean value, indicating whether or not the `catch` block matches. In our case, the logging is just a side effect; logging the exception has no effect on whether the `catch` block matches. So, I use a type like this that just provides methods to "execute this side effect and then return a boolean":

{% highlight csharp %}
public static class ExceptionFilterUtility
{
    public static bool True(Action action)
    {
        action();
        return true;
    }

    public static bool False(Action action)
    {
        action();
        return false;
    }
}
{% endhighlight %}

Once you do a `using static ExceptionFilterUtility;`, you can use it like this:

{% highlight csharp %}
try
{
    Divide(13, 0);
}
catch (Exception e) when (False(() => _logger.LogError(e, "Unexpected error.")))
{
    throw;
}
{% endhighlight %}

And there you go! Our error log message now has the full data context of where the exception was *thrown*, instead of where it was *caught*:

{% highlight text %}
fail: MyApp.Worker[0]
      => Dividing 13 by 0
      Unexpected error.
System.DivideByZeroException: Attempted to divide by zero.
   at MyApp.Worker.Divide(Int32 numerator, Int32 denominator) in ...
   at MyApp.Worker.<ExecuteAsync>b__2_0() in ...
{% endhighlight %}

The full data logging scope is now preserved.

## True or False?

I've defined two helper methods - `True` and `False` - to apply side effects and then return a boolean. I recommend using `False` if the body of your `catch` is nothing more than `throw;`. When an exception is thrown, the exception filter is run and the exception is logged, and then the `false` result means that the exception filter does not match the exception, and the runtime continues searching for a matching handler.

Another scenario is if the `catch` block actually handles the exception. Say, if we know there is an exception that is safe to ignore. In that case, use the `True` helper method so that the exception matches the `catch` block and the stack is unwound and the exception is handled there.

Both helpers are useful in different scenarios.

## Caveat

The solution here unfortunately does not work well with `async` code. This is because `async` will cause exceptions to be caught and then re-thrown at the point of the `await`. So, the exception filter runs at the point of the `await` instead of where the exception was *originally* thrown.

## Conclusion

Modern exception-logging code should do its logging from within an exception filter. As logging data scopes become more and more common, this pattern will enable much more helpful logs for your system.