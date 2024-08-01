---
layout: post
title: "ICYMI: Video Series on TCP/IP Application Protocol Design"
description: "In case you missed it: I did a (free) video series about TCP/IP protocol design"
---

I speak regularly at various programming conferences, and one of my favorite talks is actually on TCP/IP protocol design. This is a skill that I learned almost by accident (literally because _someone_ had to, and no one else in the company _wanted_ to). It's always been one of my favorite talks, and I submit it all over the place. Most of the time it's rejected. Which I get - protocol design is esoteric and just plain useless for most developers. But I think it's still _fun!_

A bit ago I decided to take everything I had ever learned about TCP/IP protcols and live-code a server and client. So, all the knowledge that goes into my talk is also in this video series. I chose a basic chat server, since the requirements are simple and easily understood. I also wanted to try out some of the newer .NET APIs. Specifically, the final solution uses:

- `async` and `await` - naturally!
- `Socket` - the Berkeley-ish API for socket communication.
- `System.IO.Pipelines` - for low-level buffer management.
- `System.Threading.Channels` and `IAsyncEnumerable` - for asynchronous streams of messages.
- A dictionary of `TaskCompletionSource` instances - to implement higher-level request/response APIs.

Someday I'd like to update this to use QUIC (`System.Net.Quic`). Let me know if that's something you'd be interested in!

The full video series (over 16 hours!) is [available for free on YouTube](https://www.youtube.com/playlist?list=PLIebvSMVr_dehKSoq6vuAW0BGEM6QnDlS), and the code is [on GitHub](https://github.com/StephenClearyExamples/TcpChat). Enjoy!