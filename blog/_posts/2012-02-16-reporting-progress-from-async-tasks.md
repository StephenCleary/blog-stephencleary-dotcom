---
layout: post
title: "Reporting Progress from Async Tasks"
---
Today, we'll look at how async methods satisfy a [common requirement]({% post_url 2010-08-16-various-implementations-of-asynchronous %}) of background operations: reporting progress.

## Progress Reporter Abstraction

When asynchronous methods report progress, they use an abstraction of the "progress reporter" concept: [**IProgress<in T>**](http://msdn.microsoft.com/en-us/library/hh138298(v=VS.110).aspx). This interface has a single method: **void Report(T value)**. You can't get much simpler than that!

An asynchronous method that wants to report progress just takes an IProgress<T> parameter, with some appropriate type for T. There are two important things to keep in mind:

1. The parameter can be null. This means that no progress reports are needed.
1. IProgress<T>.Report is thread-safe, but asynchronous. In other words, you're "posting" the progress reports to the progress reporter. The progress reporter probably hasn't responded to the progress update by the time your method continues.

That second rule can trip people up - it means _you can't modify the progress object after it's passed to Report._ It is an error to keep a single "current progress" object, update it, and repeatedly pass it to Report.

To avoid this problem, you should create a new progress object each time you call Report. This is easy if your progress type is a value type (the compiler makes a copy of it for you). Alternatively, you could make your progress type immutable and make your own copies.

> This is one small step towards a functional mindset. Async/await will gently nudge you away from OOP and towards functional programming. This is natural and should be embraced.

## Progress Reporter Implementation

Now let's look at the "receiving" side of progress reports. The caller of the asynchronous method passes in the progress reporter, so it has complete control of how progress reports are handled.

There is one built-in progress reporter: [Progress<T>](http://msdn.microsoft.com/en-us/library/hh193692(v=vs.110).aspx). You can either pass an Action<T> into the constructor or handle the ProgressChanged event.

One important aspect of this class is that it invokes ProgressChanged (and the Action<T>) in the context in which it was constructed. So it's natural to write UI updates:

    public async void StartProcessingButton_Click(object sender, EventArgs e)
    {
      // The Progress<T> constructor captures our UI context,
      //  so the lambda will be run on the UI thread.
      var progress = new Progress<int>(percent =>
      {
        textBox1.Text = percent + "%";
      });
    
      // DoProcessing is run on the thread pool.
      await Task.Run(() => DoProcessing(progress));
      textBox1.Text = "Done!";
    }
    
    public void DoProcessing(IProgress<int> progress)
    {
      for (int i = 0; i != 100; ++i)
      {
        Thread.Sleep(100); // CPU-bound work
        if (progress != null)
          progress.Report(i);
      }
    }

The context keeps the updates nicely synchronized.

However, this doesn't work as well if there's no context to capture. In this case, Progress<T> uses the thread pool context, and you'll have to deal with these problems:

 - Multiple simultaneous updates. Since the event is raised on a thread pool thread, fast updates can cause the same event handlers to run on different thread pool threads at the same time.
 - Updates after completion. If a method issues an update just before it completes, the event may be raised on a thread pool thread _after_ the task has been completed!

You do need to be aware of these problems when using Progress<T> without a UI context. We'll cover more advanced progress composition in a later post, and consider solutions to these problems.

## Progress Report Exceptions

Progress<T> raises its event within a captured context. However, this event is not wrapped in a Task or anything like that; it is just executed directly. This means that any exceptions from that event's handlers will propagate directly to the context.

In other words, exceptions from Progress<T>.ProgressChanged are treated just like exceptions from other event handlers.

In _other_ words, don't throw exceptions from Progress<T>.ProgressChanged. :)

## More Progress Reporter Implementations!

The callback-based Progress<T> is great for general use, but there's no reason you couldn't write your own IProgress<T> that works better with your own code base. Here are some implementations from the [AsyncEx library:](http://nitoasyncex.codeplex.com)

  - **PropertyProgress<T>** has a property called Progress and implements [INotifyPropertyChanged](http://msdn.microsoft.com/en-us/library/system.componentmodel.inotifypropertychanged.aspx), so progress updates can update data bindings. This implementation also captures its context just like Progress<T>, which is expected for data binding updates.
  - **ObserverProgress<T>** forwards progress updates to an [IObserver<T>](http://msdn.microsoft.com/en-us/library/dd783449.aspx), where they can be composed using Rx.
  - **ProducerProgress<T>** and **DataflowProgress<T>** both place progress updates into containers (an [IProducerConsumerCollection<T>](http://msdn.microsoft.com/en-us/library/dd287147.aspx) or an [ITargetBlock<TInput>](http://msdn.microsoft.com/en-us/library/hh194833(v=VS.110).aspx), respectively).

## Defining "Progress"

We've covered a lot about progress reporting without actually saying much about the progress update itself (other than it must be passed by value - so either a value type, or an immutable reference type works best).

> The information in this section is not Gospel. It's just a tip from my own (limited) experience dealing with progress updates from async methods. YMMV.

It's natural to think of a progress report as _cumulative_ - the canonical example being "percent complete." However, I recommend a different approach: have _incremental_ progress reports for all reusable code and only convert it to _cumulative_ just before it is displayed to the user.

So an FTP file downloader would report the number of bytes transferred after each write to disk, not the entire number of bytes transferred so far:

    public async Task DownloadFileAsync(string fileName, IProgress<int> progress)
    {
      using (var fileStream = ...) // Open local file for writing
      using (var ftpStream = ...) // Open FTP stream
      {
        while (true)
        {
          var bytesRead = await ftpStream.ReadAsync(...);
          if (bytesRead == 0)
            return;
          await fileStream.WriteAsync(...);
          if (progress != null)
            progress.Report(bytesRead);
        }
      }
    }

Perhaps it's just me, but I find it easier to compose incremental updates like this rather than cumulative ones.

