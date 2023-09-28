---
layout: post
title: "Async Unit Tests, Part 1: The Wrong Way"
---
<blockquote>Code without tests does not exist.<footer>(Overheard at <a href="http://codemash.org/">CodeMash</a>)</footer></blockquote>

The core meaning of this quote is that code without unit tests is not as useful as code with unit tests. The speaker even goes so far as to say he _won't_ use code without tests.

I don't take a position quite this extreme, but I definitely agree with the underlying sentiment: that code with unit tests is far more useful. Unit tests prove correct functionality (at least for a limited set of cases). Unit tests also provide a sort of documentation.

If you don't write unit tests - or if you or your manager think writing tests just delays software development - then I refer you to the best computer book ever written, [Code Complete](http://www.amazon.com/gp/product/0735619670/ref=as_li_ss_tl?ie=UTF8&tag=stepheclearys-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=0735619670){:rel="nofollow"}. In that book, Steve McConnell presents some very interesting hard facts about testing.

I hope we can all agree that unit testing is a fundamental skill in Modern Programming. And this brings me to a sad chapter in async/await support: the "obvious" way to do unit tests is wrong.

Let's start with a simple asynchronous method. So simple, in fact, that it will just pretend to work for a while and then do a single integer division:

    public static class MyClass
    {
      public static async Task<int> Divide(int numerator, int denominator)
      {
        // Work for a while...
        await Task.Delay(10); // (Use TaskEx.Delay on VS2010)
    
        // Return the result
        return numerator / denominator;
      }
    }

Boy, it doesn't seem that there _can_ be much wrong with that code! But as we'll see, there's a lot that can be wrong with the unit tests...

When developers write unit tests for async code, they usually take one of two mistaken approaches (with the second one being what I call "obvious"). We'll look at each of these approaches in this post and examine why they're wrong, and look at solutions next time.

## Wrong Way #1: Using Task.Wait and Task.Result

This mistake is most common for people new to async: they decide to wait for the task to complete and then check its result. Well, that _seems_ logical enough, and some unit tests written this way actually work:

    [TestMethod]
    public void FourDividedByTwoIsTwo()
    {
      Task<int> task = MyClass.Divide(4, 2);
      task.Wait();
      Assert.AreEqual(2, task.Result);
    }

![]({{ site_url }}/assets/AsyncUnitTests1.png)  

But one of the problems with this approach is unit tests that check error handling:

    [TestMethod]
    [ExpectedException(typeof(DivideByZeroException))]
    public void DenominatorIsZeroThrowsDivideByZero()
    {
      Task<int> task = MyClass.Divide(4, 0);
      task.Wait();
    }

![]({{ site_url }}/assets/AsyncUnitTests2.png)  

This unit test is failing, even though the async method under test _is_ throwing a DivideByZeroException. The Test Results Details explains why:

![]({{ site_url }}/assets/AsyncUnitTests3.png)  

The Task class is wrapping our exception into an AggregateException. This is why the Task.Wait and Task.Result members should not be used with new async code (see the end of [last week's async intro post]({% post_url 2012-02-02-async-and-await %})).

Well, we could await the task, which would unwrap the exception for us. This would require our test method to be async. Congratulations, you can move on to the next section.

## Wrong Way #2: Using Async Test Methods

This mistake is more common for people who have used async in some real-world code. They've observed how async "grows" through the code base, and so it's natural to extend async to the test methods. This is what I consider the "obvious" solution:

    [TestMethod]
    public async Task FourDividedByTwoIsTwoAsync()
    {
      int result = await MyClass.Divide(4, 2);
      Assert.AreEqual(2, result);
    }

![]({{ site_url }}/assets/AsyncUnitTests4.png)  

Yay! It works!

...

Wait...

    [TestMethod]
    public async Task FourDividedByTwoIsThirteenAsync()
    {
      int result = await MyClass.Divide(4, 2);
      Assert.AreEqual(13, result);
    }

![]({{ site_url }}/assets/AsyncUnitTests5.png)  

Um, that test should _certainly not_ be passing! What is going on here???

We've encountered a situation very similar to [async in Console programs]({% post_url 2012-02-03-async-console-programs %}): there is no async context provided for unit tests, so they're just using the thread pool context. This means that when we await our method under test, then our async test method returns to its caller (the unit test framework), and the remainder of the async test method - including the Assert - is scheduled to run on the thread pool. When the unit test framework sees the test method return (without an exception), then it marks the method as "Passed". Eventually, the Assert will fail on the thread pool.

There is now a race condition. There's no race condition in the test itself; it will always pass (incorrectly). The race condition is when the assertion fires. If the assertion fires _after_ the unit test framework finishes the test run, then you'll see a successful test run (like the last screenshot). But if the assertion fires _before_ the unit test framework finishes the test run, then you'll see something like this:

![]({{ site_url }}/assets/AsyncUnitTests6.png)  

Clicking on the link shows that the Assertion is indeed failing on the thread pool, some time after the test is considered completed and "Passed":

![]({{ site_url }}/assets/AsyncUnitTests7.png)  

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

**Update:** Visual Studio 2012 will correctly support "async _Task_" unit tests, but doesn't support "async void" unit tests.
</div>

## Next Time: The Right Way

During CodeMash, I gave a lightning talk about async unit testing. You could almost hear the teeth grinding at this point, when the TDD/BDD fans discovered that async unit tests were essentially broken. But do not give up hope!

Tomorrow we'll look at [the right way to do async unit testing]({% post_url 2012-02-07-async-unit-tests-part-2-right-way %}).

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Update (2014-12-01): For a more modern solution, see Chapter 6 in my [Concurrency Cookbook]({{ '/book/' | prepend: site.url_www }}){:.alert-link}.
</div>

