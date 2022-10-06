---
layout: post
title: "Cancellation, Part 5: Registration"
series: "Cancellation"
seriesTitle: "Registration"
description: "Responding to cancellation requests by using registration."
---

Last time in this series I talked about how to respond to cancellation requests by polling for them. That's a common approach for synchronous or CPU-bound code. In this post, I'm covering a pattern more common for asynchronous code: registration.

Registration is a way for your code to get a callback immediately when cancellation is requested. This callback can then perform some operation (often calling a different API) to cancel the asynchronous operation.

## How to Register

Your code can register a callback with any `CancellationToken` by calling `CancellationToken.Register`. 

### A Race Condition

## Cleanup Is Important!

## Sharp Corner: Synchronous Cancellation Callbacks

## Summary
