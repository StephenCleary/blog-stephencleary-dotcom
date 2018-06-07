---
layout: post
title: "Microsoft.Extensions.Logging, Part 1: Introduction"
series: "Microsoft.Extensions.Logging"
seriesTitle: "Introduction"
description: "Microsoft.Extensions.Logging: It's not just for ASP.NET Core."
---

## Microsoft.Extensions.*

As part of ASP.NET Core, Microsoft has released several libraries under the `Microsoft.Extensions` banner. I'm not entirely sure why "extensions" is in the name, but these are generic libraries that are useful in all kinds of scenarios. They were developed as part of ASP.NET Core, but can be used in any kind of application.

For example, Azure Functions has first-class support for `Microsoft.Extensions.Logging`; it will happily pass an `ILogger` to your function, which you can then use for writing logs. I've also found the `Microsoft.Extensions` abstractions useful for Console apps as well as shared code that may or may not run on .NET Core.

## Microsoft.Extensions.Logging

Recently, I found myself in a situation where I needed to work closely with `Microsoft.Extensions.Logging`, both in a consuming and producing scenario. The documentation is still a bit lacking - particularly for implementing custom providers - so I figured I'd write up my lessons learned on this blog.

## Logging for Libraries

I write a *lot* of libraries. "How should I log from my library?" is one question that the .NET ecosystem doesn't have a good answer for. "How should I log from my application?" has about a hundred answers, but what about libraries?

There are three general approaches to library logging:

1. Use a specific logger.
2. Use a common logging abstraction.
3. Use a custom logging abstraction.

Option (1) isn't great for most libraries. The problem is that a library author doesn't want to push their preferred logger onto all their consumers. There are scenarios where you can get away with it - e.g., if logging is purely for diagnostic information and you're using something very low-level like ETW, then that may work. But most of the time, tying your library to a specific logging framework will only limit your adoption.

Option (2) is to use a common logging abstraction like `Microsoft.Extensions.Logging.Abstractions` or `Common.Logging`. The advantage here is that you can work with abstract interfaces (e.g., `Microsoft.Extensions.Logging.ILogger`), and the consumer of your library can plug in a common implementation for their logging framework of choice. The disadvantage is with versioning: over time, the abstraction itself will version (e.g., `Microsoft.Extensions.Logging.Abstractions` is already at `2.1.0`), and this may cause problems as multiple libraries using multiple versions of the common logging abstraction all need to coexist in a single application.

Option (3) is to use a custom logging abstraction defined by your own code. In this case, you have your own `IMyLibraryLogger` that your consumers need to implement. The disadvantage to this approach is that since the abstraction is custom, there are no libraries of common implementations that your consumers can just "plug in". You also run the risk of not supporting the abstractions needed by your consumers; e.g., consumers will not be pleased if your library only supports text logging rather than semantic logging, or if it does not support `async`-compatible scopes. Logging abstractions are hard to get right, and doing this for each library you write just gets annoying. There is a project called [LibLog](https://github.com/damianh/LibLog) that attempts to provide a common way of defining custom logging abstractions, but it doesn't seem to have gotten a lot of traction yet.

As of today, none of these options is really strong. Option (3) is sufficient for *frameworks* (where users are willing to do extra work to get good logs), but there doesn't seem to be a clear winner for your run-of-the-mill small libraries.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Steven van Deursen, the creator of Simple Injector, has [gone on record](https://stackoverflow.com/a/41244169/263693){:.alert-link} saying the whole `Microsoft.Extensions.Logging` design is a violation of the Dependency Injection Principle, with the `ILogger<T>` specifically a violation of the Interface Segregation Principle. He strongly recommends option (3), but I have to counter that it's hard for every library author in the world to get right. The [example solution](https://stackoverflow.com/a/5646876/263693) he links to, for example, only supports text logging, not structured logging. That said, Steven is an incredibly smart person, and you should read his arguments carefully and understand them.
</div>

## Back to Microsoft.Extensions.Logging

This blog series is diving into the details and design of `Microsoft.Extensions.Logging`. I think it's a great choice for applications; whether it should be used in libraries is still up for debate. I have not (yet) adopted `Microsoft.Extensions.Logging` in my own libraries, but it is my logging abstraction of choice when writing applications - even those that have absolutely nothing to do with ASP.NET Core.

Next time, we'll dive into the main types and terminology used by `Microsoft.Extensions.Logging`.
