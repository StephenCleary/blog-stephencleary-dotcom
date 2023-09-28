---
layout: post
title: "Memory-Mapped Files and Overlaid Structs"
description: "Mapping files into memory and accessing them through structure references in C#."
---

It has been a long, long time since I've used memory-mapped files - I think the last time was before .NET existed (!). Recently, I had a need to work with memory-mapped files in C#, and I gathered together a few resources that explain how to do it - specifically, how to map a file into memory and then "overlay" a structure on top of that memory. Since it took me a while to figure this out (and I learned about some cool upcoming features along the way), I thought I'd write this up into a proper post or two.

## Memory-Mapped Files

Memory-mapped files are a pretty cool technique, where instead of reading disk data into memory directly, you can _map_ it into the memory space of your process very quickly. Once it's mapped into your process memory, reading from that memory will read from the disk (as necessary), and writing to that memory will write out to the file (eventually). You can do cool things like create a huge file mapping (way larger than your memory), and it will Just Work, paging memory in and out of your process behind the scenes. There's a ton of information about memory-mapped files out there; if you're on Windows, I like [Windows Internals](https://www.amazon.com/Windows-Internals-Part-architecture-management/dp/0735684189?crid=1R9XTJDVYVT4R&qid=1695906423&linkCode=ll1&tag=stepheclearys-20&linkId=a9d94c8104abdd7c669e33fd6ea2d430&language=en_US&ref_=as_li_ss_tl){:rel="nofollow"} - Part 1 covers the memory manager (including memory-mapped files), and Part 2 has a few additional details on how memory-mapped files interact with the cache manager.

In C#, mapping a file into memory isn't terribly complex. First, you open the file (i.e., create a `FileStream` object). Then, you create a file mapping. Tip on the file mapping: if you're mapping an existing file, you can pass `0` for the file length to just map the entire file. Finally, you create a view on that file mapping - and this is the step that actually maps the file into the memory space for your process. You _can_ create a view over the entire file, but if you're dealing with a very large file mapping, it's common to create partial views as you need them.

This code will create a new file, a file mapping (specifying 1000 bytes as the length of the file; the file is immediately grown to this size), and a single view over the entire file:

{% highlight csharp %}
using FileStream file = new FileStream(@"tmp.dat", FileMode.Create, FileAccess.ReadWrite,
    FileShare.None, 4096, FileOptions.RandomAccess);
using MemoryMappedFile mapping = MemoryMappedFile.CreateFromFile(file, null, 1000,
    MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, leaveOpen: true);
using MemoryMappedViewAccessor view = mapping.CreateViewAccessor();
{% endhighlight %}

At this point, you have a `view`, which is a handle (actually a pointer) to the part of your process' memory that actually represents the file contents. What's really nice about this code is that it's portable; the same code works on Linux and Windows (and presumably Mac and mobile platforms, though I haven't tried those). However, pointers aren't a great interface, especially in a managed language like C#. `MemoryMappedViewAccessor` has a bunch of... well... [_awkward_ methods](https://learn.microsoft.com/en-us/dotnet/api/system.io.memorymappedfiles.memorymappedviewaccessor?view=net-7.0&WT.mc_id=DT-MVP-5000058#methods) that are essentially "read a signed 16-bit integer at this offset", "write an unsigned 32-bit integer at this offset", etc. You can also copy a struct into and out of the view, but I don't want to go through the trouble of doing a file mapping just to turn around and serialize a struct anyway.

For convenience, unmanaged languages commonly overlay a structure onto the mapped memory. This approach allows you to define the file structure as an actual `struct` and then read/write fields in that struct instead of serializing values to memory or view offsets. "Overlapped structures" might be a more common term than "overlaid structures", but I want to avoid any confusion with `OVERLAPPED`, so I'm using the term "overlaid structures" in these posts.

If you're in an unmanaged language like C++, you can just `reinterpret_cast` your file mapping view pointer to a structure pointer, and that's it: you've got a struct at the same memory address as your file view! I found that there was much less information about overlaying structs in C#, though. So, let's see how to do the same thing in C#!

## Overlaid Structs

After a bit of experimentation, this is what I ended up with:

{% highlight csharp %}
public sealed unsafe class Overlay : IDisposable
{
  private readonly MemoryMappedViewAccessor _view;
  private readonly byte* _pointer;

  public Overlay(MemoryMappedViewAccessor view)
  {
    _view = view;
    view.SafeMemoryMappedViewHandle.AcquirePointer(ref _pointer);
  }

  public void Dispose() => _view.SafeMemoryMappedViewHandle.ReleasePointer();

  public ref T As<T>() where T : struct => ref Unsafe.AsRef<T>(_pointer);
}
{% endhighlight %}

This is an `unsafe` type, but ideally this is the only place where `unsafe` is necessary.

`Overlay` is mainly just a pointer - the pointer to the view of the file that has been mapped into your process' memory. It also has a `MemoryMappedViewAccessor` member, but that's just used to free the pointer when the `Overlay` instance is disposed.

`Overlay` has a single notable member: `As<T>()`, which allows you to get a reference to a struct that overlays the mapped memory view.

<div class="alert alert-info" markdown="1">
<i class="fa fa-info-circle fa-2x pull-left"></i>

On Windows (at least), the `SafeMemoryMappedViewHandle` handle actually _is_ a pointer, and the `AcquirePointer` and `ReleasePointer` calls increment and decrement a reference counter for that handle. `Overlay` could be designed very differently (and more efficiently) if it cast the `SafeMemoryMappedViewHandle` handle value to a pointer.

However, on other platforms, I'm not sure if `SafeMemoryMappedViewHandle` is actually a pointer or not, so I've stuck with this safer implementation just to make sure the code is portable.
</div>

If you are OK with assuming `SafeMemoryMappedViewHandle` is a pointer, you can use this instead of `Overlay`:

{% highlight csharp %}
public static class MemoryMappedViewAccessorExtensions
{
  public static unsafe ref T As<T>(this MemoryMappedViewAccessor accessor) where T : struct =>
    ref Unsafe.AsRef<T>(accessor.SafeMemoryMappedViewHandle.DangerousGetHandle().ToPointer());
}
{% endhighlight %}

There's a fair amount of "unsafe" and "dangerous" in that code, though, and it also makes some implementation assumptions (specifically, that `SafeMemoryMappedViewHandle`'s handle is an actual _pointer to memory_). So, for safety, I'm just sticking with `Overlay` with its explicit `AcquirePointer` and `ReleasePointer` calls.

## Using Overlay

First, define your `struct` type, keeping in mind that the in-memory layout (including packing/padding) must reflect the on-disk file structure. Then, you can map a file just like the above code, create an `Overlay` type, and acquire a struct reference. At that point, you can read or write the struct as desired.

{% highlight csharp %}
public struct Data
{
  public int First;
  public int Second;
}
{% endhighlight %}

{% highlight csharp %}
using FileStream file = new FileStream(@"tmp.dat", FileMode.Create, FileAccess.ReadWrite,
    FileShare.None, 4096, FileOptions.RandomAccess);
using MemoryMappedFile mapping = MemoryMappedFile.CreateFromFile(file, null, 1000,
    MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, leaveOpen: true);
using MemoryMappedViewAccessor view = mapping.CreateViewAccessor();
using Overlay overlay = new Overlay(view);
ref Data data = ref overlay.As<Data>();
data.First = 1;
data.Second = 2;
{% endhighlight %}

Run the code above (works in LINQPad!), and you'll end up with a `tmp.dat` file 1000 bytes long, with the first four bytes having the value of `First` (1) and the second four bytes having the value of `Second` (2). Note that since you're reading/writing structures in memory, whatever endianness your machine is will determine the endianness of the binary file. Go ahead and pop it open in a hex editor (there's an online one called [HexEd.it](https://hexed.it/)), and take a look at the binary file itself.

## Endianness

If you're working with portable file formats, handling endianness is a necessity. Values in files on disk must be little-endian or big-endian, regardless of what processor happens to be reading or writing them. I recommend handling the differences in code with helpers, like this:

{% highlight csharp %}
public static class OverlayHelpers
{
  public static int ReadBigEndian(int bigEndian) =>
      BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(bigEndian) : bigEndian;
  public static void WriteBigEndian(out int bigEndian, int value) =>
      bigEndian = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
  public static int ReadLittleEndian(int littleEndian) =>
      BitConverter.IsLittleEndian ? littleEndian : BinaryPrimitives.ReverseEndianness(littleEndian);
  public static void WriteLittleEndian(out int littleEndian, int value) =>
      littleEndian = BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value);
}
{% endhighlight %}

The helpers above let you read/write big- or little-endian values, regardless of the endianness of the current machine. They can be used in your structure definitions as such:

{% highlight csharp %}
public struct Data
{
  // Layout
  private int _first;
  private int _second;

  // Convenience accessors
  public int First
  {
    readonly get => OverlayHelpers.ReadBigEndian(_first);
    set => OverlayHelpers.WriteBigEndian(out _first, value);
  }
  public int Second
  {
    readonly get => OverlayHelpers.ReadBigEndian(_second);
    set => OverlayHelpers.WriteBigEndian(out _second, value);
  }
}
{% endhighlight %}

Now the same program as above will always write the "first" and "second" fields as 32-bit signed big-endian values:

{% highlight csharp %}
// (this is the same code as above)
using FileStream file = new FileStream(@"tmp.dat", FileMode.Create, FileAccess.ReadWrite,
    FileShare.None, 4096, FileOptions.RandomAccess);
using MemoryMappedFile mapping = MemoryMappedFile.CreateFromFile(file, null, 1000,
    MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, leaveOpen: true);
using MemoryMappedViewAccessor view = mapping.CreateViewAccessor();
using Overlay overlay = new Overlay(view);
ref Data data = ref overlay.As<Data>();
data.First = 1;
data.Second = 2;
{% endhighlight %}

Now, the code is completely portable: any .NET runtime that supports memory-mapped files (which AFAIK is all of them) will run this code, giving you the ability to define portable binary file formats using overlaid structures.

## A Word of Warning: Alignment

Since you're overlaying structures directly into memory addresses, you have to handle all the alignment requirements yourself. Some more common architectures such as x86/x64 don't care about alignment and allow you to, e.g., define an `int` field at an offset of `1`. Other architectures do not allow unaligned access at all.

As a general guideline, align your structure members by their own size. E.g., an `int` is 4 bytes, so it should be aligned on a 4-byte boundary. Put another way, the offset of an `int` field from the beginning of the `struct` should be evenly divisible by 4. Same for other types: `long` should be aligned on an 8-byte boundary, while `byte` should be aligned on a 1-byte boundary (i.e., anywhere).

## A Word of Warning: Exceptions

Memory mapped files give you one kind of convenience by mapping files into memory, but the counterpoint is that I/O exceptions may not happen exactly when you expect them to.

When reading a file using normal I/O calls, if the read fails, then it fails right at that time. When using memory-mapped files, reads _from memory_ may cause an I/O exception. This is true even if a previous read from that same memory succeeded.

Similarly, if you write to a file using normal I/O calls, any failures are reported immediately. With memory-mapped files, _memory_ writes may cause an I/O exception. And since memory-mapped files are lazily flushed to disk, I/O exceptions may be delayed until the view is flushed (during disposal).

## Next Time

I hope this has been helpful! If anyone out there knows a way to eliminate the `unsafe` code in `Overlay`, I'd love to hear it!

Next time I'm planning to write a bit about overlaying structures with holes in them, which is a useful technique when you have "header" or "container" structures that wrap other structures possibly of different types.
