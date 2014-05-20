---
layout: post
title: "Task.Run vs BackgroundWorker, Round 1: The Basic Pattern"
series: "Task.Run vs. BackgroundWorker"
seriesTitle: "Task.Run vs BackgroundWorker, Round 1: The Basic Pattern"
---
## ~ Ready? ~

I'm going to just use a simple Windows Forms framework for my test code. WinForms is a pretty common denominator for most developers; just keep in mind that both `BackgroundWorker` and `Task.Run` are using `SynchronizationContext` underneath, so these same principles apply regardless of platform (WPF, Windows Store, MonoTouch, MonoDroid, Windows Phone, Silverlight, ASP.NET, etc). I'm just using WinForms because it's simple and pretty much everyone knows it.



### The Basic Pattern: Do Work

The core problem that `BackgroundWorker` originally solved was the need to _execute synchronous code on a background thread_. If you're using `BackgroundWorker` for asynchronous or parallel work then just stop; you're not using the right tool in the first place. The core problem for `BackgroundWorker` is to execute synchronous code on a background thread.



Our example (synchronous) action is just going to sleep for a second.



### The Basic Pattern: Completion

In almost every real-world scenario, we also want to be notified when the background operation has completed. Also, most of the time, we want our "handle the completion" code to run on the original context (e.g., a UI context so we could update the UI). It would be best if this marshaling back to the UI thread could be automatic.



Our example completion code will just toss up a message box.



## ~ Fight! ~

### BackgroundWorker

{% highlight csharp %}private void button1_Click(object sender, EventArgs e)
{
    var bgw = new BackgroundWorker();
    bgw.DoWork += (_, __) =>
    {
        Thread.Sleep(1000);
    };
    bgw.RunWorkerCompleted += (_, __) =>
    {
        MessageBox.Show("Hi from the UI thread!");
    };
    bgw.RunWorkerAsync();
}
{% endhighlight %}

### Task.Run

{% highlight csharp %}private async void button2_Click(object sender, EventArgs e)
{
    await Task.Run(() =>
    {
        Thread.Sleep(1000);
    });
    MessageBox.Show("Hi from the UI thread!");
}
{% endhighlight %}

### Discussion

Both of these are pretty straightforward. Both of them will marshal our `MessageBox.Show` back to the UI thread, so we don't have to worry about it.



The `BackgroundWorker` code does suffer from more "ceremony", since it has to deal with events. It's also a bit awkward in that you have to wire up your events first and then explicitly start the work going. The equivalent `Task.Run` is simpler - not a _lot_ simpler, but simpler nonetheless.


