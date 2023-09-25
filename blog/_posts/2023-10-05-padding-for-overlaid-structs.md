---
layout: post
title: "Padding for Overlaid Structs"
description: "Techniques for adding padding or holes in overlaid structs used with memory-mapped files in C#."
---

[Last time]({% post_url 2023-09-28-memory-mapped-files-overlaid-structs %}) we covered the basics of memory-mapped files and how to overlay structs onto the in-memory view of the file. This time we'll take a look at different techniques to add "padding" or "holes" in our overlaid structs. Sometimes your overlaid struct is a header or container for another struct, which may be one of several different structure types. For example, a binary file may be composed of records, each with an identical header, and one field of that header is the record type, which defines how the remainder of that record should be interpreted.

For this post, we'll use the same `Data` struct we were working with last time, but this time we want to add some padding between the first and second data fields:

{% highlight csharp %}
public struct Data
{
  private int _first;
  /* TODO: forty bytes of padding goes here */
  private int _second;
}
{% endhighlight %}

Bonus points if our solution allows accessing that padding as another overlaid struct type.

## The Ideal Solution (Not Supported): Safe Fixed-Size Buffers

Ideally, we could just define a block of memory in our struct. This is similar to how it's done in unamanged languages:

{% highlight csharp %}
// The code below currently causes these compiler erorrs.
// Error CS0650 Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
// Error CS0270 Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
public struct Data
{
  private int _first;
  private byte _padding[40];
  private int _second;
}
{% endhighlight %}

There's actually been some discussion about adding this to C#; the feature is called "safe fixed-size buffers" (a.k.a., "anonymous inline arrays"). It [didn't make it into C# 11](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/low-level-struct-improvements.md#safe-fixed-size-buffers). The syntax above [was considered for C# 12](https://github.com/dotnet/csharplang/issues/1314) but [rejected earlier this year](https://github.com/dotnet/csharplang/blob/main/meetings/2023/LDM-2023-05-01.md#fixed-size-buffers).

## Inline Arrays (.NET 8.0 / C# 12)

Even though the nicer syntax above was rejected, [inline arrays](https://github.com/dotnet/csharplang/blob/f2800749ab171e9d6076f4f4bb5d0513f11c234a/proposals/csharp-12.0/inline-arrays.md) themselves have been accepted. Indeed, it is possible that a future version of C# may give us the nice syntax above, [implemented using inline arrays](https://github.com/dotnet/csharplang/blob/f2800749ab171e9d6076f4f4bb5d0513f11c234a/proposals/csharp-12.0/inline-arrays.md#detailed-design-option-2).

For now, we can just deconstruct that ourselves and write by hand what we wish the compiler would write for us:

{% highlight csharp %}
public struct Data
{
  private int _first;
  private Padding40 _padding;
  private int _second;

  [InlineArray(40)]
  private struct Padding40
  {
    private byte _start;
  }
}
{% endhighlight %}

The `InlineArrayAttribute` is a bit odd; what it's actually doing is telling the runtime to repeat the single field in that struct that many times. So `Padding40` is actually 40 bytes long.

This works fine, as long as you're on .NET 8.0; the `InlineArrayAttribute` [requires runtime support](https://github.com/dotnet/runtime/issues/61135). If you define your own `InlineArrayAttribute` and try to run this on earlier runtimes, the `Padding40` struct will be the wrong size, and `Data` will not get the correct amount of padding.

Bonus: we can access the padding as another overlaid struct type by adding this member to the `Data` struct:

{% highlight csharp %}
[UnscopedRef] public ref T PaddingAs<T>() where T : struct => ref Unsafe.As<Padding40, T>(ref _padding);
{% endhighlight %}

## Unsafe Fixed-Size Buffers

The nicer syntax above is all about taking an existing feature - [unsafe fixed-size buffers](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/unsafe-code?WT.mc_id=DT-MVP-5000058#fixed-size-buffers) - and allowing them in a safe context. If you're not on .NET 8.0 yet, you can still use the old-school unsafe fixed-size buffers:

{% highlight csharp %}
public unsafe struct Data
{
  private int _first;
  private fixed byte _padding[40];
  private int _second;
}
{% endhighlight %}

This also works fine, but has the drawback of requiring an `unsafe` context. The `Overlay` helper from the [last post]({% post_url 2023-09-28-memory-mapped-files-overlaid-structs %}) is also `unsafe`, but it would be nice if that was the _only_ `unsafe` thing and all my overlay structures don't have to be `unsafe` just to add padding.

Bonus: we can access the padding as another overlaid struct type by adding this member to the `Data` struct:

{% highlight csharp %}
public unsafe ref T PaddingAs<T>()
{
  fixed (byte* p = _padding)
    return ref Unsafe.AsRef<T>(p);
}
{% endhighlight %}

It does seem a bit awkward to me, though. The `fixed` statement is informing the GC that `_padding` can't be moved... but since this is an overlaid structure (at the address of a memory-mapped view), it can't be moved _anyway_. So it seems superfluous. Probably [not a lot of overhead](https://stackoverflow.com/a/22204244/263693); it's just that the code seems awkward: "pin this thing in memory, read the pointer value, and then unpin it".

## Explicit Struct Layout

Let's try an old-school, p/Invoke-style approach:

{% highlight csharp %}
[StructLayout(LayoutKind.Explicit)]
private struct Data
{
  [FieldOffset(0)]
  private int _first;
  [FieldOffset(44)]
  private int _second;
}
{% endhighlight %}

I was curious to know if this approach worked, and it does. I don't really recommend it, since you have to explicitly lay out your entire struct. Also, there isn't a good way of referencing the padding.

## Marshalling (Doesn't Work)

Just as a side note, _marshalling_ directives don't work. For example:

{% highlight csharp %}
// Does not work!
[StructLayout(LayoutKind.Sequential)]
private struct Data
{
  private int _first;
  [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
  private byte[] _padding;
  private int _second;
}
{% endhighlight %}

This works if we're doing p/Invoke, because it's marshalling (copying) the structure to/from unmanaged code. Since we're _overlaying_ the structure directly in memory, marshalling directives like this don't work.

## Explicit Fields

Of course, you can always define padding using multiple explicit fields. The resulting code is ugly (and IMO more awkward to maintain), but it works fine:

{% highlight csharp %}
public struct Data
{
  private int _first;
  private int _padding0, _padding1, _padding2, _padding3, _padding4, _padding5, _padding6, _padding7, _padding8, _padding9;
  private int _second;
}
{% endhighlight %}

I'm using `int` fields above so I only have to type 10 of them, as opposed to 40 `byte`-sized fields.

You can even do a bonus round with this approach by referencing the first padding member:

{% highlight csharp %}
[UnscopedRef] public ref T PaddingAs<T>() where T : struct => ref Unsafe.As<int, T>(ref _padding0);
{% endhighlight %}

Of course, if you have lots of padding (or multiple padding sections), this can get tedious.

## Conclusion

Since I'm working on a greenfield project, I've chosen to use the .NET 8.0-style `InlineArrayAttribute` approach, with the hope that the syntax becomes nicer in future versions of C#. If I had to support older .NET versions, I'd probably take the "Unsafe Fixed-Size Buffers" approach, even though it requires `unsafe` contexts for all those overlaid structs.

I hope this has been helpful to you during your memory-mapping adventures!