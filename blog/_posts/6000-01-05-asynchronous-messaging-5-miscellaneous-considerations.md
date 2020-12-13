---
layout: post
title: "Asynchronous Messaging, Part 5: Miscellaneous Considerations"
series: "Asynchronous Messaging"
seriesTitle: "Miscellaneous Considerations"
description: "Final remarks on considerations for using asynchronous messaging with request-exogenous code."
---

## Mix and Match

## Duplicate and Lost Messages

## Poison / Dead Letter Queues

## Versioning

Mix and Match
- Hangfire: database and in-proc solution. Issues: rolling upgrades, serialization details. TODO: retrieval?
- others?
Further considerations
- Independent scaling
- CAP: either duplicates or lost messages must be possible. Most solutions try for duplicates, and lost messages are only for truly massive outages.
- Poison/deadletter queues.
- Version the message DTOs.
