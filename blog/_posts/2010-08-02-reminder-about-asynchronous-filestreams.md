---
layout: post
title: "A Reminder about Asynchronous FileStreams"
---
Still on my Rx kick...

The Rx team published a great blog post regarding using Rx on the server with [asynchronous Streams](https://docs.microsoft.com/en-us/archive/blogs/jeffva/rx-on-the-server-part-1-of-n-asynchronous-system-io-stream-reading). When doing this, you do need to make sure that the FileStream is actually asynchronous. (I believe the Rx team is fully aware of this caveat, but neglected to mention it in their blog post because it's not directly relevant to Rx).

To create a FileStream that is asynchronous, one _must_ either use the constructor that takes an **isAsync** boolean paramter (passing **true**), or use the constructor that takes the **FileOptions** parameter (passing a value including **FileOptions.Asynchronous**). Some of the static methods on the File class also take a **FileOptions** parameter, so these can also be used to create an asynchronous FileStream.

A FileStream that is constructed any other way is _not_ asynchronous. If the asynchronous APIs (such as BeginRead, BeginWrite, etc.) are used on a non-asynchronous FileStream, it will use a ThreadPool thread to "fake" asynchronous operations. Using Rx to wrap the Begin/End methods in this case only provides the _illusion_ of asynchronous operations.

Using Rx to access a non-asynchronous FileStream is counterproductive, burning a ThreadPool thread. However, using Rx to access an _asynchronous_ FileStream provides all the benefits of true asynchronous I/O.

