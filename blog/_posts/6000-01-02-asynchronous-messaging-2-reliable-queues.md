---
layout: post
title: "Asynchronous Messaging, Part 2: Reliable Queues"
series: "Asynchronous Messaging"
seriesTitle: "Reliable Queues"
description: "The definition of a reliable queue, with multiple examples."
---

Last time I concluded that the proper solution for request-extrinsic code is asynchronous messaging, which has two primary parts: a reliable queue and a backend service. Today I'm going to discuss reliable queues.

## The Definition of "Reliable"

When I suggest the asynchronous messaging solution, I try to emphasize the "reliable" in "reliable queue". But what does "reliable" really mean? There's a range of meanings, from what I would consider "minimum viable" to "paranoid".

A reliable queue must *at least* write the new item to disk when it is placed into the queue. In other words, a reliable queue is one that stores **durable messages**. This is the minimum viable behavior for a reliable queue: messages must survive shutdowns. Asynchronous messaging *must* use a queue whose messages survive shutdown. This is good enough for many (most?) applications.

A more reliable queue would be one that writes to *multiple* disks. This allows the messages to also survive a single disk failure. An even more reliable queue would be one that writes to disks on multiple *servers*; this allows the messages to survive a complete server failure. Finally, the most paranoid reliable queues write to multiple servers in distinct geographical locations; this allows the message to survive the destruction of an entire data center. Most applications do not require that level of reliability.

But it's important to note that the minimum acceptable reliability is writing to disk. In-memory queues are *not* reliable enough for asynchronous messaging; this includes `Queue<T>`, `Channel<T>`, and `BlockingCollection<T>`, the three most common in-memory queues. When I say they are not "reliable", I don't mean that they can't be used as in-memory queues; they're perfectly fine for that scenario. However, since their messages are not durable (i.e., will not survive shutdown), they do not provide sufficient reliability for use with asynchronous messaging.

## The Problem with In-Memory Queues

I'm going to dive into this in a bit of detail, because this is a common point of confusion. I've defined "reliable" as meaning at least "on disk", and I've used the reasoning that "asynchronous messages must survive shutdown". This section will go into more detail of the reasoning behind this restriction.

I think it's easiest to understand this by contemplating one question: "When is it safe for an HTTP service to shut down?"

The HTTP protocol is ubiquitous; it's used by all kinds of APIs and web services. And there's a seemingly endless number of conventions and standards built on top of HTTP. With all these details and abstractions, sometimes one critical truth is forgotten: **the HTTP protocol is a request/response protocol**. In other words, for every request there is exactly one response. From the HTTP service's perspective, a request arrives, and then some time later the response is sent and that request is completed.

Back to that question: "When is it safe for an HTTP service to shut down?" The easiest possible answer is "when a response for each request has been sent." Or to word the same idea a different way: "when there are no more outstanding requests."

This is such a natural answer to the question that *every* HTTP server has this as its default answer. It doesn't matter if you're on ASP.NET, Node.js, Ruby on Rails, ... *Every* HTTP server framework keeps track of how many outstanding requests it has, and considers itself "safe to shut down" when that number reaches zero. This also holds true for load balancers and proxies: "When is it safe to remove this HTTP server from my list?" - "When it has no more requests waiting for responses." It doesn't matter if you're using nginx, HAProxy, Kubernetes' apiserver proxy, ... *Every* HTTP proxy application keeps track of the number of outstanding requests, and considers HTTP servers "done" when they have sent out responses for all of their requests.

This is why request-extrinsic code is dangerous: all of this default behavior is suddenly wrong. The HTTP service says it's safe to shut down when it's not safe to shut down; all the proxies and load balancers say it's safe to take that machine out of rotation when it's not.

### Shutdowns Are Normal

Often developers react to this by trying to force alternative solutions. All HTTP server frameworks answer "When is it safe to shut down?" with "When there are no more outstanding requests" *by default*, but many of them allow overriding that default so the application itself can answer "It's only safe to shut down when I say it's safe to shut down."

One of the problems with trying to force that alternative solution is that it only changes the answer at the HTTP service level; proxies and load balancers *also* need to have their default logic changed (assuming that changing the default is even possible). Even if you get that working, there's an unending maintenance problem: your HTTP server farm now handles shutdowns *completely differently* than all other HTTP server farms.

When developers begin down this path, it is usually because the developer wants to keep their HTTP application running indefinitely. And this is a major misunderstanding: in reality, systems are more reliable if servers do *not* run indefinitely. In fact, shutdowns are *normal*, and we need to accept shutdowns as a normal part of life.

One example is rolling updates. When a new version of an HTTP application is developed, it needs to replace the old versions of that application. The normal way to do this is via rolling updates: for each server, the upstream proxy will stop forwarding new requests, wait until the service has no more outstanding requests, shut it down, install the update, start it up, and start forwarding new requests. Shutdowns are necessary to perform rolling updates.

Another example is applying operating system or runtime patches. This is similar to rolling updates, but in this case it is the lower layers that is being updated, so it's machine-wide. The same steps apply, though: stop forwarding new requests to all services on that machine, wait until the machine has no outstanding requests, shut down all the services, install the patches (rebooting if necessary), start up all the services, and start forwarding new requests. This kind of shutdown occurs regularly, even for HTTP services that are not in active development.

A final example is that some frameworks and host processes just do periodic application restarts just to keep things clean. For example, Apache's `MaxConnectionsPerChild` or IIS's `periodicRestart` can recycle child processes periodically. This is primarily useful for managing memory leaks in applications, frameworks, and/or libraries. Apache no longer recycles by default, but IIS still does. Again, this is based on the number of outstanding requests: the server will recycle its child application when it has no outstanding requests.

The reasonable conclusion is that *shutdowns are normal*. All HTTP applications must work correctly when shutdowns occur. Correlation: All software that assumes it will never shut down is inherently buggy.

Finally, we return to what "reliable" means. In-memory queues cannot survive shutdowns. Therefore, "minimum acceptable reliability" means that the queue of work survives shutdowns, which are normal and common.

## Examples of Reliable Queues

I tend to prefer cloud queues whenever possible, because the cloud provider manages them, they scale really well, and they give you knobs for controlling how paranoid you want your reliability to be.

For this reason, my top go-tos for reliable queues are [Azure Storage Queue](https://azure.microsoft.com/en-us/services/storage/queues/), [Amazon Simple Queue Service (SQS)](https://aws.amazon.com/sqs/), and [Google Cloud Tasks](https://cloud.google.com/tasks). I'm most familiar with Azure's queueing, though I have also used Amazon's in production systems. All cloud queueing systems provide reliable queues that can scale out automatically.

As much as I like cloud solutions, on-premises queueing systems are perfectly viable. It's not possible to get the same scaling capabilities as a cloud solution, but you can get lower latencies. The most common on-premises reliable queue these days is [RabbitMQ](https://www.rabbitmq.com/). I've also used [IBM MQ](https://www.ibm.com/products/mq) (called WebSphere MQ at the time). For older Windows systems, [Microsoft Message Queueing (MSMQ)](https://docs.microsoft.com/en-us/previous-versions/windows/desktop/msmq/ms711472(v=vs.85)) was common, though that is no longer recommended these days. Note that some on-premises queueing solutions do not use durable messages by default, so some configuration is necessary to make them actually reliable.

There are other solutions for both cloud and on-premises. The ones mentioned here are just ones I've had experience with, and which appear to be the most common.

### Database As a Reliable Queue

One other solution that is sometimes used is an actual database. Usually, this needs to be a database that guarantees ACID. Some NoSql databases can also be used as reliable queues, as long as they actually have reliable writes; but be aware that some NoSql databases can lose writes, in which case they do not qualify as reliable queues. In my experience, all databases used as reliable queues have been fully ACID (i.e., transactional).

Using an ACID database as a reliable queue allows you to use the Outbox Pattern. When a publishing service wants to publish a message *if and only if* a particular database transaction succeeds, then it writes that message to the database *as part of that transaction*. It can't publish the message before doing the database update, because the database update may fail; and it can't publish the message after doing the database update, because if there's some problem reaching the reliable queue then the message wouldn't be published. So, by using the database itself as a reliable queue, then the publishing service guarantees that the message will be published if and only if the database update takes place.

The Outbox Pattern gets its name because there's usually a separate "outbox" table that just holds messages that are published. It's possible to have the queue consumer read the outbox table directly, but a more common solution is to have the outbox table just act as temporary storage for messages on their way to another reliable queue. In that case, the publishing service (or another service) reads the messages from the outbox table, sends then to the reliable queue, and then deletes those messages from the outbox table. This provides an at-least-once delivery of the messages stored in the outbox table.
