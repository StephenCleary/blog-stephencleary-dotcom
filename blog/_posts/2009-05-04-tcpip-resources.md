---
layout: post
title: "TCP/IP Resources"
series: "TCP/IP .NET Sockets FAQ"
seriesTitle: "Resources"
---
There are two books that any TCP/IP network programmer needs to have. Unfortunately, they were both written well before .NET, so they only deal with unmanaged code - specifically, the WinSock API. However, the .NET Socket class methods directly correspond to WinSock function calls, so knowledge can be gleaned from these books and directly applied to managed code.

- [TCP/IP Illustrated, Volume 1 (The Protocols), by Stevens](http://www.amazon.com/gp/product/0201633469?ie=UTF8&tag=stepheclearys-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=0201633469). Make a copy of pg 241 (the TCP State Transition Diagram), which is one of the most important pages ever printed. A good understanding of Chapter 18 is also important. Note that volume 1 is the only one most people need; volumes 2 and 3 delve into details about implementing TCP/IP stacks and specific (and rare) application protocols. However, volume 2 does have on the inside front cover a copy of the TCP State Transition Diagram updated with timeout events, which is nice to have.
- [Network Programming for Microsoft Windows, by Jones and Ohlund](http://www.amazon.com/gp/product/0735615799?ie=UTF8&tag=stepheclearys-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=0735615799). Chapter 5 has an excellent overview of the various I/O models available, which helps socket programmers understand how the BCL code is using asynchronous calls under the hood. This entire book should be read by TCP/IP programmers.

Note that when reading the unmanaged socket documentation, there are some potentially confusing terms:
 - The terms _blocking_ and _nonblocking_ do not mean the same as the terms _synchronous_ and _asynchronous_. Nonblocking sockets were a special quasi-asynchronous socket mode that is maintained only for backwards compatibility. Most modern WinSock programs (including .NET programs) use blocking sockets.
 - TCP is a byte stream, connection-oriented protocol. Ignore any remarks specifically for message-based or connectionless protocols; they do not apply to TCP sockets.

There is a command-line utility that comes with Windows named _netstat_ which displays TCP/IP endpoints. Other useful tools from Microsoft (that are not built in to the OS) are [TCPView](http://technet.microsoft.com/en-us/sysinternals/bb897437.aspx) (a GUI version of netstat), [Process Explorer](http://technet.microsoft.com/en-us/sysinternals/bb896653.aspx) (which also displays TCP/IP endpoints for each process), and [DbgView](http://technet.microsoft.com/en-us/sysinternals/bb896647.aspx) (which displays trace statements from the Debug and default TraceSource classes in realtime).
