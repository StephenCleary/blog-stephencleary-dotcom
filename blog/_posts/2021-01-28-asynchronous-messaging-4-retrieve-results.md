---
layout: post
title: "Asynchronous Messaging, Part 4: Retrieve Results"
series: "Asynchronous Messaging"
seriesTitle: "Retrieve Results"
description: "When you do and do not need to retrieve results in an asynchronous messaging solution, and methods for doing so."
---

So far in this series, we've covered how asynchronous messaging can be implemented with a durable queue and a backend service. Those are the most necessary components of the solution, but another piece is sometimes necessary.

Many times, it's the desire to "return early" or "fire and forget" that start developers down the path of exogenous code and asynchronous messaging. Sometimes the client, as the originator of the request, wants to get the results of the long-running background operation.

## No Results Necessary

Before looking into delivering results, it's important to note that there are many scenarios that do not require explicitly sending results.

One common case for long-running operations is sending an email. In cases like these, the email itself *is* the actual result of the operation. So, there is no need for the original client to get a notification that the email has actually been sent; the email itself is the result.

Another case that doesn't need explicit results is when the human end-user will poll. Usually in this kind of scenario, some kind of status is exposed in the normal UI, and the end user will see the results sooner or later. Even most non-technical end-users know how to refresh the page when they are looking for updates to occur.

So, the first question to ask is whether retrieving results is actually necessary. In my experience, most asynchronous messaging solutions do not require explicitly retrieving results.

## Polling

If the client does need to detect the results of the asynchronous operation, then it can poll a "status" endpoint until the results are available, and then pull the final result (or error, if the operation failed). Most HTTP APIs these days are REST-based, so I'll describe here the most common approaches for implementing polling for asynchronous messaging completion.

Unfortunately, there's a wide variety of implementations for this kind of pattern. The one thing everyone agrees with is that the initial status response code should be `202 Accepted`. This is for the call that initiates the asynchronous messaging, so the HTTP application should put its message into a durable queue and then return `202 Accepted`.

The HTTP application should also return some kind of information that allows the client to poll for completion. This is usually done via some kind of "status" URI. The [actual standard](https://tools.ietf.org/html/rfc7231#section-6.3.3) just says "The representation sent with this response ought to describe the request's current status and point to (or embed) a status monitor that can provide the user with an estimate of when the request will be fulfilled." This is very open-ended, and this is where the implementations begin to diverge. One option is to return the status URI in the body of the `202 Accepted` (e.g., as a JSON property). Another option is to return the status URI in the `Location` header of the response.

Once the client has the status URI, it can begin calling that URI periodically for updates on that asynchronous operation. Again, implementations diverge on the details here, depending on whether the status is considered a "resource" in REST terms. As long as the operation is not complete, some implementations return `200 OK` with some kind of "incomplete" indicator in the body; other implementations return `202 Accepted` from the status URI. The server can also optionally include a "percentage complete" indicator in its response body, and/or a `Retry-After` header if it has an estimated time of completion (to discourage over-eager polling).

If the operation completes with an error, then the status URI can either return `200 OK` with the "error" in the response body (if treating the status as a "resource"), or it can return an appropriate error code (`4xx`/`5xx`) with optional error details in the response body. If the operation completes successfully, then the status URI should return `303 See Other` with the `Location` header set to the resource that was updated/modified by the asynchronous message. Alternatively, the status API could also return `200 OK`, `201 Created`, or `204 No Content` to indicate successful completion.

As you can see, there's considerable variation in how exactly asynchronous messaging is implemented from a REST API standpoint. There are no standards or widespread accepted pattern. I recommend not losing too much sleep over which one is "right", and just documenting clearly which approach you take for your API.

One final note: in order to retrieve results, your backend processing service must be sharing its progress with the HTTP application. At the very least, the HTTP application serving the status URIs must be able to know when the asynchronous message has been processed. A more complete implementation may need to share a `Location` URI, error details, progress percentage, and/or estimated time to completion. These "in progress details" could be in a shared database (and often are placed there), but it's not necessary that they be durable. It's fine to store in-progress details in an in-memory structure such as a shared cache.

## Notification

Polling (whether done by the user or by the client) is a perfectly valid solution for most cases. In some cases, however, your clients need to know *immediately* whenever the message has been processed. This is rare, but not unheard of. In this case, you wouldn't want to use polling, where you'd have to tradeoff how quickly your client sees the completion against the number of wasted requests all saying "is it done yet?"

In the case where you need realtime notification of completion, you should use a server-initiated notification system. These days, that pretty much always means WebSockets (or SignalR), although old-school solutions like long polling or even server-side events (SSE) are still around, too. All of these solutions enable an HTTP application to push a message to an already-connected client. In this case, the common approach is to have the backend processing service connect to the HTTP application via some kind of bus (e.g., it could connect to a SignalR hub), and then send a message over that bus directly to the HTTP application, which notifies its clients that the processing is complete.

This is a fine approach, and only has one caveat: when a system's architecture becomes complex enough to introduce asynchronous messaging, then that system is also positioned to be prepared to scale out. The bus connecting the HTTP application instances to the background processing instances needs to be ready for scaling. Some systems (e.g., SignalR) are not set up to scale by default. If I was adding asynchronous messaging to a system and we needed to use SignalR for realtime notifications, then I would set it up with a scalable backplane immediately.