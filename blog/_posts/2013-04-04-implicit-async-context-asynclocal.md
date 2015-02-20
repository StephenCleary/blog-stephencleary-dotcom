---
layout: post
title: "Implicit Async Context (\"AsyncLocal\")"
---
Occasionally, someone will ask about support for some kind of implicit "context" that will flow with `async` code. The most common use case is for logging, where you can maintain a stack of logical operations so every time you log you capture (and log) the state of that stack (e.g., [CorrelationManager](http://msdn.microsoft.com/en-us/library/1fxyt46s.aspx) for `TraceSource`, or the Nested Diagnostic Context for log4net). [Jon Skeet has a great blog entry on this kind of implicit "context" with several possible uses](http://codeblog.jonskeet.uk/2010/11/08/the-importance-of-context-and-a-question-of-explicitness/).

<!--
<p>If you're on ASP.NET, you can use <code class="csharp"><span class="type">HttpContext</span>.Current.Items</code>, which does flow by default with <code class="csharp"><span class="keyword">async</span></code> code. (Of course, this is not a recommended design, for code separation and testability reasons). You could also <a href="http://connectedproperties.codeplex.com/">attach properties</a> to other <code class="csharp"><span class="type">SynchronizationContext</span></code> instances, as long as you kept the <a href="http://blog.stephencleary.com/2009/09/another-synchronizationcontext-gotcha.html">limitations of such an approach</a> in mind. But neither <code class="csharp"><span class="type">HttpContext</span></code> nor <code class="csharp"><span class="type">SynchronizationContext</span></code> will get you a solution that works everywhere, even in 
the thread pool context.</p>
-->

<!--

<h4>Solution A: (Ab)Use Classes</h4>

<p>As I kept bringing up in my <a href="http://blog.stephencleary.com/search/label/async%20oop">async OOP series</a>, <code class="csharp"><span class="keyword">async</span></code> code is <i>functional</i> in nature rather than object-oriented. The natural representation of <code class="csharp"><span class="keyword">async</span></code> methods is purely as (static) methods without actual class instances.</p>

<p>If your <code class="csharp"><span class="keyword">async</span></code> code is "pure" (just methods), then you can (ab)use classes to create a container for those methods, and the instance properties of that class become the implicit "context" for those methods. Something like this:</p>

<pre><code class="csharp"><span class="keyword">public</span> <span class="keyword">sealed</span> <span class="keyword">class</span> <span class="type">AsyncMethodsWithContext</span>
{
    <span class="keyword">private</span> <span class="keyword">int</span> implicitContextValue;

    <span class="keyword">public</span> <span class="keyword">async</span> <span class="type">Task</span> EntryLevelAsync()
    {
        implicitContextValue = 13;
        <span class="keyword">await</span> PrivateAsync();
    }

    <span class="keyword">private</span> <span class="keyword">async</span> <span class="type">Task</span> PrivateAsync()
    {
        <span class="keyword">await</span> <span class="type">Task</span>.Delay(implicitContextValue);
    }
}
</code></pre>

<p>This approach is good from an overhead perspective: it's very efficient. However, it's not so good from a design perspective (it forces all your <code class="csharp"><span class="keyword">async</span></code> methods that share the same context into the same class, whether or not they should be logically grouped). Also, there is only one copy of the implicit state. In a fork/join scenario (e.g., <code class="csharp"><span class="type">Task</span>.WhenAll</code>), it's often useful to have the implicit state "cloned" to each sub-operation so they each get their own local copy of the state. Here's an example:</p>

<pre><code class="csharp"><span class="keyword">public</span> <span class="keyword">sealed</span> <span class="keyword">class</span> <span class="type">AsyncMethodsWithContext</span>
{
    <span class="keyword">private</span> <span class="keyword">readonly</span> <span class="type">ConcurrentStack</span>&lt;<span class="keyword">int</span>&gt; stack = <span class="keyword">new</span> <span class="type">ConcurrentStack</span>&lt;<span class="keyword">int</span>&gt;();

    <span class="keyword">public</span> <span class="keyword">async</span> <span class="type">Task</span> EntryLevelAsync()
    {
        stack.Push(13);
        <span class="keyword">await</span> <span class="type">Task</span>.WhenAll(PrivateAsync(5), PrivateAsync(7));
    }

    <span class="keyword">private</span> <span class="keyword">async</span> <span class="type">Task</span> PrivateAsync(<span class="keyword">int</span> localValue)
    {
        stack.Push(localValue);
        <span class="comment">// What&#39;s the value of the stack here?</span>
        <span class="keyword">await</span> <span class="type">Task</span>.Delay(10);
        <span class="comment">// What&#39;s the value of the stack here?</span>
        <span class="keyword">int</span> local;
        stack.TryPop(<span class="keyword">out</span> local);
    }
}
</code></pre>

<p>In this simple example, we want to keep an implicit stack. <code class="csharp">EntryLevelAsync</code> pushes 13, and each <code class="csharp">PrivateAsync</code> pushes a 5 or 7 (and pops it when done). This kind of approach works fine for linear <code class="csharp"><span class="keyword">async</span></code> code (where you <code class="csharp"><span class="keyword">await</span></code> one operation at a time), but this example is using <code class="csharp"><span class="type">Task</span>.WhenAll</code>.</p>

<p>So, let's consider the values on the stack. When <code class="csharp">EntryLevelAsync</code> calls the first <code class="csharp">PrivateAsync</code>, it pushes a 5, and the stack is <code>{5, 13}</code>. The first <code class="csharp">PrivateAsync</code> yields at its <code class="csharp"><span class="keyword">await</span></code> and <code class="csharp">EntryLevelAsync</code> calls the second <code class="csharp">PrivateAsync</code>. It pushes a 7, and the stack is <code>{7, 5, 13}</code>. At this point our stack is diverging from the actual call stack: the second <code class="csharp">PrivateAsync</code> is not expecting the 5 in the stack.</p>

<p>It gets more complex as the methods resume. Either <code class="csharp">PrivateAsync</code> may complete first, so if the first <code class="csharp">PrivateAsync</code> completes first, it will pop 7 off the stack (remember, it pushed 5), and the second <code class="csharp">PrivateAsync</code> will pop 5 off the stack (when it pushed 7).</p>

<p>This stack confusion is due to the implicit state being shared instead of copied in a fork/join scenario. You can of course do the copying manually (creating a new instance of <code class="csharp"><span class="type">AsyncMethodsWithContext</span></code>), but that detracts from the <i>implicitness</i> of our "implicit context."</p>

<p>So, this solution works well for a limited set of situations: if grouping your methods like this works for your design and if your implicit state can be shared without issues, then I'd recommend just using instance fields.</p>

-->

## Logical CallContext

There is a solution for this problem: the "logical call context", which you can access by [CallContext.LogicalGetData](http://msdn.microsoft.com/en-us/library/system.runtime.remoting.messaging.callcontext.logicalgetdata.aspx) and [CallContext.LogicalSetData](http://msdn.microsoft.com/en-us/library/system.runtime.remoting.messaging.callcontext.logicalsetdata.aspx). The regular call context (`CallContext.GetData` and `CallContext.SetData`) acts just like thread-local storage, which of course doesn't work for `async` methods.

Here's how logical call context works with asynchronous code.

Logical call context data flows with `ExecutionContext`. This means that it's not affected by `ConfigureAwait(continueOnCapturedContext: false)`; you can't "opt-out" of the logical call context. So the logical call context at the beginning of an `async` method will _always_ flow through to its continuations.

When an `async` method starts, it notifies its logical call context to activate copy-on-write behavior. This means the current logical call context is not actually changed, but it is marked so that if your code does call `CallContext.LogicalSetData`, the logical call context data is copied into a new current logical call context before it is changed. **Note: the copy-on-write behavior of logical call contexts is only available on .NET 4.5.**

This "copying" of the logical call context data is a _shallow_ copy. You can think of the logical call context data as an `IDictionary<string, object>` of name/value pairs. When it's copied, it creates a new dictionary and copies all the name/value pairs into the new dictionary. Both dictionaries then refer to all the same actual object instances; there's no "deep cloning" of any of your data being done.

Because the references are shared, it's important not to mutate any values retrieved from the logical call context. If you need to change a logical call context value, update the actual value using `CallContext.LogicalSetData`. **You should only use immutable types as logical call context data values.**

Also note that the design is heavily optimized for the common case: when there is no logical call context data at all. When you start adding "implicit context", you're going to start adding overhead. Probably not too much, though, since everything is shallow-copied at worst.

As a final note, remember that you can end up sharing data two different ways: .NET before 4.5 did not have the copy-on-write behavior, and the copies are shallow. For `async` code, any sharing of data like this will get you in trouble as soon as you start doing fork/join (e.g., `Task.WhenAll`). So follow the two rules: **only .NET 4.5** and **immutable data**!

## An Example

Let's take a simple example. We want to keep an implicit stack of logical operations, and when we call our "log" method we want to output the stack as part of that log.

First, let's define a strongly-typed accessor for the logical call context data we'll be using. Remember, we're only storing immutable data, so we'll use the new [immutable collections](https://nuget.org/packages/Microsoft.Bcl.Immutable):

{% highlight csharp %}
public static partial class MyStack
{
    private static readonly string name = Guid.NewGuid().ToString("N");

    private sealed class Wrapper : MarshalByRefObject
    {
        public ImmutableStack<string> Value { get; set; }
    }

    private static ImmutableStack<string> CurrentContext
    {
        get
        {
            var ret = CallContext.LogicalGetData(name) as Wrapper;
            return ret == null ? ImmutableStack.Create<string>() : ret.Value;
        }

        set
        {
            CallContext.LogicalSetData(name, new Wrapper { Value = value });
        }
    }
}
{% endhighlight %}

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

**Updated 2014-06-03:** Added the `Wrapper` class, which enables code to use `MyStack` in cross-AppDomain calls.
</div>

So far, so good. Now we have a strongly-typed property we can use to get (or update) the current stack. Next we'll start defining our public API. We want the ability to "push" a string onto the stack, and get back a disposable that will pop that string back off the stack when disposed. Simple enough:

{% highlight csharp %}
public static partial class MyStack
{
    public static IDisposable Push([CallerMemberName] string context = "")
    {
        CurrentContext = CurrentContext.Push(context);
        return new PopWhenDisposed();
    }

    private static void Pop()
    {
        CurrentContext = CurrentContext.Pop();
    }

    private sealed class PopWhenDisposed : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
                return;
            Pop();
            disposed = true;
        }
    }
}
{% endhighlight %}

The final part of our public API is a method that returns the current stack. I'll just return it as a string:

{% highlight csharp %}
public static partial class MyStack
{
    public static string CurrentStack
    {
        get
        {
            return string.Join(" ", CurrentContext.Reverse());
        }
    }
}
{% endhighlight %}

Now let's turn our attention to the code that will be using `MyStack`. First, our "log" method:

{% highlight csharp %}
partial class Program
{
    static void Log(string message)
    {
        Console.WriteLine(MyStack.CurrentStack + ": " + message);
    }
}
{% endhighlight %}

Yeah, that was pretty easy.

Our test code is going to be a bit more complex. First, I'm going to push a "Main" value onto the stack that will last for the entire program. Then, I'll start two separate (concurrent) pieces of work called "1" and "2". Each of those are going to log when they start and finish, and they'll each do some more work called "A" and "B" (sequentially). So we should end up with some interleaving of this output from "1":

    Main 1: <SomeWork>
    Main 1 A: <MoreWork>
    Main 1 A: </MoreWork>
    Main 1 B: <MoreWork>
    Main 1 B: </MoreWork>
    Main 1: </SomeWork>

with this output from "2":

    Main 2: <SomeWork>
    Main 2 A: <MoreWork>
    Main 2 A: </MoreWork>
    Main 2 B: <MoreWork>
    Main 2 B: </MoreWork>
    Main 2: </SomeWork>

Remember, "1" and "2" are concurrent, so there's no one right answer for the output. As long as all the messages above are present and in the correct (relative) order, it's acceptable.

The code, without further ado:

{% highlight csharp %}
partial class Program
{
    static void Main(string[] args)
    {
        using (MyStack.Push("Main"))
        {
            Task.WhenAll(SomeWork("1"), SomeWork("2")).Wait();
        }

        Console.ReadKey();
    }

    static async Task SomeWork(string stackName)
    {
        using (MyStack.Push(stackName))
        {
            Log("<SomeWork>");
            await MoreWork("A");
            await MoreWork("B");
            Log("</SomeWork>");
        }
    }

    static async Task MoreWork(string stackName)
    {
        using (MyStack.Push(stackName))
        {
            Log("<MoreWork>");
            await Task.Delay(10);
            Log("</MoreWork>");
        }
    }
}
{% endhighlight %}

One sample run from my machine is:

    Main 1: <SomeWork>
    Main 1 A: <MoreWork>
    Main 2: <SomeWork>
    Main 2 A: <MoreWork>
    Main 2 A: </MoreWork>
    Main 1 A: </MoreWork>
    Main 1 B: <MoreWork>
    Main 2 B: <MoreWork>
    Main 2 B: </MoreWork>
    Main 2: </SomeWork>
    Main 1 B: </MoreWork>
    Main 1: </SomeWork>

If you sort out the "1" and the "2" messages, you'll see that each set is in the correct order and that the stacks are nicely laid out as expected.

Similar code will compile targeting .NET 4.0 (with [Microsoft.Bcl.Async](http://nuget.org/packages/Microsoft.Bcl.Async/)); however, it will _not_ work correctly (unless it happens to run on .NET 4.5) because the logical call context is not copied at the right time. In that situation, different parts of different `async` methods will end up sharing the same stack (and overwriting each other's stack).

As a final reminder: this will work **only on .NET 4.5** and it only works because we **stored immutable data**.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Update (2014-12-01): For more details, see Recipe 13.4 in my [Concurrency Cookbook]({{ '/book/' | prepend: site.url_www }}){:.alert-link}.
</div>
