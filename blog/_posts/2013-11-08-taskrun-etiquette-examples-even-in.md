---
layout: post
title: "Task.Run Etiquette Examples: Even in the Complex Case, Don't Use Task.Run in the Implementation"
---
Last time, we looked at CPU-bound methods and learned [why we shouldn't use `Task.Run` in the implementation]({% post_url 2013-11-07-taskrun-etiquette-examples-dont-use %}). Rather, we should use it at the point of the call.

Today, let's consider a more advanced scenario. Instead of a purely CPU-bound method, let's perform a much more complex operation. We're going to predict the stock market.

In order to predict the market, our service will need to get some stock quotes from a web service and then perform some very CPU-intensive analysis. During that analysis, the service may need to retrieve more quotes and/or historical data from the web service and then do more calculation.

So this is a much more complex example with both I/O portions and CPU-bound portions. Here's how the service looks today; it uses synchronous (blocking) I/O calls as well as CPU-intensive analysis.

{% highlight csharp %}
// Warning: bad code!
class MyService
{
  public int PredictStockMarket()
  {
    // Do some I/O first.
    Thread.Sleep(1000);

    // Tons of work to do in here!
    for (int i = 0; i != 10000000; ++i)
      ;

    // Possibly some more I/O here.
    Thread.Sleep(1000);

    // More work.
    for (int i = 0; i != 10000000; ++i)
      ;

    return 42;
  }
}
{% endhighlight %}

Now, we want to start taking advantage of asynchronous code, so we can replace our blocking I/O with asynchronous I/O. But what do we do with the CPU-bound portions?

A common mistake is to wrap them in `Task.Run`.

{% highlight csharp %}
// Warning: bad code!
class MyService
{
  public async Task<int> PredictStockMarketAsync()
  {
    // Do some I/O first.
    await Task.Delay(1000);

    // Tons of work to do in here!
    await Task.Run(() =>
    {
      for (int i = 0; i != 10000000; ++i)
        ;
    });

    // Possibly some more I/O here.
    await Task.Delay(1000);

    // More work.
    await Task.Run(() =>
    {
      for (int i = 0; i != 10000000; ++i)
        ;
    });

    return 42;
  }
}
{% endhighlight %}

The problems here are the [same problems as last time]({% post_url 2013-11-07-taskrun-etiquette-examples-dont-use %}). We're still using `Task.Run` in the implementation, and we get all the problems that come along with that. It's still a fake-asynchronous method.

Well, the API can't be asynchronous (since it has CPU-bound portions) and it can't be synchronous (since we want to use asynchronous I/O). So, unfortunately there isn't an ideal solution here. To be clear, we _are_ talking about an extremely rare edge case; the vast majority of services are either asynchronous or CPU-bound, not both.

I've discussed this scenario with some Microsoft employees. The Roslyn team in particular has encountered this situation, where some of their operations need to combine heavy file I/O with non-trivial CPU usage. (These conversations occurred before I was an MVP, so this information is not under an NDA).

They concluded that the best solution is to use an asynchronous signature but document the method clearly so that its CPU-bound nature will not be surprising.

{% highlight csharp %}
class MyService
{
  /// <summary>
  /// This method is CPU-bound!
  /// </summary>
  public async Task<int> PredictStockMarketAsync()
  {
    // Do some I/O first.
    await Task.Delay(1000);

    // Tons of work to do in here!
    for (int i = 0; i != 10000000; ++i)
      ;

    // Possibly some more I/O here.
    await Task.Delay(1000);

    // More work.
    for (int i = 0; i != 10000000; ++i)
      ;

    return 42;
  }
}
{% endhighlight %}

This allows UI-based clients to properly use `Task.Run` to call the service, while ASP.NET clients would just call the method directly.

{% highlight csharp %}
private async void MyButton_Click(object sender, EventArgs e)
{
  await Task.Run(() => myService.PredictStockMarketAsync());
}

public class StockMarketController: Controller
{
  public async Task<ActionResult> IndexAsync()
  {
    var result = await myService.PredictStockMarketAsync();
    return View(result);
  }
}
{% endhighlight %}

Doing CPU-bound work in a method with an asynchronous signature is not ideal, but it does allow every possible client to use the service in the way that makes most sense for them. Each client makes the best use of its own threading situation.

In conclusion, even in the rare and complex cases, it is still best to **use `Task.Run` at the invocation, not in the implementation**.

