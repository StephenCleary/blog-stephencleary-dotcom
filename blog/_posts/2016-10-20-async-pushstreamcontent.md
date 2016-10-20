---
layout: post
title: "Asynchronous PushStreamContent"
description: "Using asynchronous PushStreamContent to zip on the fly with minimal threading and memory overhead."
---

I ran into this problem the other day, and thought it would serve as a good use case study.

Put simply, I want to write an ASP.NET WebAPI action that will download a bunch of URLs and generate a zip on the fly, without storing any files in memory, *and* without blocking any threads on I/O.

Fun, fun!

## Downloading a Simple Stream

It's pretty straightforward to download a simple stream in WebAPI; there's built-in support for that:

{% highlight csharp %}
private static HttpClient Client { get; } = new HttpClient();
public async Task<HttpResponseMessage> Get()
{
    var stream = await Client.GetStreamAsync("https://raw.githubusercontent.com/StephenClearyExamples/AsyncDynamicZip/master/README.md");

    var result = new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StreamContent(stream),
    };
    result.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
    result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "README.md" };
    return result;
}
{% endhighlight %}

First, we (asynchronously) send the HTTP request and get the stream back (at this point, the file is not in memory). Next, we create an `HttpResponseMessage` that will use that stream as its content (`StreamContent`). Finally, we set a few headers so that this file downloads nicely if a browser makes the request. If you hit this API in a browser, it will download the `README.md` file indirectly through the WebAPI.

## Constructing a Zip File on the Fly Using MemoryStream

Let's extend this example to have our WebAPI download *multiple* files, and combine them into a single zip file which is then downloaded by the user.

We can do this as such (using the excellent [`DotNetZip` library](https://www.nuget.org/packages/DotNetZip/)):

{% highlight csharp %}
private static HttpClient Client { get; } = new HttpClient();
public async Task<HttpResponseMessage> Get()
{
    var filenamesAndUrls = new Dictionary<string, string>
    {
        { "README.md", "https://raw.githubusercontent.com/StephenClearyExamples/AsyncDynamicZip/master/README.md" },
        { ".gitignore", "https://raw.githubusercontent.com/StephenClearyExamples/AsyncDynamicZip/master/.gitignore" },
    };

    var archive = new MemoryStream();
    using (var zipStream = new ZipOutputStream(archive, leaveOpen: true))
    {
        foreach (var kvp in filenamesAndUrls)
        {
            zipStream.PutNextEntry(kvp.Key);
            using (var stream = await Client.GetStreamAsync(kvp.Value))
                await stream.CopyToAsync(zipStream);
        }
    }

    archive.Position = 0;
    var result = new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StreamContent(archive)
    };
    result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
    result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "MyZipfile.zip" };
    return result;
}
{% endhighlight %}

Here we're downloading *two* files and combining them on-the-fly into a single zip which is downloaded by the user. First, we create the `MemoryStream` that the zip library will write to. Then, we download all the files asynchronously, and add them to the zip archive. Finally, we rewind the `MemoryStream` containing the zip file and send it to the browser using good 'ol `StreamContent`.

The problem with this approach is the `MemoryStream`.

Storing the zip archive in the `MemoryStream` (as you may infer from the name) means that we're building up the entire zip file in memory. The code is asynchronously downloading (using `HttpClient`), and WebAPI will asynchronously send it to the browser (using `StreamContent`), but we are holding the entire zip in memory in the meantime.

There *is* a way to build the zip file *while it is being streamed to the client*. This is possible because the zip file format lists its contents at the *end* of the file.

To use this kind of dynamic streaming, we can't use `MemoryStream` or `StreamContent`. What we *really* want is to write to the output stream directly. With ASP.NET MVC, we could use `HttpResponse.OutputStream` to grab the output stream directly and write to it (not ideal from a design standpoint, but it would work). This is not an option in ASP.NET WebAPI.

However, ASP.NET WebAPI does have a response type that acts as a "callback" that allows us to write directly to the output stream *after* we return from the controller action method. Its name is `PushStreamContent`.

## Constructing a Zip File on the Fly Using PushStreamContent

I think of `PushStreamContent` as just a "callback". When ASP.NET has sent the headers and is ready to send the actual content, it just invokes the callback that we give to `PushStreamContent`. Using this technique, our code looks like this:

{% highlight csharp %}
private static HttpClient Client { get; } = new HttpClient();
public HttpResponseMessage Get()
{
    var filenamesAndUrls = new Dictionary<string, string>
    {
        { "README.md", "https://raw.githubusercontent.com/StephenClearyExamples/AsyncDynamicZip/master/README.md" },
        { ".gitignore", "https://raw.githubusercontent.com/StephenClearyExamples/AsyncDynamicZip/master/.gitignore" },
    };

    var result = new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new PushStreamContent(async (outputStream, httpContext, transportContext) =>
        {
            using (var zipStream = new ZipOutputStream(outputStream))
            {
                foreach (var kvp in filenamesAndUrls)
                {
                    zipStream.PutNextEntry(kvp.Key);
                    using (var stream = await Client.GetStreamAsync(kvp.Value))
                        await stream.CopyToAsync(zipStream);
                }
            }
        }),
    };
    result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
    result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "MyZipfile.zip" };
    return result;
}
{% endhighlight %}

With the callback approach, we can write the zip archive directly to the output stream.

The actual sequence of operations is this:

- ASP.NET calls our controller action.
- We build the list of filenames to download, and return an `HttpResponseMessage` with a status code, a callback, and some HTTP headers.
- ASP.NET starts sending the response to the client; it sends the status code and HTTP headers first.
- Then, when ASP.NET sends the response body to the client, it invokes our callback method.
- Our `PushStreamContent` callback starts writing a zip file (directly to the response body), downloading the files asynchronously one at a time and adding them to the zipped content.
- When our callback returns, ASP.NET completes the response.

This approach has some really nice advantages:

- All I/O is asynchronous. At no time are any threads blocked waiting to read the source files from their URLs, nor are any threads blocked waiting to write to the output response stream.
- The zip file is not held in memory. It is streamed directly to the client, compressing on-the-fly.
- In fact, for large files, not even a single file is read entirely into memory. Each file is individually compressed on-the-fly.

It's interesting to think about how this API will start a download of the zip file, and the zip is already streaming when it may not even have started downloading the other source files!

## Drawbacks to PushStreamContent

Well, it's not all rainbows and unicorns, of course.

The primary drawback to `PushStreamContent` is error handling. Since an HTTP response always *starts* with sending the status code (like `200 OK`) and the headers, by the time our `PushStreamContent` callback is invoked, it's too late to notify the client of an error. So, what happens if our callback throws an exception?

Based on my testing, it appears that ASP.NET will abort the connection. With all the browsers I tested, they correctly interpreted it as a generic "network error".

The problem is, ASP.NET can't go back in time and change the status code or response headers. So it's not possible to get any kind of detailed error information to the client if there's a problem in the `PushStreamContent` callback. The best you can do is just log the error on the server side. This is something to keep in mind when using `PushStreamContent`.

## Code

A fully-working solution for ASP.NET 4.6 is [available on GitHub](https://github.com/StephenClearyExamples/AsyncDynamicZip/tree/full-dotnetzip).
