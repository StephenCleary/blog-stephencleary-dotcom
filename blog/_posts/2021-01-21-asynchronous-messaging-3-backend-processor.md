---
layout: post
title: "Asynchronous Messaging, Part 3: Backend Service"
series: "Asynchronous Messaging"
seriesTitle: "Backend Service"
description: "A discussion of backend processing services, with multiple examples."
---

The proper solution for request-extrinsic code is asynchronous messaging, which has two primary parts: a durable queue and a backend service. Today I'm going to discuss the backend service.

## Backend Services

The purpose of the background service is to process the queue messages. When the HTTP application wants to return before the processing is complete, it queues a message to the durable queue and then returns. The background service is the other side of that pipe; the HTTP application is the producer putting messages into the queue, and the background service is the consumer retrieving messages from the queue and processing them.

I usually recommend that the background service is **independent** from the HTTP application, but it doesn't strictly have to be.

One reason for having a separate service is that they can be scaled independently; if they were in the same application, then scaling out the HTTP application means the background service is also scaled out. Independent services means each one can be scaled as needed: the HTTP application scales based on HTTP requests, and the background service scales based on queue messages.

Another reason for having a separate service is that having an independent service *requires* the use of an external queue. If the background service is in the same process as the HTTP application, it's not obvious that an in-memory queue is an inappropriate solution, and a future code maintainer may change the queue to be in-memory as an attempted optimization.

A final reason for having an independent background service is that the background service itself affects how the HTTP application can be shut down. The background service consists entirely of request-extrinsic code, so special care must be taken to allow proper shutdowns.

For all these reasons, I recommend that background services are separate, independent services. But regardless of whether the background service is independent or sharing the same process as the HTTP application, it must process its messages idempotently.

## Idempotency

An operation is **idempotent** if it can be applied multiple times and produce the same result each time. In other words, once an idempotent operation has been applied, then future applications of that same operation are noops. Ideally, the background service should process its messages idempotently.

This is necessary just due to the realities of durable queues. The CAP theorem has all the details, but the takeaway for modern distributed computing is that durable queues will deliver their durable messages **at least once**. This means that the background service may get the same message more than once.

Ideally, you should try to structure your processing code so that receiving the same message more than once is a noop. Sometimes this means capturing more of the "state" of the system at the time the message is queued, and including that additional state in the queue messages.

Sometimes idempotency just isn't possible. Or easy, at least. In that case, you can use **de-duplication** to explicitly check for duplicate messages over a reasonable time period. Idempotent processing is best, and de-duplication is a reasonable fallback.

## Examples of Backend Services

Just like durable queues, my go-to solutions for backend services are cloud solutions, specifically Functions as a Service (FaaS). FaaS is a perfect fit for background services, and even more so if you're using a cloud-based durable queue. All the major cloud providers have built-in support for combining their durable queues with their FaaS offerings. This includes scaling logic: each cloud provider will auto-scale FaaS consumers based on their queue messages.

[Azure Storage Queues](https://azure.microsoft.com/en-us/services/storage/queues/) pair with [Azure Functions](https://azure.microsoft.com/en-us/services/functions/); [Amazon Simple Queue Service (SQS) queues](https://aws.amazon.com/sqs/) pair with [AWS Lambdas](https://aws.amazon.com/lambda/); and [Google Cloud Task queues](https://cloud.google.com/tasks) pair with [Google Cloud Functions](https://cloud.google.com/functions). Cloud pairs such as these are the easiest way to implement a full asynchronous messaging solution.

For on-premises solutions, one natural approach is a [Win32 service](https://docs.microsoft.com/en-us/windows/win32/services/services) (if on Windows) or a [Linux daemon](https://www.man7.org/linux/man-pages/man7/daemon.7.html) (if on Linux). These are background services that run all the time the server machine is on, regardless of whether a user is logged in. They are headless processes that do not permit direct user interaction. This makes background services a natural choice for both HTTP applications and the background processing services.

Another possible solution is a regular Console application wrapped in a Docker container. Docker containers can be deployed either on-premise or in the cloud. This is a similar approach to the Win32 service / Linux daemon approach, but has better support for scaling out. Right now, Docker orchestrators are getting more support for scaling based on queue messages; setting up an autoscaler based on RabbitMQ messages is tedious but possible.

The final - and least recommended solution - is to have the backend service run as a part of the HTTP application. As noted above, there are a few disadvantages to this approach: it cannot scale independently, the resulting architecture implies that an in-memory queue is acceptable, and it complicates shutdown handling. However, some teams do choose this option in spite of the drawbacks. When putting the backend service in the same process as the HTTP application, the host *must* be notified of the background work, or else work may be lost. Also, any "upstream" systems like HTTP proxies, load balancers, and deployment scripts may require changes so that they are also aware of the non-standard shutdown rules.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

**ASP.NET-specific:** In ASP.NET Core, use `IHostedService`, or `IHostApplicationLifetime` to detect and block HTTP application shutdown. `BackgroundService` can also be used, but be aware that work may be lost if the host shutdown times out. In ASP.NET pre-Core, use `HostingEnvironment.QueueBackgroundWorkItem` or `IRegisteredObject`. Again, this is for the *least recommended solution* and you'll *also* need to consider the impact on proxies, load balancers, and deployment systems. A far better solution would be to make the backend processing service independent (in its own process), and not change the HTTP shutdown rules at all.
</div>
