---
layout: post
title: "Don't Block on Async Code"
---
This is a problem that is brought up repeatedly on the forums and Stack Overflow. I think it's the most-asked question by async newcomers once they've learned the basics.

## UI Example

Consider the example below. A button click will initiate a REST call and display the results in a text box (this sample is for Windows Forms, but the same principles apply to _any_ UI application).

{% highlight csharp %}

// My "library" method.
public static async Task<JObject> GetJsonAsync(Uri uri)
{
  // (real-world code shouldn't use HttpClient in a using block; this is just example code)
  using (var client = new HttpClient())
  {
    var jsonString = await client.GetStringAsync(uri);
    return JObject.Parse(jsonString);
  }
}

// My "top-level" method.
public void Button1_Click(...)
{
  var jsonTask = GetJsonAsync(...);
  textBox1.Text = jsonTask.Result;
}
{% endhighlight %}

The "GetJson" helper method takes care of making the actual REST call and parsing it as JSON. The button click handler waits for the helper method to complete and then displays its results.

This code will deadlock.

## ASP.NET Example

This example is very similar; we have a library method that performs a REST call, only this time it's used in an ASP.NET context (Web API in this case, but the same principles apply to _any_ ASP.NET application):

{% highlight csharp %}

// My "library" method.
public static async Task<JObject> GetJsonAsync(Uri uri)
{
  // (real-world code shouldn't use HttpClient in a using block; this is just example code)
  using (var client = new HttpClient())
  {
    var jsonString = await client.GetStringAsync(uri);
    return JObject.Parse(jsonString);
  }
}

// My "top-level" method.
public class MyController : ApiController
{
  public string Get()
  {
    var jsonTask = GetJsonAsync(...);
    return jsonTask.Result.ToString();
  }
}
{% endhighlight %}

This code will also deadlock. For the same reason.

## What Causes the Deadlock

Here's the situation: remember [from my intro post]({% post_url 2012-02-02-async-and-await %}) that after you await a Task, when the method continues it will continue _in a context_.

In the first case, this context is a UI context (which applies to _any_ UI except Console applications). In the second case, this context is an ASP.NET request context.

One other important point: an ASP.NET request context is not tied to a specific thread (like the UI context is), but it _does_ only allow one thread in _at a time_. This interesting aspect is not officially documented anywhere AFAIK, but it is mentioned in [my MSDN article about SynchronizationContext](http://msdn.microsoft.com/en-us/magazine/gg598924.aspx?WT.mc_id=DT-MVP-5000058).

So this is what happens, starting with the top-level method (Button1_Click for UI / MyController.Get for ASP.NET):

1. The top-level method calls GetJsonAsync (within the UI/ASP.NET context).
1. GetJsonAsync starts the REST request by calling HttpClient.GetStringAsync (still within the context).
1. GetStringAsync returns an uncompleted Task, indicating the REST request is not complete.
1. GetJsonAsync awaits the Task returned by GetStringAsync. The context is captured and will be used to continue running the GetJsonAsync method later. GetJsonAsync returns an uncompleted Task, indicating that the GetJsonAsync method is not complete.
1. The top-level method synchronously blocks on the Task returned by GetJsonAsync. This blocks the context thread.
1. ... Eventually, the REST request will complete. This completes the Task that was returned by GetStringAsync.
1. The continuation for GetJsonAsync is now ready to run, and it waits for the context to be available so it can execute in the context.
1. Deadlock. The top-level method is blocking the context thread, waiting for GetJsonAsync to complete, and GetJsonAsync is waiting for the context to be free so it can complete.

For the UI example, the "context" is the UI context; for the ASP.NET example, the "context" is the ASP.NET request context. This type of deadlock can be caused for either "context".

## Preventing the Deadlock

There are two best practices (both covered in [my intro post]({% post_url 2012-02-02-async-and-await %})) that avoid this situation:

 1. In your "library" async methods, use ConfigureAwait(false) wherever possible.
 1. Don't block on Tasks; use async all the way down.

Consider the first best practice. The new "library" method looks like this:

{% highlight csharp %}

public static async Task<JObject> GetJsonAsync(Uri uri)
{
  // (real-world code shouldn't use HttpClient in a using block; this is just example code)
  using (var client = new HttpClient())
  {
    var jsonString = await client.GetStringAsync(uri).ConfigureAwait(false);
    return JObject.Parse(jsonString);
  }
}
{% endhighlight %}

This changes the continuation behavior of GetJsonAsync so that it does _not_ resume on the context. Instead, GetJsonAsync will resume on a thread pool thread. This enables GetJsonAsync to complete the Task it returned without having to re-enter the context. The top-level methods, meanwhile, do require the context, so they cannot use `ConfigureAwait(false)`.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Using `ConfigureAwait(false)` to avoid deadlocks is a dangerous practice. You would have to use `ConfigureAwait(false)` for *every* `await` in the transitive closure of all methods called by the blocking code, *including all third- and second-party code*. Using `ConfigureAwait(false)` to avoid deadlock is at best [just a hack](https://msdn.microsoft.com/en-us/magazine/mt238404.aspx?WT.mc_id=DT-MVP-5000058){:.alert-link}).

As the title of this post points out, the better solution is "Don't block on async code".
</div>

Consider the second best practice. The new "top-level" methods look like this:

{% highlight csharp %}

public async void Button1_Click(...)
{
  var json = await GetJsonAsync(...);
  textBox1.Text = json;
}

public class MyController : ApiController
{
  public async Task<string> Get()
  {
    var json = await GetJsonAsync(...);
    return json.ToString();
  }
}
{% endhighlight %}

This changes the blocking behavior of the top-level methods so that the context is never actually blocked; all "waits" are "asynchronous waits".

**Note:** It is best to apply both best practices. Either one will prevent the deadlock, but _both_ must be applied to achieve maximum performance and responsiveness.

## Resources

  - My [introduction to async/await]({% post_url 2012-02-02-async-and-await %}) is a good starting point. 
  - Stephen Toub's blog post [Await, and UI, and deadlocks! Oh, my!](https://devblogs.microsoft.com/pfxteam/await-and-ui-and-deadlocks-oh-my/?WT.mc_id=DT-MVP-5000058) covers this exact type of deadlock (in January of 2011, no less!).
  - If you prefer videos, [Stephen Toub demoed this deadlock live](https://channel9.msdn.com/Events/BUILD/BUILD2011/TOOL-829T?WT.mc_id=DT-MVP-5000058) (39:40 - 42:50, but the whole presentation is great!).
  - The [Async/Await FAQ](https://devblogs.microsoft.com/pfxteam/asyncawait-faq/?WT.mc_id=DT-MVP-5000058) goes into detail on exactly when contexts are captured and used for continuations.

This kind of deadlock is always the result of mixing synchronous with asynchronous code. Usually this is because people are just trying out async with one small piece of code and use synchronous code everywhere else. Unfortunately, partially-asynchronous code is much more complex and tricky than just making everything asynchronous.

If you _do_ need to maintain a partially-asynchronous code base, then be sure to check out two more of Stephen Toub's blog posts: [Asynchronous Wrappers for Synchronous Methods](https://devblogs.microsoft.com/pfxteam/should-i-expose-asynchronous-wrappers-for-synchronous-methods/?WT.mc_id=DT-MVP-5000058) and [Synchronous Wrappers for Asynchronous Methods](https://devblogs.microsoft.com/pfxteam/should-i-expose-synchronous-wrappers-for-asynchronous-methods/?WT.mc_id=DT-MVP-5000058), as well as my [AsyncEx library](https://github.com/StephenCleary/AsyncEx).

## Answered Questions

There are scores of answered questions out there that are all caused by the same deadlock problem. It has shown up on WinRT, WPF, Windows Forms, Windows Phone, MonoDroid, Monogame, and ASP.NET.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Update (2014-12-01): For more details, see my [MSDN article on asynchronous best practices](https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming?WT.mc_id=DT-MVP-5000058){:.alert-link} or Section 1.2 in my [Concurrency Cookbook]({{ '/book/' | prepend: site.url_www }}){:.alert-link}.
</div>
