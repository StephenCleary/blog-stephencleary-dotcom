---
layout: post
title: "Rx and Async"
tags: ["Rx", "async", ".NET"]
---


I saw some rather shocking tweets yesterday from the BUILD conference:

{:.center}
![](http://4.bp.blogspot.com/-pGyY6tjvz7o/TnNG8fH56qI/AAAAAAAAFv8/j0nAv_Orhb4/s1600/AndersNoRx.PNG)  


The author of that original tweet followed up with [a blog post]() with some interesting Rx-related quotes from Anders Hejlsberg: "I don't know if we've decided [whether Rx will be included in future versions of .NET]." and "Personally I've found the stuff we've done with async **allows you to do a lot more** [than Rx]." (emphasis mine).



Interesting. I tried to take a listen for myself, but the Channel9 live interview was no longer available. Note that these remarks were made during a live interview, and were not part of a presentation; I'm hoping that Anders just answered off the cuff and didn't mean it.



One reason I found those quotes controversial is because parallel programming (TPL/PLINQ), background operations (async/await), and asynchronous streams (Rx) all address different problems. In particular, Async _only_ supports background operations and does _not_ support asynchronous streams. Rx supports both, but Async will become the default solution for background operations because it's easier to use than Rx.



So, I agree with Anders that Async is easier to use, but I totally disagree that Async is more powerful. Rx can do everything Async can do, and can do some things that Async _can't_ do.



It comes down to the difference between _asynchronous operations_ and _asynchronous events_. An asynchronous operation is something that my program can start, and it will complete some time later. An asynchronous event stream is something that is happening all the time independent of my program; it can subscribe and unsubscribe, but does not _cause_ the events. This is an important distinction if you consider an event stream that produces in quick bursts (e.g., mouse movement); Rx allows collating all of those events, but an async-based solution may miss some (because it has to restart the operation each time it completes).



Historically, asynchronous _events_ have been a blind spot for Microsoft. Consider a condensed history of asynchronous support:

1. **Asynchronous Programming Model (APM)**. In the beginning, there was only IAsyncResult (Begin/End). The APM was everywhere, even baked into delegate types. The thing to note about APM is that it is purely an asynchronous operation; no asynchronous events are supported. The program starts the operation, which has a single point of completion.
1. **Event-Based Asynchronous Pattern (EAP)**. Way back in .NET 2.0, the EAP was introduced. EAP works by capturing the current SynchronizationContext and then raising events on that context. This was the first asynchronous pattern that supported both asynchronous operations and asynchronous events. Unfortunately, the documentation _assumed_ that EAP objects are only implementing asynchronous operations, and completely ignored the EAP support for asynchronous events. In addition, the most famous EAP implementation (BackgroundWorker) was just an asynchronous operation. However, the [Nito.Async](nitoasync.codeplex.com) library included some helpers for EAP components, and included sample socket components using EAP in an asynchronous event fashion.
1. **Rx**. Supporting .NET 3.5 and up, the Rx libraries are all about asynchronous events (and they also support asynchronous operations, which are just a singleton asynchronous event). Rx is also more powerful than EAP because it has a very flexible execution context, while EAP ties everything through a single SynchronizationContext. However, the learning curve for Rx is steep.
1. **Async/await and the Task-Based Asynchronous Pattern (TAP)**. These extensions to the language allow for a very natural and easy way to deal with asynchronous operations, but they do not support asynchronous events.


In terms of _power_ and _flexibility_, TAP is approximately equivalent to APM (less powerful than EAP and Rx). The only reason it's a step _forward_ is because it is so easy to learn and use. Some simple programs may use only TAP, but other programs will need both TAP and Rx.



Rx is a very welcome (and necessary) addition to our toolset. Async does not and can not replace it.



(P.S. All of this - and much more - is covered in my "Thread is Dead" talk, which has been submitted for consideration at a couple of conferences in the next few months. I'll update this space when it's accepted.)

