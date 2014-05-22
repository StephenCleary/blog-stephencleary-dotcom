---
layout: post
title: "Task.Run vs BackgroundWorker: Conclusion"
series: "Task.Run vs. BackgroundWorker"
seriesTitle: "Conclusion"
---
In this [series on Task.Run vs BackgroundWorker]({% post_url 2013-05-02-taskrun-vs-backgroundworker-intro %}), we've looked at the most common aspects of running background tasks. As a recap, here's the full list of posts in chronological order:

- [Introduction]({% post_url 2013-05-02-taskrun-vs-backgroundworker-intro %}) - we're only contrasting `Task.Run` with `BackgroundWorker` for situations that `BackgroundWorker` was designed for.
- [Round 1: Basics]({% post_url 2013-05-09-taskrun-vs-backgroundworker-round-1 %}) - how to run code on a background thread and receive a completion notification marshaled back to the UI thread. The `Task.Run` code is shorter and simpler with less "ceremony code".
- [Round 2: Errors]({% post_url 2013-07-26-taskrun-vs-backgroundworker-round-2 %}) - how to handle exceptions from the background thread code. The `Task.Run` code uses the more natural and less error-prone `try/catch` blocks, and has less error-prone exception propagation.
- [Round 3: Results]({% post_url 2013-08-01-taskrun-vs-backgroundworker-round-3 %}) - how to retrieve a result value from the background thread. The `Task.Run` code uses the more natural `return` statement and the result value is strongly-typed.
- [Round 4: Cancellation]({% post_url 2013-09-12-taskrun-vs-backgroundworker-round-4 %}) - how to cancel the background thread. The `Task.Run` code uses the common cancellation framework, which is simpler, less error-prone, and interoperates more cleanly with other cancellation-aware APIs.
- [Round 5: Progress Reports]({% post_url 2013-09-19-taskrun-vs-backgroundworker-round-5 %}) - how to support progress updates from the background thread. The `Task.Run` code uses a strongly-typed progress report type.

What I am not planning to cover in this series are more complex situations, which is actually where `Task.Run` _really_ outperforms `BackgroundWorker`. For example, nesting one background operation within another is easier with `Task.Run`. Also, anything like [waiting for two separate background operations to complete before doing something else](http://stackoverflow.com/questions/18659124/merging-the-results-of-two-background-workers-upon-completion/18659509#18659509) is much easier with `Task.Run`. Pretty much any time you have to _coordinate_ background operations, `Task.Run` code is going to be _much_ simpler!

I hope that this series is sufficient to convince you that `BackgroundWorker` is a type that should not be used in new code. Everything it can do, `Task.Run` can do better; and `Task.Run` can do a lot of things that `BackgroundWorker` can't!

I'll leave you with a "combined" example. The code below starts a cancelable background operation that reports progress, and will either throw an exception or return a value. These are all the basic operations of `BackgroundWorker`. One of these uses `BackgroundWorker` and the other uses `Task.Run`. Don't just look at the length of the code; consider all the little nuances of how it works (type safety, how easily the API can be misused, etc). Then ask yourself: which code would I rather maintain?

{% highlight csharp %}
private BackgroundWorker _bgw;
private void button1_Click(object sender, EventArgs e)
{
  var fail = checkBox1.Checked;
  _bgw = new BackgroundWorker();
  var bgw = _bgw;
  bgw.WorkerSupportsCancellation = true;
  bgw.WorkerReportsProgress = true;
  bgw.DoWork += (_, args) =>
  {
    for (int i = 0; i != 100; ++i)
    {
      bgw.ReportProgress(0, i + "%");
      if (bgw.CancellationPending)
      {
        args.Cancel = true;
        return;
      }
      Thread.Sleep(100);
    }
    if (fail)
      throw new InvalidOperationException("Requested to fail.");
    args.Result = 13;
  };
  bgw.ProgressChanged += (_, args) =>
  {
    label1.Text = (string)args.UserState;
  };
  bgw.RunWorkerCompleted += (_, args) =>
  {
    if (args.Cancelled)
    {
      label1.Text = "Cancelled.";
    }
    else if (args.Error == null)
    {
      var result = (int)args.Result;
      label1.Text = "Completed: " + result;
    }
    else
    {
      label1.Text = args.Error.GetType().Name + ": " + args.Error.Message;
    }
  };
  bgw.RunWorkerAsync();
}
private void cancelButton1_Click(object sender, EventArgs e)
{
  if (_bgw != null)
    _bgw.CancelAsync();
}
{% endhighlight %}

{% highlight csharp %}
private CancellationTokenSource _cts;
private async void button2_Click(object sender, EventArgs e)
{
  var fail = checkBox1.Checked;
  _cts = new CancellationTokenSource();
  var token = _cts.Token;
  var progressHandler = new Progress<string>(value =>
  {
    label2.Text = value;
  });
  var progress = progressHandler as IProgress<string>;
  try
  {
    var result = await Task.Run(() =>
    {
      for (int i = 0; i != 100; ++i)
      {
        if (progress != null)
          progress.Report(i + "%");
        token.ThrowIfCancellationRequested();
        Thread.Sleep(100);
      }
      if (fail)
        throw new InvalidOperationException("Requested to fail.");
      return 13;
    });
    label2.Text = "Completed: " + result;
  }
  catch (OperationCanceledException)
  {
    label2.Text = "Cancelled.";
  }
  catch (Exception ex)
  {
    label2.Text = ex.GetType().Name + ": " + ex.Message;
  }
}
private void cancelButton2_Click(object sender, EventArgs e)
{
  if (_cts != null)
    _cts.Cancel();
}
{% endhighlight %}