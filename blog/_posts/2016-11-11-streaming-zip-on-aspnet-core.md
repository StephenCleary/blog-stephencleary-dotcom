---
layout: post
title: "Streaming Zip on ASP.NET Core"
description: "Creating zip files on the fly for ASP.NET Core, with minimal threading and memory overhead."
---

A few weeks ago, I wrote about [using `PushStreamContent` on ASP.NET]({% post_url 2016-10-20-async-pushstreamcontent %}) to build a zip file on-demand that was totally asynchronous and did not have to buffer any intermeditate files in memory.

Today, let's take a look at doing the same thing on ASP.NET Core!

## Downloading a Simple Stream

First, let's look at how we can download a single file stream. On ASP.NET Core, we would use `FileStreamResult` to download a file to the browser:

{% highlight csharp %}
private static HttpClient Client { get; } = new HttpClient();
[HttpGet]
public async Task<FileStreamResult> Get()
{
    var stream = await Client.GetStreamAsync("https://raw.githubusercontent.com/StephenClearyExamples/AsyncDynamicZip/master/README.md");

    return new FileStreamResult(stream, new MediaTypeHeaderValue("text/plain"))
    {
        FileDownloadName = "README.md"
    };
}
{% endhighlight %}

There's a couple of interesting notes here: we pass the `MediaTypeHeaderValue` directly to the constructor of the `FileStreamResult`, which takes care of setting the `Content-Type` header on the response. Also, we set the `FileDownloadName` property, which sets the `Content-Disposition` header on the response. The "ASP.NET Core way" is more about expressing intent rather than modifying the response directly.

This is a nice approach, because there's some pretty fancy encoding that needs to take place if these parameters are unusual. For example, if the `FileDownloadName` uses non-ASCII characters, then the ASP.NET types will automatically encode it using RFC5987 (specifying `filename*` instead of `filename`).

The `FileStreamResult` is one of the "file download" results available in ASP.NET Core. There are also some others, which form a hierarchy of result types:

{:.center}
[![]({{ site_url }}/assets/FileResultHierarchy.png)]({{ site_url }}/assets/FileResultHierarchy.png)

The `FileResult` base type has several derived types, each with a different purpose:

- `PhysicalFileResult` sends an on-disk file identified by a physical path.
- `VirtualFileResult` sends a file identified by a virtual path.
- `FileContentResult` sends the file content as an in-memory byte array.
- `FileStreamResult` sends the file content as a stream.

## Constructing a Zip File on the Fly

Now, let's extend this example to have the WebAPI download a single zip file which is constructed on-demand.

None of the available `FileResult` types will do just what we want; `FileStreamResult` comes closest, but it doesn't allow us to write part of the stream and then do other work before finishing the stream. What we really need is a "callback" kind of result, just like what `PushStreamContent` did.

So, let's introduce our own `FileResult` type, called `FileCallbackResult`:

{:.center}
[![]({{ site_url }}/assets/FileCallbackResultHierarchy.png)]({{ site_url }}/assets/FileCallbackResultHierarchy.png)

My original intention of inheriting from `FileResult` is that we could take advantage of existing conventions and code. For example, I want to pass the `media-type` into the constructor and set `FileDownloadName` as a property, just like the other `FileResult` types.

However, it turns out that using the same *code* is a bit more difficult. It turns out that the action result types in ASP.NET Core do not process their results directly; rather, they use "executors", which form their own similar hierarchy:

{:.center}
[![]({{ site_url }}/assets/FileResultExecutorHierarchy.png)]({{ site_url }}/assets/FileResultExecutorHierarchy.png)

Since I want to re-use the *implementation* of file results as much as possible, I tie into this hierarchy as well with my own `FileCallbackResultExecutor`.

The action results in ASP.NET Core will forward their implementation to their respective "executor" types by using dependency injection. For my simple case, I hard-code the executor:

{% highlight csharp %}
public class FileCallbackResult : FileResult
{
    private Func<Stream, ActionContext, Task> _callback;

    public FileCallbackResult(MediaTypeHeaderValue contentType, Func<Stream, ActionContext, Task> callback)
        : base(contentType?.ToString())
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));
        _callback = callback;
    }

    public override Task ExecuteResultAsync(ActionContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        var executor = new FileCallbackResultExecutor(context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>());
        return executor.ExecuteAsync(context, this);
    }

    private sealed class FileCallbackResultExecutor : FileResultExecutorBase
    {
        public FileCallbackResultExecutor(ILoggerFactory loggerFactory)
            : base(CreateLogger<FileCallbackResultExecutor>(loggerFactory))
        {
        }

        public Task ExecuteAsync(ActionContext context, FileCallbackResult result)
        {
            SetHeadersAndLog(context, result);
            return result._callback(context.HttpContext.Response.Body, context);
        }
    }
}
{% endhighlight %}

The `FileCallbackResult` collects and verifies all the result options, and the `FileCallbackResultExecutor` takes care of actually executing the result. This follows the same pattern as the built-in `FileResult` types. If I was to make this into a NuGet package, I'd probably resolve `FileCallbackResultExecutor` using dependency injection; but other than that, this is a very reusable solution.

The `FileCallbackResultExecutor` is quite simple: it ties into `FileResultExecutorBase`, calling `SetHeadersAndLog` to set up all the response headers exactly the same way all the other file results do, and then just invokes its callback to write to the output stream.

With our new `FileCallbackResult` type, we can (finally) download multiple files and combine them into a single zip file on the fly:

{% highlight csharp %}
private static HttpClient Client { get; } = new HttpClient();
[HttpGet]
public IActionResult Get()
{
    var filenamesAndUrls = new Dictionary<string, string>
    {
        { "README.md", "https://raw.githubusercontent.com/StephenClearyExamples/AsyncDynamicZip/master/README.md" },
        { ".gitignore", "https://raw.githubusercontent.com/StephenClearyExamples/AsyncDynamicZip/master/.gitignore" },
    };

    return new FileCallbackResult(new MediaTypeHeaderValue("application/octet-stream"), async (outputStream, _) =>
    {
        using (var zipArchive = new ZipArchive(new WriteOnlyStreamWrapper(outputStream), ZipArchiveMode.Create))
        {
            foreach (var kvp in filenamesAndUrls)
            {
                var zipEntry = zipArchive.CreateEntry(kvp.Key);
                using (var zipStream = zipEntry.Open())
                using (var stream = await Client.GetStreamAsync(kvp.Value))
                    await stream.CopyToAsync(zipStream);
            }
        }
    })
    {
        FileDownloadName = "MyZipfile.zip"
    };
}
{% endhighlight %}

Note that since we want our .NET Core app to run *anywhere*, we don't take a dependency on `DotNetZip` (which as of this writing requires the full .NET framework); rather, we use the built-in `ZipArchive`. Since the [.NET Core version has the same bug](https://github.com/dotnet/corefx/issues/11497) regarding output streams, we have to use a [stream wrapper just like we did last week]({% post_url 2016-11-03-ziparchive-on-write-only-streams %}).

Our action now returns a `FileCallbackResult`, which will be evaluated by ASP.NET Core after our action returns. When it's evaluated, it forwards to a `FileCallbackResultExecutor`, which then invokes the callback given to it by our action.

This solution has all the same advantages of our previous non-Core solution:

- All I/O is asynchronous. At no time are any threads blocked on I/O.
- The zip file is not held in memory. It is streamed directly to the client, compressing on-the-fly.
- For large files, not even a single file is read entirely into memory. Each file is individually compressed on-the-fly.

## Drawbacks to FileCallbackResult

One of the major drawbacks to this approach is that I'm depending on some internals of ASP.NET Core. In particular, the whole "executor" thing (specifically, `FileResultExecutorBase`) are in an `Internals` namespace. This is an indicator that these types *would* be `internal`, but the ASP.NET team thought someone *might* want to use them (and I'm glad they did!). So they're technically `public`, but with the caveat that they are considered implementation details and not part of a public contract. In other words, they can change without warning.

In my case, I think this is an acceptable tradeoff for `FileCallbackResult`, since I want to tie into the existing file result code (`FileResultExecutorBase.SetHeadersAndLog`). It would be easier for me to deal with breaking changes when updating rather than to duplicate all that logic and take over full maintenance/bug fixing responsibility for it.

The other major drawback is error handling. Any callback-based approach (such as `PushStreamContent` or `FileCallbackResult`) will have to deal with the fact that the HTTP response code has already been sent by the time the callback is invoked. So, any exceptions from the callback cannot send a detailed response to the client - they just get a generic "network error".

`PushStreamContent` also had the caveat that the callback *must* close the output stream. ASP.NET Core is not as picky about that; if you return from your callback without closing the output stream, ASP.NET Core will close it for you.

## Code

A fully-working solution for ASP.NET Core 1.0 is [available on GitHub](https://github.com/StephenClearyExamples/AsyncDynamicZip/tree/core-ziparchive).
