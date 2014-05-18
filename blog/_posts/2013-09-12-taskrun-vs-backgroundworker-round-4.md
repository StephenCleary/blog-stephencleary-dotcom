---
layout: post
title: "Task.Run vs BackgroundWorker, Round 4: Cancellation"
tags: ["async", ".NET", "Task.Run vs BackgroundWorker"]
---
## ~ Ready? ~



Cancellation is a common requirement for background tasks, especially when those tasks are consuming resources (like the CPU). In this round, we'll be looking at the built-in cancellation support offered by `BackgroundWorker` and `Task.Run`.





For our example, the background operation is just to sleep 100 times, 100ms at a time, for a total of 10 seconds. A second button is used to cancel the operation.



## ~ Fight! ~

### BackgroundWorker



`BackgroundWorker` has its own, unique way of doing cancellation. First, when constructing the BGW instance, be sure to set `BackgroundWorker.WorkerSupportsCancellation` to `true`. Then, the calling code can request the worker to cancel by calling `BackgroundWorker.CancelAsync`.





`CancelAsync` sets `BackgroundWorker.CancellationPending` to `true`. The `DoWork` handler should periodically check `CancellationPending`, and when it detects cancellation, it should set `DoWorkEventArgs.Cancel` to `true`. The calling code can check whether cancellation happened by reading `RunWorkerCompletedEventArgs.Cancelled`.





Here's the code:



{% highlight csharp %}private BackgroundWorker _bgw;
private void button1_Click(object sender, EventArgs e)
{
    _bgw = new BackgroundWorker();
    var bgw = _bgw;
    bgw.WorkerSupportsCancellation = true;
    bgw.DoWork += (_, args) =>
    {
        for (int i = 0; i != 100; ++i)
        {
            if (bgw.CancellationPending)
            {
                args.Cancel = true;
                return;
            }
            Thread.Sleep(100);
        }
    };
    bgw.RunWorkerCompleted += (_, args) =>
    {
        if (args.Cancelled)
            MessageBox.Show("Cancelled.");
        else
            MessageBox.Show("Completed.");
    };
    bgw.RunWorkerAsync();
}
private void cancelButton1_Click(object sender, EventArgs e)
{
    if (_bgw != null)
        _bgw.CancelAsync();
}
{% endhighlight %}



One of the biggest drawbacks to cancellation support in `BackgroundWorker` is that it's just plain convoluted. Even when I was using `BackgroundWorker` regularly, every time I had to support cancellation, I had to look up how to do it. It's just too complex to remember easily.





Another minor drawback is how cancellation is observed in `RunWorkerCompleted`; it can be easy to overlook the fact that the operation was cancelled. `BackgroundWorker` had a similar problem with [error handling](http://blog.stephencleary.com/2013/07/taskrun-vs-backgroundworker-round-2.html).



### Task.Run



`Task.Run` uses the same [cooperative cancellation model](http://msdn.microsoft.com/en-us/library/dd997364.aspx) used by the rest of the .NET 4.0 framework. We note once again that `BackgroundWorker` was passed over when other types were updated to use `CancellationToken` - maybe that should tell us something...





Since `Task.Run` uses the same cancellation support as every other modern API, it's much easier to remember. Also, it's easier to implement:



{% highlight csharp %}private CancellationTokenSource _cts;
private async void button2_Click(object sender, EventArgs e)
{
    _cts = new CancellationTokenSource();
    var token = _cts.Token;
    try
    {
        await Task.Run(() =>
        {
            for (int i = 0; i != 100; ++i)
            {
                token.ThrowIfCancellationRequested();
                Thread.Sleep(100);
            }
        });
        MessageBox.Show("Completed.");
    }
    catch (OperationCanceledException)
    {
        MessageBox.Show("Cancelled.");
    }
}
private void cancelButton2_Click(object sender, EventArgs e)
{
    if (_cts != null)
        _cts.Cancel();
}
{% endhighlight %}



Compared to the `BackgroundWorker` code, the `Task.Run` code has fewer moving pieces, so it's simpler and shorter. Another benefit of the `Task.Run` approach is that it treats cancellation as an exceptional condition. This means you can't ignore it accidentally and continue on thinking that the operation completed. It also allows you to propagate cancellation very naturally.





There's yet another advantage to the `Task.Run` approach, that is rather subtle. A lot of thought went into the `CancellationTokenSource`/`CancellationToken` design, and it shows. The operation that supports cancellation only knows about the `CancellationToken`; it doesn't need to know its own `Task` or even the `CancellationTokenSource`; all it has is the _token_, which only allows it to _react_ to cancellation. This is a much cleaner design than the `BackgroundWorker` approach, where the `DoWork` handler _has_ to interact with its own `BackgroundWorker` instance.



### Discussion



Once again, `Task.Run` wins this round. Should that really be a surprise at this point? This time, the benefits of `Task.Run` are shorter code, simpler code, using common types, and a well-designed API that encourages separation of concerns.


