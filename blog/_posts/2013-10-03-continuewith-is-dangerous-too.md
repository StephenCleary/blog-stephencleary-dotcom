---
layout: post
title: "ContinueWith is Dangerous, Too"
---
One of my [recent posts]({% post_url 2013-08-29-startnew-is-dangerous %}) dove into why `Task.Factory.StartNew` is so dangerous (and why you should use `Task.Run` instead).

Unfortunately, I see developers making the same mistake with `Task.ContinueWith`. One of the main problems of `StartNew` is that it has a confusing default scheduler. This exact same problem also exists in the `ContinueWith` API. Just like `StartNew`, `ContinueWith` will default to `TaskScheduler.Current`, not `TaskScheduler.Default`.

To avoid the default scheduler issue, **you should _always_ pass an explicit `TaskScheduler` to `Task.ContinueWith` and `Task.Factory.StartNew`**.

