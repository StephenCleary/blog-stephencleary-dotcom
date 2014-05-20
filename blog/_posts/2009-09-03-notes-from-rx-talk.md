---
layout: post
title: "Notes from the Rx talk"
---
As someone who does a lot of asynchronous programming on .NET, I had heard about the Rx ("reactive") framework some time ago. The details online have been a little sketchy, so I finally took some time to watch [Erik Meijer's talk](http://www.langnetsymposium.com/2009/talks/23-ErikMeijer-LiveLabsReactiveFramework.html). Below are my notes and initial thoughts. I haven't had time to play with the actual framework itself, but it's something that is definitely coming to .NET eventually (it's rumored to be in 4.0).

This talk is titled "Live Labs Reactive Framework", given by Erik Meijer at the 2009 Lang.NET Symposium.

- LINQ is derived from Haskell ideas.
- Reactive programming is where the program asks the environment for input. But not with blocking, like Console.ReadLine does.
- Enumerable: consumer pulls successive elements from the collection, with blocking.
- Observable: producer pushes successive elements from the collection.
- There is a mathematical duality between Enumerable and Observable, which allows theorems proven for one system to have a dual theorem in the other system.
- Consider Enumerable:

{% highlight csharp %}
interface IEnumerable<out T> { IEnumerator<T> GetEnumerator(); }
interface IEnumerator<out T> : IDisposable { bool MoveNext(); T Current { get; } /* Implicit throw */ }
{% endhighlight %}
   - There are actually two interface concepts here (Enumerable and Disposable). For now, ignore Disposable.
   - Swap arguments and results (mathematical duality):
{% highlight csharp %}
interface IObservable<in T> { IDisposable Register(IObserver<T> o); }
interface IObserver<in T> { void OnCompleted(bool done); T OnUpdate { set; } void OnError(Exception e); }
{% endhighlight %}
   - A cleaner interface (getting rid of "bool done" by only calling "OnCompleted" when it's true; changing OnUpdate to a method):
{% highlight csharp %}
interface IObservable<in T> { IDisposable Attach(IObserver<T> o); }
interface IObserver<in T> { void OnCompleted(); void OnUpdate(T value); void OnError(Exception e); }
{% endhighlight %}

 - This makes the Iterator pattern (Enumerable) related to Subject Observer pattern (Observable).
 - Java has Observable/Observer, but in non-generic and noisy interfaces.
 - Since Enumerables are monads, Observables are monads. Since LINQ concepts work on monads, then there is a LINQ for Observables.
 - (At this point, Erik skipped some slides; everything under this point is my own interpretation of those slides)
  - Combinators include Select, Where, Flatten, and SelectMany (which is Select + Flatten). [Note: Enumerable LINQ also supports flattening through SelectMany].
  
       - Interactive SelectMany uses a nested foreach, which iterates each element of each sequence, moving on to the next sequence when it's empty.
       - Reactive SelectMany uses a parallel foreach, which iterates each element of any sequence as they are pushed.
  
     - Additional combinators include:
  
        - Until (to "cancel" reactions for an event)
        - TimeOut
        - Zip (to perform a logical combination of two event sequences)
        - Scan (?)
        - Take, Drop - like TakeWhile/SkipWhile (?)
        - Sample, Buffer
  

    - AJAX is all about responding to events and performing asynchronous computations; it's ideal for Rx.
    - Rx has bridges from .NET events into an Observable collection.
    - A common bug is being dependent on the order of asynchronous completions.
    - Preemption operator (".Until") to listen for an event until there's another situation (like another async computation being started, so the results of the previous computation should be ignored).
    - The let operator (".Let") avoids undesired side effects (similar to a local variable).

       - Lazy evaluation with Observable makes this difficult.

     - Comega join patterns (zips) and parser combinators can also be supported.

        - Zip enables logic such as "(an A event and B event were pushed) or (a B event and C event were pushed)" to be treated as its own event stream.

      - Related work:

         - F# first-class events
         - F# async workflows
         - Thomasp Reactive LINQ
         - Esterelle, Lustre
         - Functional reactive programming
         - Using iterators for async
         - PowerShell, SSIS, WWF

       - Rx is a way of composing delimited continuations: see paper "Delimited continuations in Operating Systems".
       - (At this point, Erik skipped some more slides; everything under this point is my own interpretation of those slides)

          - C# pseudocode (using object expressions / anonymous inner classes) to convert a C# event to an Observable:
{% highlight csharp %}
class Control { event Action<T> KeyUp; }
IObservable<T> GetKeyUp(this Control w)
{
  return new IObservible<T>
  {
    IDisposable Attach(IObserver<T> h)
    {
      var d = new Action<T>(h.Yield); // Create delegate to call observer
      w.KeyUp += d; // Attach observer to event directly
      return new IDisposable
      {
        void Dispose() { w.KeyUp -= d; } // Disconnect on dispose
      };
    }
  };
}
{% endhighlight %}
          - Two IObservable<T> extension methods exist for attaching delegates:
{% highlight csharp %}
static IDisposable Attach<T>(this IObservable<T> src, Action<T> yield);
static IDisposable Attach<T>(this IObservable<T> src, Action<T> yield, Action<Exception> throw);
{% endhighlight %}
  
             - The original style of event subscription treats the handler as a first-class object:
{% highlight csharp %}
Action<T> handler = ...;
txtbox.KeyUp += handler;
txtbox.KeyUp -= handler;
{% endhighlight %}
             - The Observable style of event subscription treats the event as a first-class object:
{% highlight csharp %}
var keyup = txtbox.GetKeyUp();
var detacher = keyup.Attach(...);
detacher.Dispose();
{% endhighlight %}
  
           - Observable event collections when used with combinators allow implicit state variables. e.g., consider a "drag & drop":
{% highlight csharp %}
var W = ... control to be dragged ...;
 
// Set up mouse down/up detector
var mouseDowns = from md in W.GetMouseDown() select true; // Pushes "true" every time the mouse button goes down
var mouseUps = from md in W.GetMouseUp() select false; // Pushes "false" every time the mouse button goes up
var mouseClicks = mouseDowns.Merge(mouseUps); // A simple combination; pushes "true" when down, "false" when up
 
// Set up mouse movement detector and measurer
var mouseMoves = from mm in W.GetMouseMove() select new { mm.X, mm.Y }; // Pushes the X,Y location of the mouse every time it moves
// (This next line has a bug)
var mouseDiffs = from diff in mouseMoves.Skip(1).Zip(mouseMoves) // This part pushes the current and last location of the mouse every time it moves
                 select new { dX = diff.First.X - diff.Second.X,
                              dY = diff.First.Y - diff.Second.Y }; // Pushes the difference in location each time the mouse moves
 
// Set up mouse drag & drop detector
var mouseDrag = from mousedown in mouseClicks
                from delta in mouseDiffs where leftdown
                select delta; // Pushes the difference in location each time the mouse moves while a button is down
 
mouseDrag.Attach(delta => { ... move W according to delta ... });
{% endhighlight %}
           - You always need to be aware of side effects; use the Let operator to tame them:
{% highlight csharp %}
var mouseDiffs = from diff in mouseMoves.Skip(1).Zip(mouseMoves) // This part pushes the current and last location of the mouse every time it moves
                 select new { dX = diff.First.X - diff.Second.X,
                              dY = diff.First.Y - diff.Second.Y }; // Pushes the difference in location each time the mouse moves
{% endhighlight %}
    should be:
{% highlight csharp %}
var mouseDiffs = mouseMoves.Let(_mouseMoves => // This part avoids side effects on mouseMoves
                 from diff in _mouseMoves.Skip(1).Zip(_mouseMoves) // This part pushes the current and last location of the mouse every time it moves
                 select new { dX = diff.First.X - diff.Second.X,
                              dY = diff.First.Y - diff.Second.Y }; // Pushes the difference in location each time the mouse moves
{% endhighlight %}

## My thoughts

Asynchronous programming has always been difficult. One of the hardest parts is the correct handling of "asynchronous state", objects whose sole purpose in life is to track the state of input as events come in. I expect the Rx framework will really shine at removing the need for explicitly tracking state, especially noticeable in situations where keeping "asynchronous state" is complex.

The documentation will have to be good regarding side effects, so that programmers can more easily determine when Let/Until operators are required. Currently, I'm not aware of any documentation, even on the .NET 4.0 beta MSDN. This is just a preemptive warning: the docs will have to be really, really good; better than what was done for LINQ (where the docs are often unclear which operators cause buffering).

There is still a missing piece in the "pipeline story". Historically, every language has gradually developed the same ideas: consumers, then producers, then a full pipeline. Push algorithms are usually the response to the "tee problem" in a pull-based framework. I did the same thing in C++ about 6 years ago, developing what I called "pipe algorithms", named after the Unix pipe. Currently, we have Enumerable (pull) and Observable (push) models, but there are no easy translators between the two (these "translators" require dedicated threads).

It's also important to note that one is not "better" than the other. Most algorithms are more naturally defined in an Enumerable model, so that is the one that is more intuitive. Other algorithms are a more natural fit to the Observable model. Once we have translators back and forth between the two models, then we'll have a more mature language.

One person stated that the Rx framework will remove the need for EBAP components. I have not yet been convinced of this; I see EBAP as still being a useful technique for synchronizing events. The synchronized events can then be used as input to an Rx query. In this sense, I see the EBAP and Rx approaches as complementing each other.

