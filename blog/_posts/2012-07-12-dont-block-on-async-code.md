---
layout: post
title: "Don't Block on Async Code"
---
This is a problem that is brought up repeatedly on the forums and Stack Overflow. I think it's the most-asked question by async newcomers once they've learned the basics.

## UI Example

Consider the example below. A button click will initiate a REST call and display the results in a text box (this sample is for Windows Forms, but the same principles apply to _any_ UI application).

// My "library" method.
public static async Task<JObject> GetJsonAsync(Uri uri)
{
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

The "GetJson" helper method takes care of making the actual REST call and parsing it as JSON. The button click handler waits for the helper method to complete and then displays its results.

This code will deadlock.

## ASP.NET Example

This example is very similar; we have a library method that performs a REST call, only this time it's used in an ASP.NET context (Web API in this case, but the same principles apply to _any_ ASP.NET application):

// My "library" method.
public static async Task<JObject> GetJsonAsync(Uri uri)
{
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

This code will also deadlock. For the same reason.

## What Causes the Deadlock

Here's the situation: remember [from my intro post]({% post_url 2012-02-02-async-and-await %}) that after you await a Task, when the method continues it will continue _in a context_.

In the first case, this context is a UI context (which applies to _any_ UI except Console applications). In the second case, this context is an ASP.NET request context.

One other important point: an ASP.NET request context is not tied to a specific thread (like the UI context is), but it _does_ only allow one thread in _at a time_. This interesting aspect is not officially documented anywhere AFAIK, but it is mentioned in [my MSDN article about SynchronizationContext](http://msdn.microsoft.com/en-us/magazine/gg598924.aspx).

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

public static async Task<JObject> GetJsonAsync(Uri uri)
{
  using (var client = new HttpClient())
  {
    var jsonString = await client.GetStringAsync(uri).ConfigureAwait(false);
    return JObject.Parse(jsonString);
  }
}

This changes the continuation behavior of GetJsonAsync so that it does _not_ resume on the context. Instead, GetJsonAsync will resume on a thread pool thread. This enables GetJsonAsync to complete the Task it returned without having to re-enter the context.

Consider the second best practice. The new "top-level" methods look like this:

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

This changes the blocking behavior of the top-level methods so that the context is never actually blocked; all "waits" are "asynchronous waits".

**Note:** It is best to apply both best practices. Either one will prevent the deadlock, but _both_ must be applied to achieve maximum performance and responsiveness.

## Resources

  - My [introduction to async/await]({% post_url 2012-02-02-async-and-await %}) is a good starting point. 
  - Stephen Toub's blog post [Await, and UI, and deadlocks! Oh, my!](http://blogs.msdn.com/b/pfxteam/archive/2011/01/13/10115163.aspx) covers this exact type of deadlock (in January of 2011, no less!).
  - If you prefer videos, [Stephen Toub demoed this deadlock live](http://channel9.msdn.com/Events/BUILD/BUILD2011/TOOL-829T) (39:40 - 42:50, but the whole presentation is great!). [Lucian Wischik also demoed this deadlock](http://blogs.msdn.com/b/lucian/archive/2012/03/29/talk-async-part-1-the-message-loop-and-the-task-type.aspx) using VB (17:10 - 19:15).
  - The [Async/Await FAQ](http://blogs.msdn.com/b/pfxteam/archive/2012/04/12/10293335.aspx) goes into detail on exactly when contexts are captured and used for continuations.

This kind of deadlock is always the result of mixing synchronous with asynchronous code. Usually this is because people are just trying out async with one small piece of code and use synchronous code everywhere else. Unfortunately, partially-asynchronous code is much more complex and tricky than just making everything asynchronous.

If you _do_ need to maintain a partially-asynchronous code base, then be sure to check out two more of Stephen Toub's blog posts: [Asynchronous Wrappers for Synchronous Methods](http://blogs.msdn.com/b/pfxteam/archive/2012/03/24/10287244.aspx) and [Synchronous Wrappers for Asynchronous Methods](http://blogs.msdn.com/b/pfxteam/archive/2012/04/13/10293638.aspx), as well as my [AsyncEx library](http://nitoasyncex.codeplex.com/).

## Answered Questions

There are scores of answered questions out there that are all caused by the same deadlock problem. It has shown up on WinRT, WPF, Windows Forms, Windows Phone, MonoDroid, Monogame, and ASP.NET.

<!--
Boring list that I'm no longer interested enough in to maintain.

<p>These questions were all caused by the same deadlock problem, just in different scenarios.</p>

<ul>
<li>UI context:
<ul>
 <li>WinRT:
 <ul>
  <li><a href="http://stackoverflow.com/questions/14485115/synchronously-waiting-for-an-async-operation-and-why-does-wait-freeze-the-pro">Synchronously waiting for an async operation, and why does Wait() freeze the program here</a></li>
  <li><a href="http://stackoverflow.com/questions/13930113/getfilesasync-stops-working">GetFilesAsync stops working</a></li>
  <li><a href="http://stackoverflow.com/questions/13333786/fileio-writeasync-hanging">FileIO.WriteAsync hanging</a></li>
  <li><a href="http://stackoverflow.com/questions/10700570/fileio-readtextasync-occasionally-hangs">FileIO.ReadTextAsync occasionally hangs</a></li>
  <li><a href="http://stackoverflow.com/questions/12701545/async-and-await-with-httpwebrequest-getresponseasync">Async and Await with HttpWebRequest.GetResponseAsync</a></li>
  <li><a href="http://stackoverflow.com/questions/12235085/winrt-loading-static-data-with-getfilefromapplicationuriasync">WinRT: Loading static data with GetFileFromApplicationUriAsync()</a></li>
  <li><a href="http://stackoverflow.com/questions/12392567/async-method-never-retuns">Async method never retuns</a></li>
  <li><a href="http://stackoverflow.com/questions/12048128/async-await-pattern-help-am-i-doing-it-right">Async await pattern help. Am I doing it right?</a></li>
  <li><a href="http://stackoverflow.com/questions/11316438/call-to-await-getfileasync-never-returns-and-app-hangs-in-winrt-app">Call to await GetFileAsync() never returns and app hangs in WinRT app</a></li>
 </ul>
 </li>
 <li>WPF:
 <ul>
  <li><a href="http://social.msdn.microsoft.com/Forums/en-US/parallelextensions/thread/8322bcc5-1ef5-4efe-9683-96fd0829b49d">async/await hang in WPF app</a></li>
 </ul>
 </li>
 <li>Windows Forms:
 <ul>
  <li><a href="http://stackoverflow.com/questions/14597232/asp-net-web-api-client-progressmessagehandler-post-task-stuck-in-winform-app">ASP.NET Web API Client ProgressMessageHandler Post Task stuck in WinForm App</a></li>
 </ul>
 </li>
 <li>Windows Phone 8:
 <ul>
  <li><a href="http://stackoverflow.com/questions/14712132/live-connect-async-and-await-blocking-ui">Live Connect - async and await blocking UI</a></li>
  <li><a href="http://stackoverflow.com/questions/15327993/windows-phone-8-sqlite-async-operations-hanging-indefinitely">Windows Phone 8 SQLite async operations hanging indefinitely</a></li>
 </ul>
 </li>
 <li>Android (MonoDroid):
 <ul>
  <li><a href="http://stackoverflow.com/questions/14631781/using-microsoft-bcl-async-in-pcl-with-mono-droid">Using Microsoft.bcl.async in PCL with Mono Droid?</a></li>
 </ul>
 </li>
 <li>Monogame:
 <ul>
  <li><a href="http://stackoverflow.com/questions/15193520/unable-to-get-list-of-files-in-directory-from-monogame">Unable to get list of files in directory from MonoGame</a></li>
 </ul>
 </li>
 <li>Unspecified:
 <ul>
  <li><a href="http://social.msdn.microsoft.com/Forums/en-AU/async/thread/269172a3-adb9-4b5e-9ac1-8b67ff920177">Async Issue(Why blocking the UI thread)??</a></li>
 </ul>
 </li>
</ul>
</li>
<li>ASP.NET context:
<ul>
 <li><a href="http://stackoverflow.com/questions/14875856/asp-net-web-api-httpclient-download-large-files-breaks">Asp.net Web API: HttpClient Download large files breaks</a></li>
 <li><a href="http://stackoverflow.com/questions/14526377/why-does-this-async-action-hang">Why does this async action hang?</a></li>
 <li><a href="http://stackoverflow.com/questions/14046471/task-waitall-keeps-in-loop">Task.WaitAll keeps in loop</a></li>
 <li><a href="http://stackoverflow.com/questions/13621647/using-async-even-if-it-should-complete-as-part-of-a-mvc-route-deadlocks-the">Using “async” (even if it should complete) as part of a MVC route deadlocks the route; how can this be avoided?</a></li>
 <li><a href="http://stackoverflow.com/questions/12981490/task-waitall-hanging-with-multiple-awaitable-tasks-in-asp-net">Task.WaitAll hanging with multiple awaitable tasks in ASP.NET</a></li>
 <li><a href="http://stackoverflow.com/questions/13140523/await-vs-task-wait-deadlock">await vs Task.Wait - Deadlock?</a></li>
 <li><a href="http://stackoverflow.com/questions/12701879/confusing-behaviour-when-invoking-async-methods-inside-asp-net">Confusing behaviour when invoking async methods inside ASP.NET</a></li>
 <li><a href="http://stackoverflow.com/questions/11887915/preventing-a-deadlock-when-calling-an-async-method-without-using-await">Preventing a deadlock when calling an async method without using await</a></li>
 <li><a href="http://stackoverflow.com/questions/11364272/asp-net-mvc-4-controller-hangs-whenever-async-is-used">ASP.Net MVC 4 controller hangs whenever async is used</a></li>
 <li><a href="http://stackoverflow.com/questions/10343632/httpclient-getasync-never-returns-when-using-await-async">HttpClient.GetAsync(…) never returns when using await/async</a></li>
 <li><a href="http://stackoverflow.com/questions/7804363/async-ctp-bug-task-never-completes">Async CTP Bug - Task Never Completes</a></li>
</ul>
</li>
<li>Unspecified context:
<ul>
 <li><a href="http://stackoverflow.com/questions/14470983/await-task-getting-lost-with-dbcontext-savechanges">Await Task 'getting lost' with dbContext.savechanges()</a></li>
 <li><a href="http://stackoverflow.com/questions/14186608/c-sharp-net-4-5-async-await-task-wait-blocking-issue">c# .net 4.5 async await Task.Wait() blocking issue</a></li>
 <li><a href="http://stackoverflow.com/questions/9545885/get-result-of-async-method">Get result of async method</a></li>
</ul>
</li>
</ul>

-->