---
layout: post
title: "Asynchronous Contexts in Rx"
---
Yesterday I (finally) wrote my first real-world code using Rx. Like many others, I've played around with various aspects of Rx, but until last night these were all just throwaway experiments. It turns out that in my very first real-world use of Rx, I had to implement an old concept: asynchronous contexts.

There have been some great resources released about Rx recently. Most notably, the [Rx hands-on lab](https://docs.microsoft.com/en-us/archive/blogs/rxteam/rx-hands-on-labs-published) ([direct link to PDF](http://download.microsoft.com/download/C/5/D/C5D669F9-01DF-4FAF-BBA9-29C096C462DB/Rx%20HOL%20.NET.pdf)), which is the closest thing to an Rx tutorial in existence. The Rx team followed up later this month with a two-part series on using Rx on the server: [asynchronous Stream](https://docs.microsoft.com/en-us/archive/blogs/jeffva/rx-on-the-server-part-1-of-n-asynchronous-system-io-stream-reading) and [asynchronous StreamReader](https://docs.microsoft.com/en-us/archive/blogs/jeffva/rx-on-the-server-part-2-of-n-asynchronous-streamreader). These blog posts are great examples of how to think when approaching a problem with Rx in hand.

This week, I had a business need to create a "search" form. The form is very simple: the user types something in a TextBox, and we populate a ListView with matching objects. It's sort of like a whole form devoted to AutoComplete. The actual "matching" function could be run asynchronously, so this problem ended up almost exactly like the dictionary lookup in the Rx hands-on lab document.

The one big difference is that the "matching" function will return its results incrementally (it's actually an IEnumerable\<T>), and I'd like to display the results incrementally as they are found. In contrast, the dictionary lookup in the Rx hands-on lab example returns all of its results at once.

Here's the first brush of the code:

{% highlight csharp %}

// Listen for the user typing.
var searchCommands = Observable.FromEvent<EventArgs>(this.textBoxSearch, "TextChanged")
  .Select(x => this.textBoxSearch.Text)
  .Throttle(TimeSpan.FromMilliseconds(200)) // For fast typists.
  .DistinctUntilChanged() // Only pass along the event if the actual text changed.
  .ObserveOn(this) // Marshal to UI thread.
  .Merge(Observable.Return(string.Empty)) // Start by searching an empty string.
  .Do(_ =>
  {
    // Update UI each time we get a new search request.
    this.listViewResults.Items.Clear();
    this.labelStatus.Text = "Searching...";
  });

// Define how we do searches.
Func<string, IObservable<T>> performSearch = searchString => this.matchProvider.Lookup(searchString)
  .ToObservable(Scheduler.ThreadPool) // Do the iteration on a ThreadPool thread.
  .ObserveOn(this); // Marshal to the UI thread.

// Each time a search is requested, cancel any existing searches and start the new one.
this.searchAction =
  searchCommands
  .Select(searchString =>
    performSearch(searchString) // Do the search.
    .Do(_ => { },
      () =>
      {
        // Update UI when the search is done.
        this.labelStatus.Text = "Done!";
      }))
  .Switch() // Cancel existing searches when a new search starts.
  .Subscribe(response => this.listViewResults.Items.Add(this.toListViewItem(response)));
{% endhighlight %}

The first chunk of the code is almost identical to the first chunk of the Rx hands-on lab code. The only difference is that I use ObserveOn(this) and Do() to clear out any previous search results when a new search _starts_ (the hands-on lab clears previous search results when a search _completes_). I also do a Merge() with an empty string, which causes all results to be returned as soon as the form is loaded.

The second chunk of code defines how searches are performed. The "matchProvider" object just returns an IEnumerable\<T> for a given search string. This enumerable is iterated on a ThreadPool thread, and the results are marshalled to the UI thread. This is similar to the asynchronous web service used by the Rx hands-on lab, except that it produces its results incrementally instead of all at once.

The third part of the code uses the Switch() operator to cancel old searches and start new ones as they are ready. A label is updated to notify the user when a search completes. All results from the combined searches are added to the ListView as they arrive. There is no need to marshal to the UI thread first, because both of the observable sources in this combination have already been marshalled to the UI thread.

## The Need for an Asynchronous Context

There's a rather subtle race condition in the code above. Observable sequences can get tricky whenever they change threads, and that is happening a couple of times here. The first one is not really obvious: Throttle() transfers control to a ThreadPool thread because of its timer. The other one _is_ obvious: we're converting an IEnumerable\<T> to an observable using Scheduler.ThreadPool. Both of these sequences do get marshalled back to the UI thread and combined using Switch(), and that's actually where the problem comes in.

According to [an authoritative post on the Rx forums](http://social.msdn.microsoft.com/Forums/en-US/rx/thread/19be939b-d257-4d8e-b104-4dfcc59b3ff8), when subscriptions are disposed they _may_ not stop immediately. At first this seems like a design flaw, but it actually makes perfect sense. Believe me - I've done enough asynchronous work to know how complicated it would be to have all subscription disposals stop their observables immediately.

In short, it's possible to have a former search complete (and update the UI displaying "Done!") after a newer search starts (and updates the UI displaying "Searching..."). The Rx hands-on lab does not have this problem because they only marshal to the UI thread (and display the results) when the lookup has completed.

Conceptually, this is the same problem that I discussed in [one of my first blog posts]({% post_url 2009-04-24-asynchronous-callback-contexts %}): an asynchronous operation can't always be reliably cancelled. In this case, the solution is to introduce an _asynchronous callback context_ and have the operation actively check its context before executing. If the callback is synchronized before checking the callback context, then it _knows_ whether or not it is cancelled (without causing another race condition).

To solve this problem in Rx, we'll use an asynchronous context (dropping the "callback" moniker, since it doesn't really apply). The concept is the same: asynchronous events copy the current value of the context (while they are synchronized), then go off and do whatever they do asynchronously, and finally check their saved context against the current value of the context (after they are re-synchronized).

Note that asynchronous contexts in Rx need to be attached to _each element_ in the observable. Logically, each observable element is an event.

## Using the Asynchronous Context

This code uses an asynchronous context. The simplest context is just an Object instance, which can be easily compared for equality and is guaranteed unique from any other context.

{% highlight csharp %}

// Our asynchronous context.
object context = null;

// Listen for the user typing.
var searchCommands = Observable.FromEvent<EventArgs>(this.textBoxSearch, "TextChanged")
  .Select(x => this.textBoxSearch.Text)
  .Throttle(TimeSpan.FromMilliseconds(200)) // For fast typists.
  .DistinctUntilChanged() // Only pass along the event if the actual text changed.
  .ObserveOn(this) // Marshal to UI thread.
  .Merge(Observable.Return(string.Empty)) // Start by searching an empty string.
  .Do(_ =>
  {
    // Change the context to prevent any future updates from old observables.
    context = new object();

    // Update UI each time we get a new search request.
    this.listViewResults.Items.Clear();
    this.labelStatus.Text = "Searching...";
  })
  .Select(searchString => new { context, searchString }); // Attach context to each search string.

// Define how we do searches.
Func<string, IObservable<T>> performSearch = searchString => this.matchProvider.Lookup(searchString)
  .ToObservable(Scheduler.ThreadPool) // Do the iteration on a ThreadPool thread.
  .ObserveOn(this); // Marshal to the UI thread.

// Each time a search is requested, cancel any existing searches and start the new one.
// Propogate the context to each search result.
this.searchAction =
  searchCommands
  .Select(request =>
    performSearch(request.searchString) // Start searching.
    .Select(result => new { request.context, result }) // Propogate the context to each search result.
    .Do(_ => { },
      () =>
      {
        // Check the context before handling the result.
        if (request.context == context)
        {
          // Update UI when the search is done.
          this.labelStatus.Text = "Done!";
        }
      }))
  .Switch()
  .Subscribe(
    response =>
    {
      // Check the context before handling the result.
      if (response.context == context)
      {
        this.listViewResults.Items.Add(this.toListViewItem(response.result));
      }
    });
{% endhighlight %}

The changes in this code all have to do with the asynchronous context. The local "context" variable always refers to the currently valid context (all other contexts are, by definition, invalid). When a new user search request is detected, we create a new context for the request, and we "bind" the context to the search request using an anonymous projection.

The second block of code (defining how we perform a search) is the same. The search results are treated a bit differently, though: we "bind" each search result to the same context associated with the search request. Also, when the search is completed, the request's bound context is verified against the current context before updating the UI.

Finally, the bound context for each response is verified against the current context before updating the UI. Remember that each response's context is copied from their associated requests's context, so they remain valid as long as their request is the most recent one.

Note that all context-based actions (setting the current context when starting a request, binding the current context to the observable elements, and verifying the bound contexts against the current context) are all done on the UI thread. Synchronizing context actions is a requirement for asynchronous contexts, to avoid race conditions.

## A Reusable Solution

I'm playing around with a few classes that make asynchronous contexts a little easier to use. Observable elements bound to a context are placed into a structure similar to Timestamped\<T> (which binds observable elements to a timestamp), and there are special binding and verification operators. The actual AsynchronousContext type also includes thread checking to ensure that it is used in a synchronized fashion.

However, I'm just not pleased with how usable it is. I'll continue playing with it over the next week or so, and if I can find a good solution, I'll post it here and put it into [Nito.Async](http://nitoasync.codeplex.com/). Suggestions are welcome. :)

