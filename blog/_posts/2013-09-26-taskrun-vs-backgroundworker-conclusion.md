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

<div style="float:left;">
<pre><code class="csharp" style="max-height:none;overflow:visible;"><span class="keyword">private</span> BackgroundWorker _bgw;
<span class="keyword">private</span> <span class="keyword">void</span> button1_Click(<span class="keyword">object</span> sender, EventArgs e)
{
    <span class="keyword">var</span> fail = checkBox1.Checked;
    _bgw = <span class="keyword">new</span> BackgroundWorker();
    <span class="keyword">var</span> bgw = _bgw;
    bgw.WorkerSupportsCancellation = <span class="keyword">true</span>;
    bgw.WorkerReportsProgress = <span class="keyword">true</span>;
    bgw.DoWork += (_, args) =&gt;
    {
        <span class="keyword">for</span> (<span class="keyword">int</span> i = 0; i != 100; ++i)
        {
            bgw.ReportProgress(0, i + <span class="string">"%"</span>);
            <span class="keyword">if</span> (bgw.CancellationPending)
            {
                args.Cancel = <span class="keyword">true</span>;
                <span class="keyword">return</span>;
            }
            Thread.Sleep(100);
        }
        <span class="keyword">if</span> (fail)
            <span class="keyword">throw</span> <span class="keyword">new</span> InvalidOperationException(<span class="string">"Requested to fail."</span>);
        args.Result = 13;
    };
    bgw.ProgressChanged += (_, args) =&gt;
    {
        label1.Text = (<span class="keyword">string</span>)args.UserState;
    };
    bgw.RunWorkerCompleted += (_, args) =&gt;
    {
        <span class="keyword">if</span> (args.Cancelled)
        {
            label1.Text = <span class="string">"Cancelled."</span>;
        }
        <span class="keyword">else</span> <span class="keyword">if</span> (args.Error == <span class="keyword">null</span>)
        {
            <span class="keyword">var</span> result = (<span class="keyword">int</span>)args.Result;
            label1.Text = <span class="string">"Completed: "</span> + result;
        }
        <span class="keyword">else</span>
        {
            label1.Text = args.Error.GetType().Name + <span class="string">": "</span> + args.Error.Message;
        }
    };
    bgw.RunWorkerAsync();
}
<span class="keyword">private</span> <span class="keyword">void</span> cancelButton1_Click(<span class="keyword">object</span> sender, EventArgs e)
{
    <span class="keyword">if</span> (_bgw != <span class="keyword">null</span>)
        _bgw.CancelAsync();
}
</code></pre>
</div>
<div style="float:right;">
<pre><code class="csharp" style="max-height:none;overflow:visible;"><span class="keyword">private</span> CancellationTokenSource _cts;
<span class="keyword">private</span> <span class="keyword">async</span> <span class="keyword">void</span> button2_Click(<span class="keyword">object</span> sender, EventArgs e)
{
    <span class="keyword">var</span> fail = checkBox1.Checked;
    _cts = <span class="keyword">new</span> CancellationTokenSource();
    <span class="keyword">var</span> token = _cts.Token;
    <span class="keyword">var</span> progressHandler = <span class="keyword">new</span> Progress&lt;<span class="keyword">string</span>&gt;(<span class="keyword">value</span> =&gt;
    {
        label2.Text = <span class="keyword">value</span>;
    });
    <span class="keyword">var</span> progress = progressHandler <span class="keyword">as</span> IProgress&lt;<span class="keyword">string</span>&gt;;
    <span class="keyword">try</span>
    {
        <span class="keyword">var</span> result = <span class="keyword">await</span> Task.Run(() =&gt;
        {
            <span class="keyword">for</span> (<span class="keyword">int</span> i = 0; i != 100; ++i)
            {
                <span class="keyword">if</span> (progress != <span class="keyword">null</span>)
                    progress.Report(i + <span class="string">"%"</span>);
                token.ThrowIfCancellationRequested();
                Thread.Sleep(100);
            }
            <span class="keyword">if</span> (fail)
                <span class="keyword">throw</span> <span class="keyword">new</span> InvalidOperationException(<span class="string">"Requested to fail."</span>);
            <span class="keyword">return</span> 13;
        });
        label2.Text = <span class="string">"Completed: "</span> + result;
    }
    <span class="keyword">catch</span> (OperationCanceledException)
    {
        label2.Text = <span class="string">"Cancelled."</span>;
    }
    <span class="keyword">catch</span> (Exception ex)
    {
        label2.Text = ex.GetType().Name + <span class="string">": "</span> + ex.Message;
    }
}
<span class="keyword">private</span> <span class="keyword">void</span> cancelButton2_Click(<span class="keyword">object</span> sender, EventArgs e)
{
    <span class="keyword">if</span> (_cts != <span class="keyword">null</span>)
        _cts.Cancel();
}
</code></pre>
</div>
