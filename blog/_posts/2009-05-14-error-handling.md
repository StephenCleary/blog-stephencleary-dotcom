---
layout: post
title: "Error Handling"
tags: [".NET", "TCP/IP sockets"]
---


(This post is part of the [TCP/IP .NET Sockets FAQ](http://blog.stephencleary.com/2009/04/tcpip-net-sockets-faq.html))





Generally speaking, one should expect any socket operation to have a possibility of failure. Even the immediate operations (see [Socket Operations](http://blog.stephencleary.com/2009/05/socket-operations.html)) may fail. A socket operation error is uniquely identified by its error code (MSDN: [Windows Sockets Error Codes](http://msdn.microsoft.com/en-us/library/ms740668.aspx)).


 


Some methods (such as [Socket.EndReceive](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.endreceive.aspx)) have overloads that will return the error code one of two ways. A [SocketError](http://msdn.microsoft.com/en-us/library/system.net.sockets.socketerror.aspx) enumeration **out** parameter may be specified for these methods, which receives the error from the operation, if any. The methods without SocketError **out** parameters will raise a [SocketException](http://msdn.microsoft.com/en-us/library/system.net.sockets.socketexception.aspx) with its [ErrorCode](http://msdn.microsoft.com/en-us/library/system.net.sockets.socketexception.errorcode.aspx) set to the SocketError value. The SocketError overloads were added purely for performance reasons, and are not necessary for the vast majority of socket applications.


 
## Response to Errors
 


A connected socket should be immediately closed when any Read, Write, or Disconnect operation error is detected. Socket errors usually indicate a problem with the underlying connection (or possibly the network itself), and the socket should be considered unstable and be closed.


 


Closing a socket almost never raises an exception. Only the "fatal" exceptions (OutOfMemory, StackOverflow, ThreadAbort, and possibly others in future CLR versions) can ever be raised from Socket.Close. This makes it safe to call without requiring a try/catchall.


 


Bind and Connect failures are not uncommon. Depending on the application, one may either inform the user and exit, or retry at a later time (see below).


 


Listen or Shutdown failures are extremely rare but still possible. These failures may indicate a shortage of OS resources. For the Listen operation, consider notifying the user and then exiting; alternatively, close the listening socket and retry at a later time (see below). For the Shutdown operation, close the socket.


 


Accept operations may also fail (though this may be surprising to some). In this case, the server should simply continue accepting new connections. This may be caused by a client socket program unexpectedly exiting.


 
## Retry Timers
 


It is important not to retry socket operations immediately, since not all errors are the result of network communication. Even the Connect operation may fail immediately if the network cable is unplugged. Retrying socket operations immediately may result in high CPU usage or an exhaustion of OS socket resources.


 


A long-running server (or client) program should have a built-in automatic "retry timer". When any error is detected, the socket should be closed and the retry timer should be started. When the retry timer goes off, then the operation may be attempted again. The timer does not have to be very long: usually 1 second will suffice.


 


There are only a couple socket errors that may skip the retry timer and immediately retry: SocketError.TimedOut (WSAETIMEDOUT/10060) and SocketError.ConnectionRefused (WSAECONNREFUSED/10061). Both of these error codes indicate that an actual network timeout (WSAETIMEDOUT) or network round-trip (WSAECONNREFUSED) has taken place, so a futher "retry timeout" is unnecessary.


 
## Common Errors and Their Causes
 


There are a lot of possible WinSock errors, but it's not clear from the MSDN documentation which errors are "normal". The most common errors and their most common causes are below.





**SocketError.AddressNotAvailable / WSAEADDRNOTAVAIL / 10049** - Indicates a bad or invalid address (e.g., "255.255.255.255").





**SocketErorr.TimedOut / WSAETIMEDOUT / 10060** - This happens when trying to connect to a valid address that doesn't respond (e.g., a powered-off server or intermediate router). This may also be caused by a firewall on the remote side.





**SocketError.ConnectionRefused / WSAECONNREFUSED / 10061** - Indicates that the connection request got to a valid address that is powered on, but there is no program listening on that port. Usually, this is an indication that the server software is not running, though the computer is on. This may also be caused by a firewall, though most firewalls drop the packet (causing WSAETIMEDOUT) instead of actively refusing the connection (causing WSAECONNREFUSED).





**SocketError.ConnectionReset / WSAECONNRESET / 10054** - The remote side has abortively closed the connection. This is commonly caused by the remote process exiting or the remote computer being shut down. However, some software (especially server software) is written to abortively close connections as a normal practice, since this does reclaim server resources more quickly than a graceful close. Therefore, this is not necessarily indicative of an actual error condition; if the communication was complete (and the socket was about to be closed anyway), then this error should just be ignored.


 


**SocketError.NoBufferSpaceAvailable / WSAENOBUFS / 10055** - Technically this means that the OS has run out of buffer space for a socket. However, it's usually an indicator that the application is trying to use too many temporary ports. This may be caused by a retry rate that is too high (i.e., the retry timer timeout is too short).


 


Other errors may be seen occasionally, especially when a network is in the process of coming online or going offline (e.g., the computer is in the process of connecting to a wireless network).





(This post is part of the [TCP/IP .NET Sockets FAQ](http://blog.stephencleary.com/2009/04/tcpip-net-sockets-faq.html))

