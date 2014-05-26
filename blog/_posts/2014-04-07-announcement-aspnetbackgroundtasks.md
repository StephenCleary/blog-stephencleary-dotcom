---
layout: post
title: "Announcement: AspNetBackgroundTasks NuGet library"
---
<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

**Update, 2014-05-07:** This library just won the "shortest lifetime" award. One month after I released this library, [.NET Framework 4.5.2 introduced `HostingEnvironment.QueueBackgroundWorkItem`](http://msdn.microsoft.com/en-us/library/ms171868(v=vs.110).aspx#v452){:.alert-link}, which effectively rendered this library obsolete. On .NET 4.5.2, you can use the new API instead of the `BackgroundTaskManager.Run` in the AspNetBackgroundTasks library. However, it's still almost always the wrong solution.
</div>

Yielding to popular demand - and against my better judgement - I have created a NuGet package for the code [I previously wrote]({% post_url 2012-12-13-returning-early-from-aspnet-requests %}) for handling "fire-and-forget" tasks in ASP.NET.

Why is this against my better judgement? Because it's almost always the wrong solution, and making the wrong solution easy is digging a pit of failure rather than a pit of success. However, a lot of people want to use it anyway (and many people are actually using _worse_ solutions because I didn't make this one easy), so I put in plenty of warnings and published it [on GitHub](https://github.com/StephenCleary/AspNetBackgroundTasks) and [on NuGet](https://www.nuget.org/packages/Nito.AspNetBackgroundTasks/).
