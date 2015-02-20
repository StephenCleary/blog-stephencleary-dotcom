---
layout: post
title: "Async and Await"
---
Most people have already heard about the new "async" and "await" functionality coming in Visual Studio 11. This is Yet Another Introductory Post.

First, the punchline: Async will fundamentally change the way most code is written.

Yup, I believe async/await will have a bigger impact than LINQ. Understanding async will be a basic necessity just a few short years from now.

## Introducing the Keywords

Let's dive right in. I'll use some concepts that I'll expound on later on - just hold on for this first part.

Asynchronous methods look something like this:

{% highlight csharp %}

public async Task DoSomethingAsync()
{
  // In the Real World, we would actually do something...
  // For this example, we're just going to (asynchronously) wait 100ms.
  await Task.Delay(100);
}
{% endhighlight %}

The "async" keyword enables the "await" keyword in that method and changes how method results are handled. _That's all the async keyword does!_ It does not run this method on a thread pool thread, or do any other kind of magic. The async keyword _only_ enables the await keyword (and manages the method results).

The beginning of an async method is executed just like any other method. That is, it runs synchronously until it hits an "await" (or throws an exception).

The "await" keyword is where things can get asynchronous. Await is like a unary operator: it takes a single argument, an **awaitable** (an "awaitable" is an asynchronous operation). Await examines that awaitable to see if it has already completed; if the awaitable has already completed, then the method just continues running (synchronously, just like a regular method).

If "await" sees that the awaitable has not completed, then it acts asynchronously. It tells the awaitable to run the remainder of the method when it completes, and then _returns_ from the async method.

Later on, when the awaitable completes, it will execute the remainder of the async method. If you're awaiting a built-in awaitable (such as a task), then the remainder of the async method will execute on a "context" that was captured before the "await" returned.

I like to think of "await" as an "asynchronous wait". That is to say, the async _method_ pauses until the awaitable is complete (so it _waits_), but the actual _thread_ is not blocked (so it's _asynchronous_).

## Awaitables

As I mentioned, "await" takes a single argument - an "awaitable" - which is an asynchronous operation. There are two awaitable types already common in the .NET framework: Task\<T> and Task.

There are also other awaitable types: special methods such as "Task.Yield" return awaitables that are not Tasks, and the WinRT runtime (coming in Windows 8) has an unmanaged awaitable type. You can also create your own awaitable (usually for performance reasons), or use extension methods to make a non-awaitable type awaitable.

That's all I'm going to say about making your own awaitables. I've only had to write a couple of awaitables in the entire time I've used async/await. If you want to know more about writing your own awaitables, see the [Parallel Team Blog](http://blogs.msdn.com/b/pfxteam/) or [Jon Skeet's Blog](http://codeblog.jonskeet.uk/).

One important point about awaitables is this: it is the _type_ that is awaitable, not the method returning the type. In other words, you can await the result of an async method that returns Task ... _because the method returns Task, not because it's async_. So you can also await the result of a _non-async_ method that returns Task:

{% highlight csharp %}

public async Task NewStuffAsync()
{
  // Use await and have fun with the new stuff.
  await ...
}

public Task MyOldTaskParallelLibraryCode()
{
  // Note that this is not an async method, so we can't use await in here.
  ...
}

public async Task ComposeAsync()
{
  // We can await Tasks, regardless of where they come from.
  await NewStuffAsync();
  await MyOldTaskParallelLibraryCode();
}
{% endhighlight %}

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Tip: If you have a very simple asynchronous method, you may be able to write it without using the await keyword (e.g., using Task.FromResult). If you _can_ write it without await, then you _should_ write it without await, and remove the async keyword from the method. A non-async method returning Task.FromResult is more efficient than an async method returning a value.
</div>

## Return Types

Async methods can return Task\<T>, Task, or void. In almost all cases, you want to return Task\<T> or Task, and return void only when you have to.

Why return Task\<T> or Task? Because they're awaitable, and void is not. So if you have an async method returning Task\<T> or Task, then you can pass the result to await. With a void method, you don't have anything to pass to await.

You have to return void when you have async event handlers.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

You can also use async void for other "top-level" kinds of actions - e.g., a single "static async void MainAsync()" for Console programs. However, this use of async void has its own problem; see [Async Console Programs]({% post_url 2012-02-03-async-console-programs %}){:.alert-link}. The primary use case for async void methods is event handlers.
</div>

## Returning Values

Async methods returning Task or void do not have a return value. Async methods returning Task\<T> must return a value of type T:

{% highlight csharp %}
public async Task<int> CalculateAnswer()
{
  await Task.Delay(100); // (Probably should be longer...)

  // Return a type of "int", not "Task<int>"
  return 42;
}
{% endhighlight %}

This is a bit odd to get used to, but there are [good reasons]({% post_url 2011-09-01-async-ctp-why-do-keywords-work-that-way %}) behind this design.

## Context

In the overview, I mentioned that when you await a built-in awaitable, then the awaitable will capture the current "context" and later apply it to the remainder of the async method. What exactly is that "context"?

Simple answer:

1. If you're on a UI thread, then it's a UI context.
1. If you're responding to an ASP.NET request, then it's an ASP.NET request context.
1. Otherwise, it's usually a thread pool context.

Complex answer:

 1. If SynchronizationContext.Current is not null, then it's the current SynchronizationContext. (UI and ASP.NET request contexts are SynchronizationContext contexts).
 1. Otherwise, it's the current TaskScheduler (TaskScheduler.Default is the thread pool context).

What does this mean in the real world? For one thing, capturing (and restoring) the UI/ASP.NET context is done transparently:

{% highlight csharp %}

// WinForms example (it works exactly the same for WPF).
private async void DownloadFileButton_Click(object sender, EventArgs e)
{
  // Since we asynchronously wait, the UI thread is not blocked by the file download.
  await DownloadFileAsync(fileNameTextBox.Text);

  // Since we resume on the UI context, we can directly access UI elements.
  resultTextBox.Text = "File downloaded!";
}

// ASP.NET example
protected async void MyButton_Click(object sender, EventArgs e)
{
  // Since we asynchronously wait, the ASP.NET thread is not blocked by the file download.
  // This allows the thread to handle other requests while we're waiting.
  await DownloadFileAsync(...);

  // Since we resume on the ASP.NET context, we can access the current request.
  // We may actually be on another *thread*, but we have the same ASP.NET request context.
  Response.Write("File downloaded!");
}
{% endhighlight %}

This is great for event handlers, but it turns out to not be what you want for most other code (which is, really, most of the async code you'll be writing).

## Avoiding Context

Most of the time, you don't _need_ to sync back to the "main" context. Most async methods will be designed with composition in mind: they await other operations, and each one represents an asynchronous operation itself (which can be composed by others). In this case, you want to tell the awaiter to _not_ capture the current context by calling **ConfigureAwait** and passing false, e.g.:

{% highlight csharp %}

private async Task DownloadFileAsync(string fileName)
{
  // Use HttpClient or whatever to download the file contents.
  var fileContents = await DownloadFileContentsAsync(fileName).ConfigureAwait(false);

  // Note that because of the ConfigureAwait(false), we are not on the original context here.
  // Instead, we're running on the thread pool.

  // Write the file contents out to a disk file.
  await WriteToDiskAsync(fileName, fileContents).ConfigureAwait(false);

  // The second call to ConfigureAwait(false) is not *required*, but it is Good Practice.
}

// WinForms example (it works exactly the same for WPF).
private async void DownloadFileButton_Click(object sender, EventArgs e)
{
  // Since we asynchronously wait, the UI thread is not blocked by the file download.
  await DownloadFileAsync(fileNameTextBox.Text);

  // Since we resume on the UI context, we can directly access UI elements.
  resultTextBox.Text = "File downloaded!";
}
{% endhighlight %}

The important thing to note with this example is that each "level" of async method calls has its own context. DownloadFileButton_Click started in the UI context, and called DownloadFileAsync. DownloadFileAsync also started in the UI context, but then stepped out of its context by calling ConfigureAwait(false). The rest of DownloadFileAsync runs in the thread pool context. However, when DownloadFileAsync completes and DownloadFileButton_Click resumes, it _does_ resume in the UI context.

A good rule of thumb is to use ConfigureAwait(false) unless you know you _do_ need the context.

## Async Composition

So far, we've only considered serial composition: an async method waits for one operation at a time. It's also possible to start several operations and await for one (or all) of them to complete. You can do this by starting the operations but not awaiting them until later:

    public async Task DoOperationsConcurrentlyAsync()
    {
      Task[] tasks = new Task[3];
      tasks[0] = DoOperation0Async();
      tasks[1] = DoOperation1Async();
      tasks[2] = DoOperation2Async();
    
      // At this point, all three tasks are running at the same time.
    
      // Now, we await them all.
      await Task.WhenAll(tasks);
    }
    
    public async Task<int> GetFirstToRespondAsync()
    {
      // Call two web services; take the first response.
      Task<int>[] tasks = new[] { WebService1Async(), WebService2Async() };
    
      // Await for the first one to respond.
      Task<int> firstTask = await Task.WhenAny(tasks);
    
      // Return the result.
      return await firstTask;
    }

By using concurrent composition (Task.WhenAll or Task.WhenAny), you can perform simple concurrent operations. You can also use these methods along with Task.Run to do simple parallel computation. However, this is not a substitute for the Task Parallel Library - any advanced CPU-intensive parallel operations should be done with the TPL.

## Guidelines

Read the [Task-based Asynchronous Pattern (TAP) document](http://www.microsoft.com/download/en/details.aspx?id=19957). It is extremely well-written, and includes guidance on API design and the proper use of async/await (including cancellation and progress reporting).

There are many new await-friendly techniques that should be used instead of the old blocking techniques. If you have any of these Old examples in your new async code, you're Doing It Wrong(TM):

<div class="panel panel-default" markdown="1">

{:.table .table-striped}
|Old|New|Description|
|-
|task.Wait|await task|Wait/await for a task to complete|
|task.Result|await task|Get the result of a completed task|
|Task.WaitAny|await Task.WhenAny|Wait/await for one of a collection of tasks to complete|
|Task.WaitAll|await Task.WhenAll|Wait/await for every one of a collection of tasks to complete|
|Thread.Sleep|await Task.Delay|Wait/await for a period of time|
|Task constructor|Task.Run or TaskFactory.StartNew|Create a code-based task|

</div>

## Next Steps

I have published an MSDN article [Best Practices in Asynchronous Programming](http://msdn.microsoft.com/en-us/magazine/jj991977.aspx), which further explains the "avoid async void", "async all the way" and "configure context" guidelines.

The [official MSDN documentation](http://msdn.microsoft.com/en-us/library/hh191443.aspx) is quite good; they include an online version of the [Task-based Asynchronous Pattern document](http://msdn.microsoft.com/en-us/library/hh873175.aspx) which is excellent, covering the designs of asynchronous methods.

The async team has published an [async/await FAQ](http://blogs.msdn.com/b/pfxteam/archive/2012/04/12/10293335.aspx) that is a great place to continue learning about async. They have pointers to the best blog posts and videos on there. Also, pretty much any blog post by [Stephen Toub](http://blogs.msdn.com/b/pfxteam) is instructive!

Of course, another resource is my own blog.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

My [Concurrency Cookbook]({{ '/book/' | prepend: site.url_www }}){:.alert-link} covers a wide range of use cases for `async` and `await`, and also covers situations when you should use the Task Parallel Library, Rx, or TPL Dataflow instead.
</div>

