---
layout: post
title: "Modern API Clients, Part 4: Authorization"
series: "Modern API Clients"
seriesTitle: "Authorization"
description: "Using ."
---

## Problems with HttpClient

`HttpClient` is awesome, but it does have a few drawbacks. Two, in fact, that make working with `HttpClient` less ideal than it should be.

TL;DR: Modern code should not create `HttpClient` instances; instead, it should use an `HttpClient` factory. Also, register your custom `HttpClientHandler`s with a transient lifetime.

### HttpClient and Socket Exhaustion

One of the problems with `HttpClient` is that if you use one per request (which is the most natural usage suggested by the API), then you can run into a socket exhaustion problem. Specifically, this kind of code pattern should be avoided:

{% highlight csharp %}
// Bad code; do not use!
using (var client = new HttpClient())
  return await client.GetStringAsync("https://example.com/");
{% endhighlight %}

The problem with this code is that the `HttpClient` is creating a new `HttpClientHandler` for each API call. As a refresher, the `HttpClientHandler` is the inner handler at the end of the pipeline that actually does the HTTP socket communication. By re-creating `HttpClientHandler`, this code is not able to re-use those client sockets.

When the application uses a lot of connections using a pattern like the above, it can eventually cause socket exhaustion since those client sockets are not re-used. Technically, this is a form of [ephemeral port exhaustion](https://docs.microsoft.com/en-us/windows/client-management/troubleshoot-tcpip-port-exhaust), where the server runs out of ports to use for new client sockets.

When this problem started becoming common [several years ago](https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/), the conventional workaround was to declare `HttpClient` instances as `static` variables, generally one per remote host. Reusing `HttpClient`s (and thus `HttpClientHandler`s) enabled socket reuse, but this caused [a new problem](http://byterot.blogspot.com/2016/07/singleton-httpclient-dns.html).

### Static HttpClients and DNS Updates

The problem with indefinitely keeping an `HttpClient` (and `HttpClientHandler`) alive is that it caches its DNS lookups indefinitely. Under the hood, a socket is always connected to an IP address, not a hostname; so `HttpClientHandler` will first do a DNS lookup translating the hostname (e.g., `example.com`) to an IP address (e.g., `93.184.216.34`).

The problem is that these DNS lookups are only valid for a certain time, and by indefinitely reusing `HttpClient`, the result of that DNS lookup is cached indefinitely. DNS resolution can change especially in cloud deployments, or more generally in most systems that use rolling deployments. For example, a single hostname may resolve to any of 3 IP addresses; one machine is taken out to upgrade, and then that hostname only resolves to 2 IP addresses. A long-lived `HttpClient` may incorrectly attempt to communicate to a machine that has been taken out of rotation, since it cached the old DNS lookup results. This is just one example; DNS lookup results may change in many other scenarios, too.

So, long-lived applications can't use the workaround where `HttpClient` is just a `static` variable. That approach solves one issue but causes another.

## Solution: HttpClient factory

The modern solution for using `HttpClient` instances is that you *don't*. Instead, you interact with an `HttpClient` factory, which manages a pool and passes out `HttpClient`s as the code has need of them. Socket connections come from the pool, so they are intelligently reused; but they also have a limited lifetime, so that they don't cache DNS lookups indefinitely. Both problems are solved!

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

`HttpClient` on modern platforms (.NET Core 3.0 and newer) uses `SocketsHttpHandler`, which does its own connection pooling. So the pooling behavior of the `HttpClient` factory is not necessary; however, the `HttpClient` factory still offers superior service configuration APIs, even if all your code is already on .NET Core 3.0.

This series will exclusively use `HttpClient` factories, and will never `new`-up an `HttpClient`.
</div>

The standard way to use the default `HttpClient` factory is by using typed clients. The [.NET docs](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests#how-to-use-typed-clients-with-ihttpclientfactory) are pretty good in describing this pattern.

Essentially, you first define a "service" - i.e., the shape of the API as consumed by your application. I recommend you follow the ports/adapters (a.k.a. hexagonal architecture) pattern for this "service"; in other words, *define your service interface according to how your application wants to consume it, not according to how the HTTP API is actually shaped*.

For example, if you wanted to use the GitHub API to get a list of branch names for a repository, then you wouldn't care about [all the other data that that API returns](https://developer.github.com/v3/repos/branches/#list-branches). You'd just want the list of names:

{% highlight csharp %}
public interface IGitHubService
{
  Task<IReadOnlyCollection<string>> GetBranchNamesAsync(string owner, string repository);
}

public sealed class GitHubService : IGitHubService
{
  ...
}
{% endhighlight %}

Then, you can define an `HttpClient` specifically for use by `GitHubService` as such:

{% highlight csharp %}
services.AddHttpClient<IGitHubService, GitHubService>(c =>
    c.BaseAddress = new Uri("https://api.github.com"));
{% endhighlight %}

The `HttpClient` is then injected into your service:

{% highlight csharp %}
public sealed class GitHubService : IGitHubService
{
  private readonly HttpClient _client;
  public GitHubService(HttpClient client) => _client = client;
  public async Task<IReadOnlyCollection<string>> GetBranchNamesAsync(string owner, string repository)
  {
    ...
  }
}
{% endhighlight %}

## Lifetimes for HttpClient and Service Types

The `HttpClient` factory doesn't actually maintain a pool of `HttpClient`s; it maintains a pool of `HttpMessageHandler`s - specifically, the *last* `HttpMessageHandler`s in the message handler pipeline. These are the handlers that actually have the socket connection and send the actual HTTP messages.

The `HttpClient` instance itself is not pooled and reused. It is actually a transient wrapper around the pooled `HttpMessageHandler`.

This also means that your service types are declared as transient. The code that adds a typed `HttpClient` also registers your service with a transient lifetime:

{% highlight csharp %}
// The following line registers `GitHubService` as a transient implementation of `IGitHubService`.
services.AddHttpClient<IGitHubService, GitHubService>(c =>
    c.BaseAddress = new Uri("https://api.github.com"));
{% endhighlight %}

So at runtime, when an instance of `IGitHubService` is needed, the .NET Core dependency injection will attempt to create a `GitHubService` by taking an `HttpMessageHandler` from the pool, wrapping it with an `HttpClient` (setting the `BaseAddress`), and using that to construct the `GitHubService`. When the `GitHubService` passes out of scope, the `GitHubService` and `HttpClient` are eligible for garbage collection, and the `HttpMessageHandler` is returned to the pool.

## Lifetimes for Custom HttpMessageHandlers

We haven't gotten to the "custom `HttpMessageHandler`" part of this series yet, but this is a good time to call out that when we do write a custom `HttpMessageHandler`, we want it to have a transient lifetime. The `HttpClient` is transient, and so is its `HttpMessageHandler` pipeline; only the *last* `HttpMessageHandler` in that pipeline is pooled and reused.

For example, let's consider a custom `HttpMessageHandler` that does some kind of logging. The details aren't important - we'll cover real-world `HttpMessageHandler`s later - for now we'll just look at the general structure of a custom `HttpMessageHandler` and how to hook it into the `HttpClient` pipeline for a specific service.

As noted last time, the custom `HttpMessageHandler` should *not* take an `HttpMessageHandler` argument in its constructor, even though the `DelegatingHandler` base type does; it should *only* take instances that can be provided (with a transient lifetime) by the dependency injection container:

{% highlight csharp %}
public sealed class MyLoggingHandler : DelegatingHandler
{
  private readonly ILogger<MyLoggingHandler> _logger;
  public MyLoggingHandler(ILogger<MyLoggingHandler> logger) => _logger = logger;
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Calling {uri}", request.RequestUri);
    var result = await base.SendAsync(request, cancellationToken);
    _logger.LogInformation("Called {uri}", request.RequestUri);
    return result;
  }
}
{% endhighlight %}

To use `MyLoggingHandler` with the `HttpClient` factory, it must be declared as transient, and then it can be attached to the factory configuration as such:

{% highlight csharp %}
services.AddTransient<MyLoggingHandler>();

services.AddHttpClient<IGitHubService, GitHubService>(c => c.BaseAddress = new Uri("https://api.github.com"))
    .AddHttpMessageHandler<MyLoggingHandler>();
{% endhighlight %}

Now, when an `IGitHubService` instance is needed, the .NET Core dependency injection will attempt to create a `GitHubService` by taking an `HttpMessageHandler` from the pool, wrapping it with an `HttpClient` (setting the `BaseAddress` and adding a transient `MyLoggingHandler`), and using that to construct the `GitHubService`. When the `GitHubService` passes out of scope, `GitHubService`, `HttpClient`, and `MyLoggingHandler` are eligible for garbage collection, and the core `HttpMessageHandler` is returned to the pool.
