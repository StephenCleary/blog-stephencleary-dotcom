---
layout: post
title: "Cancellation, Part 5: Linking"
series: "Cancellation"
seriesTitle: "Linking"
description: "Providing complemetary cancellation by linking cancellation tokens."
---

So far we've covered how [cancellation is requested by one piece of code, and responded to by another piece of code]({% post_url 2022-02-24-cancellation-1-overview %}). The requesting code has a [standard way of requesting cancellation]({% post_url 2022-03-03-cancellation-2-requesting-cancellation %}), as well as a standard way of [detecting whether the code was canceled or not]({% post_url 2022-03-10-cancellation-3-detecting-cancellation %}). Meanwhile, the responding code can observe cancellation either by [polling]({% post_url 2022-03-17-cancellation-4-polling %}) or (more commonly) by [registering a cancellation callback]({% post_url 2024-08-08-cancellation-5-registration %}). So far, so good; and we're ready for the next step!

In this article, we'll look at how _linked_ cancellation tokens work. 

## Linked Cancellation Tokens



## Summary

