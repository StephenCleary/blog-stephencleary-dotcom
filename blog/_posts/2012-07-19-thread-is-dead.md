---
layout: post
title: "Thread is Dead"
---
At this point, it should be clear that Tasks are far superior to BackgroundWorkers. In fact, both Thread and BackgroundWorker just make things harder.

This post (and most of my other [async posts]({% post_url 2009-01-24-announcing-release-of-nitoasync %})) are from a talk I gave in October 2011 called "Thread is Dead". Here's the punchline slide:

{:.center}
![]({{ site_url }}/assets/Thread%20is%20Dead.png)

Friends don't let friends use Thread. Or BackgroundWorker. It is time for these classes to go the way of "lock (this)" and "Application.DoEvents".

...

OK, OK, you can use a thread _if_ you need to implement a specific scheduling context for a task that isn't already provided. But the _only_ case I can think of for this is if you need an STA context - and in that case, you can use [AsyncContextThread](http://nitoasyncex.codeplex.com/wikipage?title=AsyncContextThread).

