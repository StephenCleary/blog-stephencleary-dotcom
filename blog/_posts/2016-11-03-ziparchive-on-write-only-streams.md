---
layout: post
title: "ZipArchive on Write-Only Streams"
description: "A necessary workaround for using ZipArchive with write-only streams."
---

A couple weeks ago, I described [how to build a zip file on-the-fly from ASP.NET WebAPI]({% post_url 2016-10-20-async-pushstreamcontent %}) using the [`DotNetZip` library](https://www.nuget.org/packages/DotNetZip/).

So, here's a question: "Why use `DotNetZip` instead of the built-in `ZipArchive`?"

The answer is: You *can* use `ZipArchive`, but you would need to work around a bug in the `ZipArchive` class.

The straightforward approach is to just use `ZipArchive` with `PushStreamContent`, very similar to [the previous example]({% post_url 2016-10-20-async-pushstreamcontent %}):

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
            using (var zipArchive = new ZipArchive(outputStream, ZipArchiveMode.Create))
            {
                foreach (var kvp in filenamesAndUrls)
                {
                    var zipEntry = zipArchive.CreateEntry(kvp.Key);
                    using (var zipStream = zipEntry.Open())
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

This code *should* work, but it will actually fail (as of .NET 4.6.2). It will throw a `NotSupportedException` from within the `PushStreamContent` callback, so it's annoying to find and debug.

Here's the problem:

- Streams can be "seekable." Seekable streams allow reading the `Position` property and updating it via the `Seek` method.
- The ASP.NET response output stream is not seekable. This is a common aspect of all network streams.
- `ZipArchive` should work with write-only (non-seekable) streams. However (and this is the bug), it will actually *read* `Position` even for non-seekable streams in order to build up its list of zip entry offsets in the zip file.

This bug [was reported several years ago](https://connect.microsoft.com/VisualStudio/feedback/details/816411/ziparchive-shouldnt-read-the-position-of-non-seekable-streams) ([webcite](http://www.webcitation.org/6lGHjvc3C)), and it has been closed as "Won't Fix" for some reason. The ever-intelligent @svick has [suggested a workaround](http://stackoverflow.com/questions/16585488/writing-to-ziparchive-using-the-httpcontext-outputstream/21513194#21513194) for this bug: writing a stream wrapper that keeps track of `Position` and allows it to be read.

The important parts of this stream wrapper look like this (the other members of [this type](https://github.com/StephenClearyExamples/AsyncDynamicZip/blob/full-ziparchive/Example/WebApplication/WriteOnlyStreamWrapper.cs) just forward to the underlying stream):

{% highlight csharp %}
public class WriteOnlyStreamWrapper : Stream
{
    private readonly Stream _stream;
    private long _position;

    public WriteOnlyStreamWrapper(Stream stream)
    {
        _stream = stream;
    }

    public override long Position
    {
        get { return _position; }
        set { throw new NotSupportedException(); }
    }
    public override void Write(byte[] buffer, int offset, int count)
    {
        _position += count;
        _stream.Write(buffer, offset, count);
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
    {
        _position += count;
        return _stream.BeginWrite(buffer, offset, count, callback, state);
    }

    public override void EndWrite(IAsyncResult asyncResult) => _stream.EndWrite(asyncResult);

    public override void WriteByte(byte value)
    {
        _position += 1;
        _stream.WriteByte(value);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        _position += count;
        return _stream.WriteAsync(buffer, offset, count, cancellationToken);
    }
}
{% endhighlight %}

The only tricky part about this wrapper is that we want to be sure to override the asynchronous methods as well as the synchronous ones. This is because the `Stream` base class will provide a default implementation of these that just runs the synchronous APIs on a thread pool thread. In other words, it's using fake asynchrony by default! So we need to override them to provide true asynchrony.

With that wrapper in place, our action method can be updated:

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
            // The only change is in this line:
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
        }),
    };
    result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
    result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "MyZipfile.zip" };
    return result;
}
{% endhighlight %}

And `ZipArchive` is quite happy with the stream wrapper!

## Code

A fully-working solution for ASP.NET 4.6 (using the built-in `ZipArchive`) is [available on GitHub](https://github.com/StephenClearyExamples/AsyncDynamicZip/tree/full-ziparchive).
