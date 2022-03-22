---
layout: post
title: "Modern API Clients, Part 2: HttpClient"
series: "Modern API Clients"
seriesTitle: "HttpClient"
description: "HttpClient and its design."
---

## Use HttpClient

If your codebase still uses `WebClient` or `HttpWebRequest`, it's time to upgrade. It is *far* easier to build a great client API using `HttpClient` than either of those outdated choices. For one thing, `HttpClient` was designed to be `async`-friendly from the start, rather than tacking it on later. Another important aspect of `HttpClient` is its pipeline architecture, which we'll be using quite a bit in this series.

## HttpClient's Pipeline

Internally, `HttpClient` has a single "handler" (an `HttpMessageHandler`) that actually does the processing of the HTTP request. `HttpClient` provides the nice API: you can set `BaseAddress` and `DefaultRequestHeaders` and then call a method like `GetStringAsync` or `PostAsync` to interact with an API. When you make these calls, `HttpClient` takes its properties (like `DefaultRequestHeaders`) along with the method-specific inputs, and combines them into a single `HttpRequestMessage` instance. This instance is then passed to the `HttpMessageHandler`, which does the actual API call and returns a `HttpResponseMessage`.

This means that the "guts" of the `HttpClient` can be swapped out while retaining the same nicer-to-use API. The default message handler is an `HttpClientHandler` which then uses the appropriate platform-specific technique to actually send and receive HTTP requests.

However, this design also means that `HttpClient` can have a *pipeline* of `HttpMessageHandler`s. Specifically, any message handler deriving from `DelegatingHandler` is "linked" to the next `HttpMessageHandler`. This enables a form of middleware, very similar to ASP.NET Core's middleware:

{:.center}
[![]({{ site_url }}/assets/httpclient-pipeline.jpg)]({{ site_url }}/assets/httpclient-pipeline.jpg)

When developers use `HttpClient` directly, they very seldom use the `HttpMessageHandler` pipeline. But it is a critical tool when using `HttpClient` as part of a comprehensive modern API client system. Throughout this series, we'll use delegating `HttpMessageHandler`s to manage authentication headers, retries, timeouts, and logging.

## Implementing DelegatingHandler

As a general rule, try to keep state out of your `DelegatingHandler` implementations. There is one primary method (`SendAsync`) that can be called concurrently, and it's easier to implement `DelegatingHandler` correctly if the class has no state.

Any custom `DelegatingHandler` that you write should have a constructor that does *not* take an `HttpMessageHandler`. The `DelegatingHandler` base type [has two constructors](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.delegatinghandler.-ctor?WT.mc_id=DT-MVP-5000058): one with an `HttpMessageHandler` and one without. It's best to ignore the constructor that takes an `HttpMessageHandler` and just always use the base constructor that takes no arguments. This is because the "pipeline building" code will use the [`DelegatingHandler.InnerHandler` property](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.delegatinghandler.innerhandler?WT.mc_id=DT-MVP-5000058) to create the pipeline; it will *not* use constructor arguments to build the pipeline.

In your custom `DelegatingHandler`, there is one important method to override:

{% highlight csharp %}
protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
{% endhighlight %}

This method takes the HTTP request (`HttpRequestMessage`) and returns the HTTP response (`HttpResponseMessage`). This is a true "middleware" design; your `DelegatingHandler` can inspect or modify both the request and the result.

To call the "next" `HttpMessageHandler`, invoke the `DelegatingHandler.SendAsync` implementation by calling `base.SendAsync`, as such:

{% highlight csharp %}
protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
{
  ... // Inspect/modify `request` and/or `cancellationToken` as desired.
  HttpResponseMessage result = await base.SendAsync(request, cancellationToken);
  ... // Inspect/modify `result` as desired.
  return result;
}
{% endhighlight %}

This is the common pattern for implementing custom `DelegatingHandler`s, and we'll be using it several times in this series.
