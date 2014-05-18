---
layout: post
title: "Task.Run vs BackgroundWorker, Round 3: Returning Results"
tags: ["async", ".NET", "Task.Run vs BackgroundWorker"]
---
## ~ Ready? ~



When you perform a background operation - that is, some actual CPU work that you push off to a background thread - it's usually done to calculate some result. Today we're looking at how `Task.Run` and `BackgroundWorker` handle returning results.



## ~ Fight! ~

### BackgroundWorker



It's pretty straightforward to return values from a `BackgroundWorker`; just set the `DoWorkEventArgs.Result` property and then you can pick up the results from `RunWorkerCompletedEventArgs.Result`:



{% highlight csharp %}private void button1_Click(object sender, EventArgs e)
{
    var bgw = new BackgroundWorker();
    bgw.DoWork += (_, args) =>
    {
        Thread.Sleep(1000);
        args.Result = 13;
    };
    bgw.RunWorkerCompleted += (_, args) =>
    {
        var result = (int)args.Result;
        MessageBox.Show("Result is " + result);
    };
    bgw.RunWorkerAsync();
}
{% endhighlight %}



The biggest awkwardness caused by this code is the loss of type information of the result. Both `DoWorkEventArgs.Result` and `RunWorkerCompletedEventArgs.Result` are of type `object`, so you have to cast it to the correct type when retrieving the result.



### Task.Run



The lambda passed to `Task.Run` can simply return a value:



{% highlight csharp %}private async void button2_Click(object sender, EventArgs e)
{
    var result = await Task.Run(() =>
    {
        Thread.Sleep(1000);
        return 13;
    });
    MessageBox.Show("Result is " + result);
}
{% endhighlight %}

### Discussion



The `Task.Run` code uses the natural `return` syntax, is strongly typed, and is more concise than `BackgroundWorker`. This round clearly goes to `Task.Run`.

