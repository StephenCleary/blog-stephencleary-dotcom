---
layout: post
title: "Cancellation, Part 2: Requesting Cancellation"
series: "Cancellation"
seriesTitle: "Requesting Cancellation"
description: "Creating cancellation tokens and cancelling them."
---

Last time we covered the basic cancellation contract. Responding code takes a `CancellationToken`, which is a way to communicate a cancellation request. Today we're looking at how to create `CancellationToken`s and how to request cancellation.

## CancellationTokenSource

Some `CancellationToken`s are provided by a framework or library that you're using. For example, ASP.NET will provide you a `CancellationToken` that represents an unexpected client disconnection. As another example, Polly can provide your delegate with a `CancellationToken` that represents a more generic timeout (e.g., a timeout policy being triggered).

For other scenarios, you'll need to provide your own `CancellationToken`. You can use the `CancellationToken` constructor or `CancellationToken.None` to create a cancellation token that is either signalled (and always signalled) or unsignalled (and never signalled).

But in the general case, when you want to create a `CancellationToken` that can be cancelled later, then you'll need to use `CancellationTokenSource`.

Each `CancellationTokenSource` controls its own set of `CancellationToken`s. Each `CancellationToken` created from a `CancellationTokenSource` is just a small `struct` that refers back to its `CancellationTokenSource`. A `CancellationToken` can only respond to cancellation requests; the `CancellationTokenSource` is necessary to request cancellation. So the requesting code creates the `CancellationTokenSource` and keeps a reference to it (later, using it to request cancellation), and the responding code just gets a `CancellationToken` and uses that to respond to the cancellation requests.

## Timeouts

One common need for cancellation is implementing a timeout. The solution is to have a timer that requests cancellation when it expires. This is actually common enough that `CancellationTokenSource` has this behavior built-in. You can either use the `CancellationTokenSource` constructor that takes a delay, or call `CancelAfter` on an existing `CancellationTokenSource`.

For example, if you want to apply a timeout to a code scope:

{% highlight csharp %}
async Task DoSomethingWithTimeoutAsync()
{
    // Create a CTS that cancels after 5 minutes.
    using CancellationTokenSource cts = new(TimeSpan.FromMinutes(5));

    // Pass the token for that CTS to lower-level code.
    await DoSomethingAsync(cts.Token);

    // At the end of this method, the CTS is disposed.
    // All of its tokens should not be used after this point.
}
{% endhighlight %}

## Manual Cancellation

For a fully generic solution, create a `CancellationTokenSource`, and at some point in the future call its `Cancel` method to manually request cancellation.

One example is a GUI application with an actual "Cancel" button:

{% highlight csharp %}
private CancellationTokenSource? _cts;

async void StartButton_Click(..)
{
    // Create a CTS for manual cancellation requests.
    using var cts = _cts = new();

    try
    {
        // Pass the token for that CTS to lower-level code.
        await DoSomethingAsync(_cts.Token);
    }
    catch (Exception ex)
    {
        .. // Display error in UI.
    }
}

async void CancelButton_Click(..)
{
    // Manually cancel the CTS.
    _cts!.Cancel();
}
{% endhighlight %}

The code above shows the basic idea, but has some serious problems that you wouldn't want to have in production. For one thing, the cancel button can be clicked when `_cts` is `null`, causing a `NullReferenceException`. Also, the start button handler will blindly overwrite any `_cts` value, ignoring any existing ongoing operation.

The proper resolution of these issues depends on your desired user experience and nature of the operation. Just to make this example more complete and production-ready, let's implement the following:

- Either the start or cancel buttons should be enabled at any time, never both.
- If the operation completes on its own, the start button should be enabled and the cancel button disabled.
- If one operation is cancelled, the start button should remain disabled until the operation completes (either successfully or with an `OperationCanceledException`).
- After the operation is cancelled, the cancel button remains enabled but becomes a noop.

These requirements result in this kind of code:

{% highlight csharp %}
Constructor() => CancelButton.Enabled = false;

private CancellationTokenSource? _cts;

async void StartButton_Click(..)
{
    StartButton.Enabled = false;
    CancelButton.Enabled = true;

    // Create a CTS for manual cancellation requests.
    using var cts = _cts = new();

    try
    {
        // Pass the token for that CTS to lower-level code.
        await DoSomethingAsync(_cts.Token);
        .. // Display success in UI.
    }
    catch (Exception ex)
    {
        .. // Display error in UI.
    }
    finally
    {
        StartButton.Enabled = true;
        CancelButton.Enabled = false;
    }
}

async void CancelButton_Click(..)
{
    // Manually cancel the CTS.
    _cts!.Cancel();
}
{% endhighlight %}

You may wish to have different requirements. For example:

- Either the start or cancel buttons should be enabled at any time, never both.
- If the operation completes on its own, the start button should be enabled and the cancel button disabled.
- ~If one operation is cancelled, the start button should remain disabled until the operation completes (either successfully or with an `OperationCanceledException`).~ *If one operation is cancelled, the start button should become enabled immediately. The cancelled operation does not update the UI.*
- ~After the operation is cancelled, the cancel button remains enabled but becomes a noop.~

Since the new requirements allow the user to start a new operation *as soon as the old operation is cancelled* (without waiting for the old operation to complete), the "update the UI" code needs to be guarded to ensure only the current operation updates the UI:

{% highlight csharp %}
Constructor() => CancelButton.Enabled = false;

private CancellationTokenSource? _cts;

async void StartButton_Click(..)
{
    StartButton.Enabled = false;
    CancelButton.Enabled = true;

    // Create a CTS for manual cancellation requests.
    using var cts = _cts = new();

    // In this method, we can check whether we are the current operation by doing (cts == _cts)
    // This works because _cts changes every time the start button is clicked.

    try
    {
        // Pass the token for that CTS to lower-level code.
        await DoSomethingAsync(_cts.Token);
        if (cts == _cts)
        {
            .. // Display success in UI.
        }
    }
    catch (Exception ex)
    {
        if (cts == _cts)
        {
            .. // Display error in UI.
        }
    }
}

async void CancelButton_Click(..)
{
    StartButton.Enabled = true;
    CancelButton.Enabled = false;

    // Manually cancel the CTS.
    _cts!.Cancel();
}
{% endhighlight %}

There are many other options available, depending on your desired user experience. For example, you might choose to keep the start button enabled and just have it implicitly cancel the previous operation (if any). Whatever you end up with, just be sure to walk through all the possible states of your UI and ensure that your handlers are interacting with your `CancellationTokenSource` instances appropriately.

## Cleaning Up: Cancelling and Disposing

// TODO: Does CancelAfter only use one timer? And is that timer cleaned up when cancelled and disposed?

To avoid resource leaks, it's important to clean up your `CancellationTokenSource` instances. There are a couple of kinds of resources that are cleaned up: first, the timeout timer (if any) is freed; second, any "listeners" attached to `CancellationToken`s are freed (we'll cover "listening" registrations later in this series). This cleanup is done when the `CancellationTokenSource` is cancelled *or* when it's disposed. You can either cancel or dispose, but you should ensure one or the other is done to avoid resource leaks.

The examples in this blog post always dispose the `CancelltionTokenSource` when the responding code is done executing (and thus the `CancellationToken`s are no longer used). If the `CancellationToken` is saved and used later, then you *don't* want to dispose the `CancellationTokenSource`. In that case, you'd want to keep the `CancellationTokenSource` alive until you are sure that all code is done with its `CancellationToken`s. This is a more advanced case, and often it's more convenient to cancel the `CancellationTokenSource` rather than disposing it.
