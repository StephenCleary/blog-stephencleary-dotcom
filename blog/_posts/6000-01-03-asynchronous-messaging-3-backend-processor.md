---
layout: post
title: "Asynchronous Messaging, Part 3: Backend Service"
series: "Asynchronous Messaging"
seriesTitle: "Backend Service"
description: "A discussion of backend processing services, with multiple examples."
---

The proper solution for request-extrinsic code is asynchronous messaging, which has two primary parts: a reliable queue and a backend service. Today I'm going to discuss the backend service.


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
    https://docs.microsoft.com/en-us/azure/architecture/patterns/async-request-reply
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
  - Request-extrinsic.