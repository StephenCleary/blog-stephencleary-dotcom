---
layout: post
title: "Async Unit Tests, Part 2: The Right Way"
---
<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

**Update:** The information in this blog post _only applies to Visual Studio 2010_. Visual Studio 2012 _will_ support asynchronous unit tests, **as long as those tests are "async Task" tests, not "async void" tests**.
</div>

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Update (2014-12-01): For a more modern solution, see Chapter 6 in my [Concurrency Cookbook]({{ '/book/' | prepend: site.url_www }}){:.alert-link}.
</div>

Last time, we looked at [incorrect approaches to async unit testing]({% post_url 2012-02-06-async-unit-tests-part-1-wrong-way %}). We also identified the underlying problem: that unit tests do not have an appropriate async context.

At this point, the solution should be pretty obvious: give the unit tests an async context!

It really is that easy! Why, all you have to do is write your own SynchronizationContext implementation. Keep in mind that thread-safety is paramount, because the methods under test may interact with the thread pool or other async contexts. Note that [async void methods interact with SynchronizationContext in a different way](http://msdn.microsoft.com/en-us/magazine/gg598924.aspx) than other async methods. Oh, and also remember that exceptions need special handling in some cases so their original call stack is preserved appropriately, and if you're on VS2010 you'll need to hack this in because [there's no support for it on .NET 4.0](http://connect.microsoft.com/VisualStudio/feedback/details/633822/allow-preserving-stack-traces-when-rethrowing-exceptions).

Just kidding! Ha, ha! The good folks on the Async team have done all the hard work for you. :)

## Right Way #1: The Official Approach

If you have the Async CTP installed, then check out the "My Documents\Microsoft Visual Studio Async CTP\Samples\(C# Testing) Unit Testing\AsyncTestUtilities" folder. You'll find not just one, but _three_ async-compatible contexts, ready for you to use!

You should use GeneralThreadAffineContext unless you absolutely need another one. To use it, just copy AsyncTestUtilities.cs, CaptureAndRestorer.cs, and GeneralThreadAffineContext.cs into your test project.

Then, take each unit test and re-write it so that it has a context:

{% highlight csharp %}
[TestMethod]
public void FourDividedByTwoIsTwo()
{
    GeneralThreadAffineContext.Run(async () =>
    {
        int result = await MyClass.Divide(4, 2);
        Assert.AreEqual(2, result);
    });
}
    
[TestMethod]
[ExpectedException(typeof(DivideByZeroException))]
public void DenominatorIsZeroThrowsDivideByZero()
{
    GeneralThreadAffineContext.Run(async () =>
    {
        await MyClass.Divide(4, 0);
    });
}
{% endhighlight %}

Our unit test methods are not async. Each one sets up an async context and passes the _actual_ test into it as an async lambda expression. So, the _actual_ test code can still be written with all the benefits of async/await, and the async context takes care of making sure it runs as expected:

![]({{ site_url }}/assets/AsyncUnitTests8.png)  

Just as importantly, the async context ensures that tests that _should_ fail, _will_ fail:

{% highlight csharp %}
[TestMethod]
public void FourDividedByTwoIsThirteen_ShouldFail()
{
    GeneralThreadAffineContext.Run(async () =>
    {
        int result = await MyClass.Divide(4, 2);
        Assert.AreEqual(13, result);
    });
}
{% endhighlight %}

![]({{ site_url }}/assets/AsyncUnitTests9.png)  

And everyone lived happily ever after!

Well, sort of. This solution does work, but it's a bit cumbersome. Copying code files into each test project? Modifying _every_ unit test to set up its own async context? _Really?_

## Right Way #2: Now with Less Effort!

Boy, if only there was _some way_ to have the MSTest framework apply the async context _for_ us, then we could just write async unit test methods and not worry about it!

Oh yeah - there is. Visual Studio allows you to define a custom "test type." It really is that easy! Why, all you have to do is... ah, forget it. A custom "async unit test" type is already available:

- Install the [AsyncUnitTests-MSTest NuGet package](http://nuget.org/packages/AsyncUnitTests-MSTest) into your test project.
- Add a **using Nito.AsyncEx.UnitTests;** line.
- Change your [TestClass] attribute to [AsyncTestClass].

Sweet.

Now you can write async unit tests (using async void):

{% highlight csharp %}
[TestMethod]
public async void FourDividedByTwoIsTwoAsync()
{
    int result = await MyClass.Divide(4, 2);
    Assert.AreEqual(2, result);
}
    
[TestMethod]
[ExpectedException(typeof(DivideByZeroException))]
public async void DenominatorIsZeroThrowsDivideByZeroAsync()
{
    await MyClass.Divide(4, 0);
}
{% endhighlight %}

And it works:

![]({{ site_url }}/assets/AsyncUnitTests10.png)  

And test failures actually fail:

{% highlight csharp %}
[TestMethod]
public async void FourDividedByTwoIsThirteenAsync_ShouldFail()
{
    int result = await MyClass.Divide(4, 2);
    Assert.AreEqual(13, result);
}
{% endhighlight %}

![]({{ site_url }}/assets/AsyncUnitTests11.png)  

_Sniff..._ It's... so... beautiful...

But not quite perfect. You still have to add a NuGet package _and_ remember to change [TestClass] to [AsyncTestClass].

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Tip: You can download an [Async Unit Test item type](http://asyncunittests.codeplex.com/wikipage?title=Optional%20Component){:.alert-text} which uses [AsyncTestClass] instead of [TestClass]. This makes writing new async tests just a little bit easier, but not entirely foolproof.
</div>

## Future Directions

xUnit.NET has recently released [first-class support for asynchronous unit tests](http://xunit.codeplex.com/workitem/9733): in version 1.9 (2012-01-02) and newer, for any test method returning Task/Task\<T>, the test framework will wait until the task completes before declaring success/failure. However, as of now, it does not support async void unit tests; this [is planned](http://xunit.codeplex.com/workitem/9752) for a future release.

I've been in contact with some people inside of Microsoft regarding this issue, and they said they're aware of it and are considering various options. They wouldn't give me any details, of course, but they did suggest that I would be "pleasantly surprised" when Visual Studio vNext comes out.

So, that's where we are today. Hopefully Microsoft will ship built-in async unit test support in Visual Studio vNext, and I'll be able to look back at this blog post and laugh at how fraught with peril async unit testing _used_ to be.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Update (2014-12-01): For a more modern solution, see Chapter 6 in my [Concurrency Cookbook]({{ '/book/' | prepend: site.url_www }}){:.alert-link}.
</div>

