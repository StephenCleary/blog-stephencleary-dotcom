---
layout: post
title: "Task.Run vs BackgroundWorker, Round 5: Reporting Progress"
tags: ["async", ".NET", "Task.Run vs BackgroundWorker"]
---
## ~ Ready? ~



When you have a lengthly background operation, it's considered polite to give some kind of progress notification to the user (if possible). This usually takes the form of a percentage, with some optional extra information (e.g., the name of the current file being processed).





We'll take the same operation as last time (sleep for 100ms 100 times, for a total of 10 seconds), and this time we'll report progress at each iteration. Since we'll be updating the UI with our progress information, we want each progress update to be raised in the UI context.



## ~ Fight! ~

### BackgroundWorker



`BackgroundWorker` has built-in support for progress reporting, and even automatically marshals to the UI thread. Like last time, we have to first set the `BackgroundWorker.WorkerSupportsProgress` property to allow progress reports. Then, the `DoWork` method can call the `BackgroundWorker.ReportProgress` method, which raises the `BackgroundWorker.ProgressChanged` event.



{% highlight csharp %}private void button1_Click(object sender, EventArgs e)
{
    var bgw = new BackgroundWorker();
    bgw.WorkerReportsProgress = true;
    bgw.DoWork += (_, args) =>
    {
        for (int i = 0; i != 100; ++i)
        {
            bgw.ReportProgress(i);
            Thread.Sleep(100);
        }
    };
    bgw.ProgressChanged += (_, args) =>
    {
        label1.Text = args.ProgressPercentage.ToString();
    };
    bgw.RunWorkerCompleted += (_, args) =>
    {
        label1.Text = "Completed.";
    };
    bgw.RunWorkerAsync();
}
{% endhighlight %}



Just like last time, this approach works but is rather convoluted. Again, we see the `BackgroundWorker` type having too many responsibilities: it has the enabling property, the method to report progress, and the event that is raised when the progress changes.



### Task.Run



The new `async` support also introduces a pattern for progress reporting in asynchronous methods: the caller optionally creates an implementation of `IProgress<T>` (usually an instance of `Progress<T>`), and that instance is passed into the asynchronous method. The method then sends progress reports to its `IProgress<T>` instance (if it is not `null`).



{% highlight csharp %}private async void button2_Click(object sender, EventArgs e)
{
    var progressHandler = new Progress<int>(value =>
    {
        label2.Text = value.ToString();
    });
    var progress = progressHandler as IProgress<int>;
    await Task.Run(() =>
    {
        for (int i = 0; i != 100; ++i)
        {
            if (progress != null)
                progress.Report(i);
            Thread.Sleep(100);
        }
    });
    label2.Text = "Completed.";
}
{% endhighlight %}



What we end up here is also somewhat convoluted. Normally, the `IProgress<T>` is a method parameter, but for these examples I'm trying to keep everything in the same method. Also, in this example there's no need to check `progress != null`, but that is standard practice for asynchronous code so I'm including it here.





OK, so the `Task.Run` code is not a _lot_ better than `BackgroundWorker`. It is better, though; it's a bit shorter, and the types definitely have better separation of concerns. So far, I'd way it's winning by technicalities instead of by a knockout.





Let's take the stakes a bit higher. Let's say we want to report strings instead of an integer percentage. Furthermore, let's pretend we're doing an operation where the percent complete is difficult to compute, so instead we'll report strings that describe the current stage of the operation.



## ~ Fight! ~

### BackgroundWorker



`BackgroundWorker.ReportProgress` takes an optional second argument, which is a custom "progress report" instance. This is then available to the progress changed handler as `ProgressChangedEventArgs.UserState`.



{% highlight csharp %}private void button1_Click(object sender, EventArgs e)
{
    var bgw = new BackgroundWorker();
    bgw.WorkerReportsProgress = true;
    bgw.DoWork += (_, args) =>
    {
        for (int i = 0; i != 100; ++i)
        {
            bgw.ReportProgress(0, "Stage " + i);
            Thread.Sleep(100);
        }
    };
    bgw.ProgressChanged += (_, args) =>
    {
        label1.Text = (string)args.UserState;
    };
    bgw.RunWorkerCompleted += (_, args) =>
    {
        label1.Text = "Completed.";
    };
    bgw.RunWorkerAsync();
}
{% endhighlight %}



There are a couple of drawbacks to the way `BackgroundWorker` reports custom progress types. The first is that it is untyped (we just get an `object` instance, which we cast to `string`). The second is that we _must_ pass back a "percent complete", even if there is no way to calculate a meaningful value for that parameter. You can, of course, just pass zero and document that the caller should ignore that value.



### Task.Run



The `IProgress<T>` and `Progress<T>` types allow any type for `T`, so changing the progress report type from `int` to `string` is quite straightforward:



{% highlight csharp %}private async void button2_Click(object sender, EventArgs e)
{
    var progressHandler = new Progress<string>(value =>
    {
        label2.Text = value;
    });
    var progress = progressHandler as IProgress<string>;
    await Task.Run(() =>
    {
        for (int i = 0; i != 100; ++i)
        {
            if (progress != null)
                progress.Report("Stage " + i);
            Thread.Sleep(100);
        }
    });
    label2.Text = "Completed.";
}
{% endhighlight %}

### Discussion



I must conclude again that this round goes to `Task.Run`. The `IProgress<T>` and `Progress<T>` types are strongly typed, have better separation of concerns, and they easily allow any type of custom progress report.


