---
layout: post
title: "Asynchronous Messaging, Part 4: Retrieve Results"
series: "Asynchronous Messaging"
seriesTitle: "Retrieve Results"
description: "When you need to retrieve results, and methods for doing so."
---

Retrieve results:
- Polling.
  - HTTP standards, e.g., 302.
    https://docs.microsoft.com/en-us/azure/architecture/patterns/async-request-reply
- SignalR / WebSockets
- None (e.g., email *is* the eventual result).
- None (e.g., status is exposed in the normal UI, and the end-user does manual polling).
Mix and Match
- Hangfire: database and in-proc solution. Issues: rolling upgrades, serialization details. TODO: retrieval?
- others?
Further considerations
- Independent scaling
- CAP: either duplicates or lost messages must be possible. Most solutions try for duplicates, and lost messages are only for truly massive outages.
- Poison/deadletter queues.
- Version the message DTOs.
- In-proc considerations: notify ASP.NET; clean shutdown.
  - Request-extrinsic.
