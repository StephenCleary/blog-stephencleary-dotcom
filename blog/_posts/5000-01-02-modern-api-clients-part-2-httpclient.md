---
layout: post
title: "Modern API Clients, Part 2: HttpClient"
series: "Modern API Clients"
seriesTitle: "HttpClient"
description: "HttpClient and its design."
---

## Use HttpClient

If your codebase still uses `WebClient` or `HttpWebRequest`, it's time to upgrade. It is *far* easier to build a great client API using `HttpClient` than either of those outdated choices. For one thing, `HttpClient` was designed to be `async`-friendly from the start, rather than tacking it on later. Another important aspect of `HttpClient` is its pipeline architecture, which we'll be using quite a bit in this series.

## HttpClient's Pipeline

