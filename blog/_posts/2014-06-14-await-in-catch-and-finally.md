---
layout: post
title: "Await in Catch and Finally"
description: "Visual Studio 14 will probably support await in catch and finally blocks."
---

This is just a brief note to publicize a coming improvement to the `async` language support.

Visual Studio "14" [is currently in CTP](https://docs.microsoft.com/en-us/archive/blogs/somasegar/visual-studio-14-ctp?WT.mc_id=DT-MVP-5000058), and is [available for download](http://www.visualstudio.com/en-us/downloads/visual-studio-14-ctp-vs). One of the primary advantages of this release is the new Roslyn-based compilers.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Note that the "14" in the name is the version number, not the year of release. In other words, the CTP is for "Visual Studio 14", _not_ "Visual Studio 2014". If I had to guess, I would say that this CTP will probably become "Visual Studio 2015".
</div>

With the new compilers, changes to the C# language (e.g., `async`/`await`) are easier than they used to be. One improvement that is coming is the use of `await` in `catch` and `finally` blocks. This enables your error-handling/cleanup code to be asynchronous without awkward code mangling.

For example, let's say that you want to (asynchronously) log an exception in one of your `async` methods. The natural way to write this is:

{% highlight csharp %}
try
{
  await OperationThatMayThrowAsync();
}
catch (ExpectedException ex)
{
  await MyLogger.LogAsync(ex);
}
{% endhighlight %}

And this natural code works fine in Visual Studio "14". However, the currently-released Visual Studio 2013 does not support `await` in a `catch`, so you would have to keep some kind of "error flag" and move the actual error handling logic outside the `catch` block:

{% highlight csharp %}
ExpectedException exception = null;
try
{
  await OperationThatMayThrowAsync();
}
catch (ExpectedException ex)
{
  exception = ex;
}
if (exception != null)
  await MyLogger.LogAsync(exception);
{% endhighlight %}

This is only a simple example; in real-world code, this can get ugly rather quickly!

Fortunately, it looks like the next version of Visual Studio will fix this by allowing `await` within `catch` and `finally` blocks. I've tested this out with the Visual Studio "14" CTP, and it does work!

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

This blog post is describing technology currently in preview (CTP). The final product may be different.
</div>
