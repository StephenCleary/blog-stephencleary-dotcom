---
layout: post
title: "Asynchronous Messaging, Part 1: Basic Distributed Architecture"
series: "Asynchronous Messaging"
seriesTitle: "Basic Distributed Architecture"
description: "Solving request-extrinsic problems with a distributed architecture."
---

This is the first of a short series of blog posts on Asynchronous Messaging. This is not a new problem at all, but it's something I've observed becoming more and more common over the last few years. Also, this is the kind of a problem that is difficult to solve quickly - or even _describe_ the solution quickly, so I think a blog (series) is appropriate.

A bit of a side note, here: I primarily develop in the .NET stack these days (for the backend, at least). So some of my details will discuss ASP.NET-specific technologies and solutions. However, the general problem and solution is applicable to **all** technology stacks. I'll call out the ASP.NET-specific parts as I cover them.

So first, I'd like to describe how this problem usually manifests, and then I'll discuss the solution. Sorry for the somewhat repetitive nature in this first "problem" section; I fully admit that I'm primarily writing this section for Google.

## The Problem

The problem usually manifests in a desire to **return early** from an HTTP request. So, once the request has been received, the developer wants the server-side API application to **not wait** for the processing to complete, and instead send the response back immediately.

A common term for this is **fire and forget**, in the sense that the developer wants to start ("fire") some background work but then not wait for it to complete ("forget").

The goal is to have the HTTP call just **start a workflow**. This workflow then runs on the server side without further input from the client application. It's a form of **long-running operation**.

I've decided to call this "request-extrinsic code", because it sounds fancy. "Request-extrinsic" means that it's code that runs *outside* of a request. This is fundamentally dangerous, which is why the solution is more complex than at first seems necessary.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

**ASP.NET-specific:** In ASP.NET pre-Core, there is an exception that developers may trigger with the message "An asynchronous module or handler completed while an asynchronous operation was still pending." This is a "safety net" exception that indicates there's some request-extrinsic code (which is dangerous). Unfortunately, ASP.NET Core does not have this "safety net" check, even though request-extrinsic code is just as bad on ASP.NET Core as it was on ASP.NET pre-Core.
</div>

## The Solution

The proper solution for request-extrinsic code is **asynchronous messaging**.


<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

My blog often deals with "asynchronous" in the sense of the `async`/`await` keywords. This blog series is on *asynchronous messaging*, which is a completely different thing. `async`/`await` deal with asynchrony within the scope of a single process; asynchronous messaging deals with asynchrony across two processes (client and server). The two uses of "asynchronous" are similar at the 10,000 foot view, but completely different on the ground.
</div>

Asynchronous messaging has two parts (with an optional third part):

1. A *durable queue*. By "durable", I mean a queue that at least flushes to disk on writes. In other words, the *messages* sent to the queue are durable. An in-memory `Queue<T>` or `BlockingCollection<T>` or `ChannelWriter<T>` is not a "durable queue" by this definition.
1. A *backend service*. This is an independent service that reads from that durable queue and processes the items in it (i.e., executes the long-running operation).
1. (optional) Some method to *retrieve results*. If the client needs to know the outcome of the long-running operation, then this is the part that provides that outcome to the client.

One common example is sending emails. If an API wants to send an email but does not want to wait for the email to be sent before returning to the client, then the API should add a message to the durable queue describing the email to be sent and then return. Since this is a durable queue, the queue message (containing the email details) is flushed to disk before the HTTP response is sent to the client. Then a separate backend service reading from that queue retrieves the queue message and sends the actual email.

Another common example is database writes. Sometimes there are situations where the API knows what to write to the database but doesn't want to make the client wait for it. In that case, the API should write the information to a durable queue and then return to the client. Then a separate backend service reading from that queue retrieves the information and performs the actual database update.

Retrieving results is often not necessary. E.g., the email itself usually *is* the result of sending an email, and database writes will show up eventually as the user navigates/refreshes. But sometimes you do need the client to be notified of results; this is possible either using polling or a proactive notification using a messaging technology like WebSockets.

In the rest of this series, I'll dive more into each parts of the solution, and discuss specific approaches in more detail. But the description above is usually all that's needed.

The proper solution for request-extrinsic code is **asynchronous messaging**, which is accomplished by adding a **durable queue** coupled with a **backend service**.
