---
layout: post
title: "Historical Asynchronous PushStreamContent"
description: "An odd historical note regarding PushStreamContent."
---

Last time, [we looked at using `PushStreamContent` with an asynchronous delegate to zip up files on-demand with a fully-asynchronous system]({% post_url 2016-10-20-async-pushstreamcontent %}).

I didn't want to complicate that post by mentioning the odd history of asynchronous `PushStreamContent` use, but I personally find it - er, "interesting." :)

Today, `PushStreamContent` takes an [asynchronous delegate]({% post_url 2014-02-20-synchronous-and-asynchronous-delegate %}) (`Func<Stream, HttpContent, TransportContext, Task>`), and it finishes sending the response when that asynchronous delegate completes. This is the natural way of supporting asynchronous stream writes.

Historically, `PushStreamContent` was a lot weirder. It always supported asynchronous writing of the response stream, but the way it used to do so was odd, to say the least.

When `PushStreamContent` was first introduced, it only allowed *synchronous* delegates (`Action<Stream, HttpContent, TransportContext>`). It wasn't really obvious, but it *did* actually support asynchronous usage. What you had to do was pass an asynchronous lambda expression, which would actually get converted to an `async void` method. One of the main [problems with `async void` methods](https://msdn.microsoft.com/en-us/magazine/jj991977.aspx) is that it's very difficult for the caller to know when the `async void` method has completed.

`PushStreamContent` used to solve this in an unusual way: [`PushStreamContent` would consider its callback "complete" when it closed the output stream](http://stackoverflow.com/questions/15060214/web-api-httpclient-an-asynchronous-module-or-handler-completed-while-an-async). "Interesting," indeed.

This unusual design implies an unwritten rule: after closing the output stream, the callback really shouldn't do anything else.

Fortunately, [`PushStreamContent` has been changed to support asynchronous delegates](https://github.com/ASP-NET-MVC/aspnetwebstack/commit/262ec8b273e2c8b7a4ae4cc7d43ad8e3f9c36c64#diff-778a5a33d4cdc98ca84864b003b2c36c) as first-class citizens (as of [`Microsoft.AspNet.WebApi.Client` version 5.0.0](https://www.nuget.org/packages/Microsoft.AspNet.WebApi.Client/5.0.0)). Now it works just like any other asynchronous code, rather than having the odd "`async void` that completes when the stream is closed" behavior.

There's nothing really important in this blog post for modern code (the newer `PushStreamContent` went live 3 years ago this month) - just an interesting historical note.
