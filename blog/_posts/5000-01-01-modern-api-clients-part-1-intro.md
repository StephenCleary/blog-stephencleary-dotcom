---
layout: post
title: "Modern API Clients, Part 1: Introduction"
series: "Modern API Clients"
seriesTitle: "Introduction"
description: "A discussion of API clients, and why it's often best to write your own."
---

This post kicks off a new series on writing modern API clients in C#. I'm excited about this series, because there's a *lot* of information out there on all the different pieces, but AFAIK there's no complete guide that puts everything together.

Over the next few weeks, we'll look at how to create a modern API client, using `HttpClient` (via `HttpClientFactory`), `Polly`, and `Refit` to provide a first-class C# client for any kind of API service.

## Why?

C# is a pretty major language, so a lot of APIs provide their own C# client (along with clients for other common languages). One would think that those teams know their own API well, so shouldn't everyone just use the C# clients provided by those teams instead of going through all the trouble of creating one?

Well, yes and no. Yes, those teams know their own API well. No, their own C# client is probably not ideal.

A lot of services on the Internet are API-first. So their API is their primary export; the API is documented well, including all edge cases. In many cases, their API is their actual product - or at least a critical feature of their product. When these teams provide language-specific clients, there are some common problems that result.

### Documentation

For an API-first service, the API is documented well. Client libraries using that API? Not so much. With many client libraries, you have to spend enough time with it to know its "flavor" of talking to the API, and then use the actual API documentation to figure out how to use the client. Sometimes the C# client libraries have barely any documentation at all; other times the client library documentation is auto-generated from the API documentation, and will occasionally not make sense since it is written from an HTTP perspective rather than a C# perspective.

The bottom line is that client libraries are almost never as well-documented as the actual API.

### Feature Lag

There's almost always a feature lag between the API and its client libraries. The API is the first-class citizen; it is the important thing; and a feature is considered "shipped" when it hits the API. The client libraries (sometimes maintained by different teams) try to keep up, with varying degrees of success.

Some teams try to use tools like Swagger to auto-generate client libraries and thus avoid the feature lag; however, the resulting client library suffers from an impedance mismatch (see next section) as well as documentation problems (see previous section) since the auto-generated docs are written for the HTTP API rather than a C# API.

### Impedance Mismatch

This kind of problem is common in auto-generated client libraries, or libraries that strongly follow a pattern set by the HTTP API. The problem is that HTTP is one kind of API, and C# class libraries are another kind of API; when using a C# client with an impedance mismatch, the code just doesn't feel "right". It doesn't have the same structure and naming and typing that a C# developer would use if writing a general-purpose library; it has the structure and naming and typing of an HTTP API. That kind of impedance mismatch just makes the API more awkward to use.

Although this is a common problem with auto-generated clients, it can occur with manually-written clients as well. Many times, a single team is responsible for maintaining clients written in several widely different languages, and it's almost certain that they're not familiar with *all* of those languages/runtimes. So you'll sometimes see C# clients written in a style that just seems very foreign to anyone who is familiar with that language.

### Bugs

Client libraries tend to have bugs. Many times, there are not fully exhaustive tests for the client libraries. This makes sense - otherwise, the team would have to maintain multiple exhaustive test suites across a wide variety of languages and runtimes, which is impractical. So, C# API client libraries tend to have more bugs than other C# libraries. Sometimes it's something as simple as a missing property for some object passed to the API; other times it's more serious like structuring the C# API so it's impossible to handle all results from the HTTP API. (I have seen both of these in real-world C# APIs).

### Variation in Error Handling

There's a wide variety of ways to handle errors. Of course, exceptions are the standard form in C#. But there are some situations in an API call that are not clearly *errors*. `404 Not Found` is a common one - does the C# client treat that as an *error* (exception)? Or would it treat it as a special return value?

Additionally, C# client libraries often do not handle their exceptions well. Most libraries are good about consistently throwing a specific exception type, but it can be difficult (or impossible) to retrieve the actual HTTP status code from the exception. And few C# client libraries allow reading and paring a detailed error description from the body of the HTTP response; most C# clients are content with throwing the exception and the details are lost.

### Change in Behavior

Over time, teams may deliberately change the behavior of their C# clients. When this happens, your upgrades can become tedious or even dangerous.

Some teams deliberately move *towards* auto-generated clients, wanting the benefits of feature parity with the API and removing bugs. Other teams deliberately move *away from* auto-generated clients, wanting the benefits of native-friendly documentation and "feel" of the API. And some teams have moved both ways, at different times. Neither solution is perfect; there are tradeoffs both ways. But as the teams deliberately move in one direction or the other, their client libraries will change.

When C# client libraries change, this results in additional work for all of their consumers. As a consumer, you now have to update to the latest C# client version and possibly learning a new "flavor" for the library. And all of this work is tedious because there is no net benefit to your customers - you have to do the work (to stay up-to-date), but the end result is that your code calls the same APIs in the same way as is was doing before. It's work just to keep up with the client; it adds no value to your application.

Updates can also be dangerous. For example, if a C# client library chooses to change how it handles errors. This actually happened to me. One version of a C# client library returned an HTTP response kind of object from every API call, but it would throw an exception instead of returning the response if the status code indicated an error. The next (major) version of that same C# client library also returned an HTTP response kind of object from every API call, but the exceptions were no longer thrown. Instead, the caller was expected to check the status code and throw if necessary.

Both of these approaches to error handling are valid, but I was not aware that the new client library version no longer threw exceptions. Unfortunately, I didn't have integration tests covering this specific call, and a number of `4xx` responses in production actually caused loss of data. That was a fun week.

### Scenario Support and Customization (Cancellation, Retries, Logging, Authentication)

These days pretty much everyone supports `async`, so I think that's a given for C# client libraries. But there are many other scenarios that C# clients could provide support for.

Cancellation is a pretty good one. Some C# client libraries have exhaustive support for `CancellationToken`s throughout; others do not. This is usually a "yes or no" kind of thing; either the whole library supports cancellation well, or the whole library is missing cancellation support.

Support for retries is much more variable. Some kind of retry is an absolute must. Does the C# client library have support for automatic retries? If so, can they be configured or disabled?

Logging support is also all over the map. Some C# client libraries simply have no logging support at all; others have their own scheme. Very few require a specific logging provider. If the C# client library supports logging, how customizable is it? Can it be turned off?

Authentication is another issue. It's pretty easy if the API is authenticated with a key - usually there's one place to set the key (e.g., in the constructor of the main client type). But if the API requires an authorization token which can expire (or be rotated) and requires renewal, does the C# client library handle all these details? With retries?

It can take a long time to determine the actual behavior of any given C# client library.

### Abandonware

Of course, one reason not to use C# client libraries is that they may be abandonware. Or may *become* abandonware in the future. Many teams tire of walking the tightrope between auto-generated code that "feels weird" and hand-maintained code in a language they may not be familiar with. Abandonware is certainly a possibility over time.

Or the C# client library may just not exist at all. Some companies do not see C# as a common enough language to warrant an ongoing investment.

## Anti-Flow

To me, the biggest problem when using C# client libraries is anti-flow. When you're in the flow, you can code quickly. C# client libraries - just due to their nature - disrupt that flow.

Each client API can have its own "flavor", i.e., how the DTOs are shaped, whether it works with more general HTTP request/responses or has a more custom C# API, and how status codes are handled. Each client not only has its own "flavor", but also different semantics for retries and logging. The problem is not just that the flavor is different than the application's preferred flavor; the problem is really that the flavor is different across *all* C# client libraries for all the different APIs your application needs. So you have to constantly be switching contexts in your brain, and this acts as anti-flow.

It takes time to re-acquire the "taste" of each library. Every C# client library has a slightly different set of choices across each of these aspects. And even for the same API, the next version of the client may change one or more of those choices. There's just too much to keep track of, and this results in anti-flow.

## Solution

The solution is simple. But also hard. :)

Here it is: For API-first services, code to the API.

Over the last few years, I've been moving away from using C# client libraries at all, for all the reasons listed above. And I'm not the only one! Many developers are creating their own API clients rather than using the provided C# libraries.

This series is mainly a "lessons learned" along with some "useful tips and tools". My goal with this series is to empower other developers to write their own API clients. The big benefit you get from writing your own API clients for all the APIs you use is that *they all behave the same*. Flow is preserved rather than disrupted.

### Perspective

Before we dive in, I'd like to suggest a word on perspective. As you develop your own API client, your goal is to write the client that *your* application needs to use *that* API. Your goal is not to write a client for that API; your goal is to write a client for your application - in other words, don't put in features the API has that your application is not going to use. Similarly, your goal is not to write a client for all APIs; for each API, tailor the client to that specific API.

I do believe that this is the future of how HTTP APIs are called in C#: each application defines its own clients for the APIs it needs, and each client has a very focused scope: it only talks to one API on behalf of one application.
