---
layout: post
title: "Task.Run vs BackgroundWorker, Round 2: Error Handling"
series: "Task.Run vs. BackgroundWorker"
seriesTitle: "Task.Run vs BackgroundWorker, Round 2: Error Handling"
---
## ~ Ready? ~

Proper error handling is necessary for any application. When you consider various solutions for a problem, don't forget to consider error handling as well! All too often I have seen developers use an inappropriate solution because it was easier in the "success" case. Just as one example: the last few years I've seen many developers use `ThreadPool.QueueUserWorkItem` for background operations; after all, (they think), it's really simple - I can just toss a delegate onto the thread pool! It is true that for the "success" case it's easier to use `ThreadPool.QueueUserWorkItem` than `BackgroundWorker`, but what about for the "failure" case? What happens when the delegate throws an exception? Hint: it's not pretty, and the code they have to write to catch the exception and marshal it to another thread is way more complex than the same code using `BackgroundWorker`.



So, the lesson here is that you need to consider error handling when looking at options. We'll look at optional characteristics later in this series (cancellation, progress reporting, etc), but proper error handling is not optional; it is required.



## ~ Fight! ~

### BackgroundWorker

The `DoWork` event can throw exceptions, which are automatically caught and placed on the `Error` property of the arguments passed to `RunWorkerCompleted`. The code is not too bad:



{% highlight csharp %}private void button1_Click(object sender, EventArgs e)
{
    var bgw = new BackgroundWorker();
    bgw.DoWork += (_, __) =>
    {
        Thread.Sleep(1000);
        throw new InvalidOperationException("Hi!");
    };
    bgw.RunWorkerCompleted += (_, args) =>
    {
        if (args.Error != null)
            MessageBox.Show(args.Error.Message);
    };
    bgw.RunWorkerAsync();
}
{% endhighlight %}

### Task.Run

`Task.Run` will also capture any exceptions and place them on the returned `Task`. When the task is awaited, the exceptions are propagated. This means that you can use the normal try/catch blocks to handle exceptions:



{% highlight csharp %}private async void button2_Click(object sender, EventArgs e)
{
    try
    {
        await Task.Run(() =>
        {
            Thread.Sleep(1000);
            throw new InvalidOperationException("Hi!");
        });
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message);
    }
}
{% endhighlight %}

### Discussion

Personally, I prefer the try/catch system because it is more familiar to developers than `RunWorkerCompletedEventArgs`. Also, it's easy to overlook the exception in `BackgroundWorker.RunWorkerCompleted`; there's no possible way to overlook an exception thrown by `await`!



Let's make the example a little more realistic. Instead of displaying the exception to the user, let's allow it to propagate through the continuation. This is a realistic way to handle unexpected exceptions at this level in the code.



### BackgroundWorker

There's a pretty good "gotcha" when propagating exceptions. If you just re-throw them, then you lose the original stack trace. .NET 4.5 introduced the `ExceptionDispatchInfo` type which can preserve the original stack trace; you just have to remember to use it.



{% highlight csharp %}private void button1_Click(object sender, EventArgs e)
{
    var bgw = new BackgroundWorker();
    bgw.DoWork += (_, __) =>
    {
        Thread.Sleep(1000);
        throw new InvalidOperationException("Hi!");
    };
    bgw.RunWorkerCompleted += (_, args) => ExceptionDispatchInfo.Capture(args.Error).Throw();
    bgw.RunWorkerAsync();
}
{% endhighlight %}

### Task.Run

Since `await` will correctly preserve the stack trace for propagated exceptions, the `Task.Run` code is quite simple:



{% highlight csharp %}private async void button2_Click(object sender, EventArgs e)
{
    await Task.Run(() =>
    {
        Thread.Sleep(1000);
        throw new InvalidOperationException("Hi!");
    });
}
{% endhighlight %}

### Discussion

Whether handling the exception immediately, or propagating the exception, the `Task.Run` code is cleaner and less error-prone than the `BackgroundWorker` code.

