---
layout: post
title: "The Async CTP \"Why Do the Keywords Work THAT Way\" Unofficial FAQ"
---
There's a lot of interest in the [Async CTP](http://msdn.microsoft.com/en-US/vstudio/async?WT.mc_id=DT-MVP-5000058), with good reason. The Async CTP will make asynchronous programming much, much easier than it has ever been. It's somewhat less powerful but much easier to learn than [Rx](http://msdn.microsoft.com/en-us/data/gg577609?WT.mc_id=DT-MVP-5000058).

The Async CTP introduces two new keywords, **async** and **await**. Asynchronous methods (or lambda expressions) must return **void**, **Task**, or **Task\<TResult>**.

This post is not an introduction to the Async CTP; there's plenty of tutorial resources available out there (including [one by yours truly]({% post_url 2012-02-02-async-and-await %})). This post is an attempt to bring together the answers to a few common questions that programmers have when they start using the Async CTP.

## Inferring the Return Type

When returning a value from an **async** method, the method body returns the value directly, but the method itself is declared as returning a **Task\<TResult>**. There is a bit of "disconnect" when you declare a method returning one type and have to return another type:

{% highlight csharp %}

// Actual syntax
public async Task<int> GetValue()
{
  await TaskEx.Delay(100);
  return 13; // Return type is "int", not "Task<int>"
}
{% endhighlight %}

Question: Why can't I write this:

{% highlight csharp %}

// Hypothetical syntax
public async int GetValue()
{
  await TaskEx.Delay(100);
  return 13; // Return type is "int"
}
{% endhighlight %}

Consider: How will the method signature look to callers? Async methods that return a value must have an actual result type of **Task\<TResult>**. So **GetValue** will show up in IntelliSense as returning **Task\<TResult>** (this would also be true for the object browser, Reflector, etc).

Inferring the return type [was considered](http://social.msdn.microsoft.com/Forums/en-US/async/thread/0ee0af6a-3034-4ac3-aa82-cb6bd62a9ab9#8d1826a5-d603-4b74-8c64-2a9b32d6af24?WT.mc_id=DT-MVP-5000058) during the initial design, but the team concluded that the keeping the "disconnect" within the **async** method was better than spreading the "disconnect" throughout the code base. The "disconnect" is still there, but it's smaller than it could be. The consensus is that a consistent method signature is preferred.

Consider: There is a difference between **async void** and **async Task**.

An **async Task** method is just like any other asynchronous operation, only without a return value. An **async void** method acts as a "top-level" asynchronous operation. An **async Task** method may be composed into other async methods using **await**. An **async void** method may be used as an event handler. An **async void** method also has another important property: in an ASP.NET context, it informs the web server that the page is not completed until it returns (see [my MSDN article](http://msdn.microsoft.com/en-us/magazine/gg598924.aspx?WT.mc_id=DT-MVP-5000058) for more information on how this works).

Inferring the return type would remove the distinction between **async void** and **async Task**; either all async methods would be **async void** (preventing composability), or they would all be **async Task** (preventing them from being event handlers, and requiring an alternative solution for ASP.NET support).

## Async Return

There is still a "disconnect" between the method declaration return type and the method body return type. Another option that [has been suggested](http://gauravsmathur.wordpress.com/2010/11/04/something-wrong-with-async-await-and-the-tasktask/) is to add a keyword to **return** to indicate that the value given to **return** is not really what's being returned, e.g.:

{% highlight csharp %}

// Hypothetical syntax
public async Task<int> GetValue()
{
  await TaskEx.Delay(100);
  async return 13; // "async return" means the value will be wrapped in a Task
}
{% endhighlight %}

Consider: Converting large amounts of code from synchronous to asynchronous.

The **async return** keyword [was also considered](http://social.msdn.microsoft.com/Forums/en-US/async/thread/75493675-4a39-4958-a493-ad8a96f8a19d?WT.mc_id=DT-MVP-5000058), but it wasn't compelling enough. This is particularly true when converting a lot of synchronous code to asynchronous code (which will be common over the next few years); forcing people to add **async** to every **return** statement just seemed like "needless busy-work." It's easier to get used to the "disconnect".

## Inferring "async"

The **async** keyword _must_ be applied to a method that makes use of **await**. However, it also gives a warning if it is applied to a method that does _not_ make use of **await**.

Question: Why can't **async** be inferred based on the presence of **await**:

{% highlight csharp %}

// Hypothetical syntax
public Task<int> GetValue()
{
  // The presence of "await" implies that this is an "async" method.
  await TaskEx.Delay(100);
  return 13;
}
{% endhighlight %}

Consider: Backwards compatibility and code readability.

Eric Lippert has the [definitive post](https://docs.microsoft.com/en-us/archive/blogs/ericlippert/asynchrony-in-c-5-part-six-whither-async?WT.mc_id=DT-MVP-5000058) on the subject. It's also been discussed in [blog comments]https://docs.microsoft.com/en-us/archive/blogs/ericlippert/asynchronous-programming-in-c-5-0-part-two-whence-await?WT.mc_id=DT-MVP-5000058), [Channel9](http://channel9.msdn.com/Forums/Coffeehouse/Why-is-the-async-keyword-needed?WT.mc_id=DT-MVP-5000058), [forums](http://social.msdn.microsoft.com/Forums/en-US/async/thread/75493675-4a39-4958-a493-ad8a96f8a19d?WT.mc_id=DT-MVP-5000058), and [Stack Overflow](http://stackoverflow.com/questions/9225748/why-does-the-async-keyword-exist).

To summarize, a single-word **await** keyword would be too big of a breaking change. The choice was between a multi-word await (e.g., **await for**) or a keyword on the method (**async**) that would enable the **await** keyword just within that method. Explicitly marking methods **async** is easier for both humans and computers to parse, so they decided to go with the **async**/**await** pair.

## Inferring "await"

Question: Since it makes sense to explicitly include **async** (see above), why can't **await** be inferred based on the presence of **async**:

{% highlight csharp %}

// Hypothetical syntax
public async Task<int> GetValue()
{
  // "await" is implied, since this is an "async" method.
  TaskEx.Delay(100);
  return 13;
}
{% endhighlight %}

Consider: Parallel composition of asynchronous operations.

At first glance, inferring **await** appears to simplify basic asynchronous operations. As long as all waiting is done in serial (i.e., one operation is awaited, then another, and then another), this works fine. However, it falls apart when one considers parallel composition.

Parallel composition in the Async CTP is done using **TaskEx.WhenAny** and **TaskEx.WhenAll** methods. Here's a simple example which starts two operations immediately and asynchronously waits for both of them to complete:

{% highlight csharp %}

// Actual syntax
public async Task<int> GetValue()
{
  // Asynchronously retrieve two partial values.
  // Note that these are *not* awaited at this time.
  Task<int> part1 = GetValuePart1();
  Task<int> part2 = GetValuePart2();

  // Wait for both values to arrive.
  await TaskEx.WhenAll(part1, part2);

  // Calculate our result.
  int value1 = await part1; // Does not actually wait.
  int value2 = await part2; // Does not actually wait.
  return value1 + value2;
}
{% endhighlight %}

In order to do parallel composition, we must have the ability to say we're _not_ going to **await** an expression.

