---
layout: post
title: "Announcement: AspNetBackgroundTasks NuGet library"
---
<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

**Update, 2014-06-07:** There is more information in [a newer post]({% post_url 2014-06-07-fire-and-forget-on-asp-net %}){:.alert-link}.
</div>

Yielding to popular demand - and against my better judgement - I have created a NuGet package for the code [I previously wrote]({% post_url 2012-12-13-returning-early-from-aspnet-requests %}) for handling "fire-and-forget" tasks in ASP.NET.

Why is this against my better judgement? Because it's almost always the wrong solution, and making the wrong solution easy is digging a pit of failure rather than a pit of success. However, a lot of people want to use it anyway (and many people are actually using _worse_ solutions because I didn't make this one easy), so I put in plenty of warnings and published it [on GitHub](https://github.com/StephenCleary/AspNetBackgroundTasks) and [on NuGet](https://www.nuget.org/packages/Nito.AspNetBackgroundTasks/).

