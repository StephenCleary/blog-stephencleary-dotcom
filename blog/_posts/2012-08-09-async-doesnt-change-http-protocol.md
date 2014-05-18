---
layout: post
title: "Async Doesn't Change the HTTP Protocol"
tags: ["async", ".NET", "ASP.NET"]
---


In my experience, there are two questions commonly asked by programmers new to async after they've learned the basics.





The most-asked question is ["why does my partially-async code deadlock?"](http://blog.stephencleary.com/2012/07/dont-block-on-async-code.html).





The second-most-asked question usually takes a form like this: "I have a long-running method on my web service. I made it async, but it doesn't return back to the client when it awaits!" Or sometimes like this: "I have to call this web service. How do I use async with progress reporting to get updates before the call completes?"





The answer is: Async doesn't change the HTTP protocol.





The HTTP protocol is centered around a request and a matching response. That's it. There's no progress reporting, or multiple responses per request, or anything like that. If you need progress reporting, or immediate responses, or some stateful concept of long-running operations on the server, then you need to build it yourself using a higher level of abstraction.





You can use async on the client and server, but that won't change the way the HTTP protocol works.





When you use async on the client side (e.g., with HttpClient), then you can treat the entire web service call as a single asynchronous operation. But you can't get progress support, because the HTTP protocol doesn't support it.





When you use async on the server side (e.g., with ApiController), then you can treat each web request as an asynchronous operation. But when you yield, you only yield to the web server thread pool, not to the client. HTTP only allows a single response, so the response can only be sent when the request is fully complete.





Now, you can _use_ async to create higher-level abstractions. For example, you can invent the notion of a "workflow" on the server: a POST call could create the workflow and return a unique identifier, and a GET call could return the current status of the workflow item (e.g., percent complete). Once this is implemented on the server, you could wrap multiple HTTP calls and responses into a single async method on the client. This higher-level async abstraction could support progress notification via IProgress.



> Frameworks such as SignalR can make implementation easier.




There aren't too many examples of doing that these days; async is still pretty new. But I'm sure that higher-level async abstractions will become a common approach in the future.

