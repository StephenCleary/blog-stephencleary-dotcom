---
layout: post
title: "Task.Run Etiquette and Proper Usage"
---
I have a confession to make: I enjoy etiquette. My wife and I own a good dozen or two etiquette books, ranging from the classic [Post](http://www.amazon.com/gp/product/0061740233/ref=as_li_ss_tl?ie=UTF8&camp=1789&creative=390957&creativeASIN=0061740233&linkCode=as2&tag=stepheclearys-20) to a [more playful Austen-themed guide](http://www.amazon.com/gp/product/159691274X/ref=as_li_ss_tl?ie=UTF8&camp=1789&creative=390957&creativeASIN=159691274X&linkCode=as2&tag=stepheclearys-20) as well as several historical books (including an uncut copy of [Social Life of Virginia in the Seventeenth Century](http://www.amazon.com/gp/product/1163408735/ref=as_li_ss_tl?ie=UTF8&camp=1789&creative=390957&creativeASIN=1163408735&linkCode=as2&tag=stepheclearys-20)). There's a certain nerdy appreciation of knowing how to act (at least, according to _somebody..._).

Many developers are unsure how to properly use `Task.Run`. This confusion is perfectly normal; when you first start using `async`, it's kind of like sitting down at a formal dinner and seeing three forks when you've spent your entire life only using spoons. "When should I use the `Task.Run` fork? Is this for salad or dessert???"

The key central theme of all etiquette is to _treat others well_. In this case, the "others" are other developers. "Others" will come along and maintain your code; "others" will try to reuse your code in different contexts. As we'll see, treating those "others" well is the key to properly using `Task.Run`.

First, let's think of what `Task.Run` is really _for_ (you wouldn't want to use a fork to eat soup!). **The purpose of `Task.Run` is to execute CPU-bound code in an asynchronous way.** `Task.Run` does this by executing the method on a thread pool thread and returning a `Task` representing the completion of that method.

That sounds so simple, but we've already eliminated a whole slew of poor examples. Many `async` newbies start off by trying to treat asynchronous tasks the same as parallel (TPL) tasks, and this is a major misstep. True, asynchronous tasks and parallel tasks are the same type (`Task`, of course), but their _purpose_ is completely different and thus their _proper usage_ is also completely different. Developers new to `async` begin reading some examples, see these newfangled `async` methods returning tasks, and (incorrectly) assume that `async` is all about background threads and whatnot. With that initial misunderstanding, the next logical step is to attempt to implement all asynchronous methods using `Task.Run` (or [even worse]({% post_url 2013-08-29-startnew-is-dangerous %}), `Task.Factory.StartNew`).

I've seen many, many intelligent developers fall into that same mistake.

> -> Want a more detailed discussion? Read [Don't Use Task.Run for the Wrong Thing]({% post_url 2013-11-06-taskrun-etiquette-examples-using %}).

So, the question remains: Where _should_ I use `Task.Run`?

**Use `Task.Run` to call CPU-bound methods.** That is all.

One common mistake is to try to make asynchronous "wrappers" around existing synchronous methods. Stephen Toub has a detailed blog post describing [why this is a bad idea](http://blogs.msdn.com/b/pfxteam/archive/2012/03/24/10287244.aspx). I call such methods "fake-asynchronous methods" because they _look_ asynchronous but are really just faking it by doing synchronous work on a background thread. In general, **do not use `Task.Run` in the implementation of the method; instead, use `Task.Run` to _call_ the method.** There are two reasons for this guideline:

1. Consumers of your code assume that if a method has an asynchronous signature, then it will act truly asynchronously. Faking asynchronicity by just doing synchronous work on a background thread is surprising behavior.
1. If your code is ever used on ASP.NET, a fake-asynchronous method leads developers down the wrong path. The goal of `async` on the server side is scalability, and fake-asynchronous methods are _less_ scalable than just using synchronous methods.

So, any code that you want to be reusable should not use `Task.Run` in its implementation. Consider the developers (including yourself) who will need to consume that code.

> -> Want a more detailed discussion? Read [Don't Use Task.Run in the Implementation]({% post_url 2013-11-07-taskrun-etiquette-examples-dont-use %}).

OK, so let's complicate the question a bit. What if I have a reusable method that uses significant amounts of both I/O _and_ CPU? Should I use `Task.Run` for the CPU-bound parts?

The answer is still no.

However, in this (uncommon) situation, you do end up with a bit of an awkward solution: an asynchronous method that also does CPU-bound work. In this case, you should document clearly that the method is not fully asynchronous, so that callers know to wrap it in a `Task.Run` if necessary. (Remember, it's necessary if it's being called from the UI thread, but not necessary if called from a background thread or ASP.NET).

> -> Want a more detailed discussion? Read [Even in the Complex Case, Don't Use Task.Run in the Implementation]({% post_url 2013-11-08-taskrun-etiquette-examples-even-in %}).

To conclude, synchronous methods should have a synchronous signature:

{% highlight csharp %}
// Documentation: This method is CPU-bound (use Task.Run to call from a UI thread).
void DoWork();
{% endhighlight %}

Asynchronous methods should have an asynchronous signature:

{% highlight csharp %}
Task DoWorkAsync();
{% endhighlight %}

And methods that are a _mixture_ of synchronous and asynchronous work should have an asynchronous signature with documentation pointing out their partially-synchronous nature:

{% highlight csharp %}
// Documentation: This method is CPU-bound (use Task.Run to call from a UI thread).
Task DoWorkAsync();
{% endhighlight %}