---
layout: post
title: "Asynchronous Messaging, Part 5: Miscellaneous Considerations"
series: "Asynchronous Messaging"
seriesTitle: "Miscellaneous Considerations"
description: "Final remarks on considerations for using asynchronous messaging with request-exogenous code."
---

This entry in my asynchronous messaging series is kind of a grab bag of miscellaneous topics. I don't really have enough to say on these to make them their own posts, but some are too important to drop completely. So, here we go!

## Poison / Dead Letter Queues

When designing your system, you need to decide how to handle queue messages that insist on failing to process. Usually, you plan for some kind of "dead letter queue" to hold these "poison" messages, and then set up alerting or something on that queue. Many cloud queue/worker systems will automatically do this for you: after a certain number of retries, the problematic message is removed from the regular queue and sent to a poison queue instead.

Just don't forget to set up alerting on those poison queues!

## Versioning

When I design asynchronous messaging systems, I tend to treat the queued messages as a kind of Data Transfer Object (DTO). These messages act as a bridge between two processes: the HTTP application and the backend processor.

Just like the rest of the system, the DTOs will change over time, and it's best to be prepared for that. Unlike an HTTP stack, there's no versioning possible in the URL or in a header. I tend to prefer versioning in the queue name itself, but you could also embed versions in the DTOs themselves. Generally, "storage DTOs" like these only require a single version number (i.e., they only have a major version, not a minor/patch version); this is because you explicitly *don't* want an older consumer to process newer queue messages.

## Mix and Match

I've pointed out that the cloud queue solutions work out of the box with the cloud backend processor solutions from the same company, including automatic scaling. But you don't *have* to use the same provider for each part of your asynchronous messaging architecture. It's entirely possible to, e.g., scale Azure Functions based off a RabbitMQ, or wire up a Google Cloud hosted Docker backend to an Amazon SQS queue. Sometimes there are extra costs when crossing cloud providers, and sometimes you have to write a plugin so that your backend will use your kind of queue for its scaling; but it's certainly possible to mix and match.

## All-In-One

And then there is the other side: some solutions are all-in-one, complete solutions for asynchronous messaging. Examples of all-in-one solutions are [Hangfire](https://www.hangfire.io/) (.NET) and [Delayed Job](https://github.com/collectiveidea/delayed_job) (Ruby). Their all-in-one nature means they are easier to set up, but inevitably also means they are less flexible. There are also some very serious considerations you need to look into before adopting an all-in-one solution; what the developers of that solution created may be very different than what your application needs.

Specifically, you need to look into:

1. Does it use a durable queue? If not, I would not even consider it. As a corollary, anything that uses Redis as a queue should not be used in its default configuration, including some very popular solutions such as Sidekiq (Ruby) and Bull (NodeJS). If you do wish to use Redis-based message queues, then you should configure Redis to be durable by telling it to write an Append Only File *and* telling it to sync that file on every command. Both Hangfire and Delayed Job use a database as a queue, which is *just ok* (assuming you already have a database in your architecture), but not *ideal* (now your database server has to deal with all the queue messages as well as its normal data).
1. How are jobs serialized - is the serialization backwards-compatible when your library is updated? As an example of this, until recently (early 2019), Hangfire *did not support rolling upgrades* due to the way they serialized jobs. Before that time, Hangfire-based applications had to shut down completely before rolling out a Hangfire upgrade, and if a rollback was necessary, they had to shut down completely before doing the rollback, too. Ouch!
1. How are jobs serialized - how much can the "runner" code change? My experience here is more with Hangfire: .NET is pretty specific when it comes to serializing method delegates, and even something like adding a parameter (with a default argument) can cause a failure. Any change like that now requires *two* updates instead of one: the first will add the new overload, and once the old jobs have all completed, a second update can roll out to remove the old overload. I'm not as familiar with Delayed Jobs; Ruby is a more dynamic language, so it may not have this problem.
1. How are errors handled? Most all-in-one solutions will automatically retry, but if the job message insists on failing, then it has to do something else with it. By default, Hangfire will leave those job messages in a "failed" state (and it's up to you to build some kind of notification on that), whereas Delayed Job will *delete* those jobs (!).

In conclusion, caveat emptor. Don't just slap an all-in-one solution into your architecture; a well-thought-out, proper asynchronous messaging solution is almost always the better choice.
