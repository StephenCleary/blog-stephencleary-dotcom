---
layout: post
title: "Asynchronous Messaging, Part 1: Basic Distributed Architecture"
series: "Asynchronous Messaging"
seriesTitle: "Basic Distributed Architecture"
description: "An explanation of when to know you need a distributed architecture."
---

Intro:
- Problem:
  - "Return early"
  - "Don't want to wait"
  - Start a workflow
  - Long-running operation
  - Error messages:
    - Return when async op is pending.
- Quick Answer:
  - Reliable queue.
  - Backend processor.
  - (optional) Retrieve results.
Reliable queues:
- Azure Storage Queue.
- Amazon Simple Queue Service (SQS).
- Google Cloud Tasks.
- RabbitMQ
- MSMQ (Microsoft Message Queueing)
- IBM WebSphereMQ
- Database (ACID/relational, so it has reliable writes)
  - Nice for "send message iff transaction succeeded"
  - TODO: link to blog.
Backend processor:
- Azure Functions
- Amazon Lambda
- Google Cloud Functions.
- Win32 service or Linux daemon.
- Separate process.
  - Dockerized process.
- In-proc solutions.
  - .NET Core background worker / service.
  - ASP.NET pre-core?
Retrieve results:
- Polling.
  - HTTP standards, e.g., 302.
- SignalR / WebSockets
- None (e.g., email *is* the eventual result).
- None (e.g., status is exposed in the normal UI, and the end-user does manual polling).
Mix and Match
- Hangfire: database and in-proc solution. TODO: retrieval?
- others?
Further considerations
- Independent scaling
- CAP: either duplicates or lost messages must be possible. Most solutions try for duplicates, and lost messages are only for truly massive outages.
- Poison/deadletter queues.
- Version the message DTOs.
- In-proc considerations: notify ASP.NET; clean shutdown.

