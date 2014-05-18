---
layout: post
title: "TCP/IP .NET Sockets FAQ"
tags: [".NET", "TCP/IP sockets"]
---


This is an attempt to address some TCP/IP frequently asked questions and present best practices. While the [WinSock Programmer's FAQ](http://tangentsoft.net/wskfaq/) will remain the ultimate FAQ for native code, there is a growing need for a simplified version that addresses the managed interface to TCP/IP sockets.






**Section 1 - Application Protocol Design**  
[1.1 - Message framing](http://blog.stephencleary.com/2009/04/message-framing.html), also known as:  
&nbsp; "One side sent X bytes, but the other side only got Y bytes."  
&nbsp; "One side sent several packets, but the other side only got one packet, which was all the sent packets appended together."  
&nbsp; "I need the function that will send exactly one packet of data."  
[1.2 - Detection of half-open (dropped) connections](http://blog.stephencleary.com/2009/05/detection-of-half-open-dropped.html), also known as:  
&nbsp; "My socket doesn't detect a lost connection; it just sits there forever waiting for more data to arrive."  
[1.3 - Application Protocol Specifications](http://blog.stephencleary.com/2009/06/application-protocol-specifications.html)  
[1.4 - XML over TCP/IP](http://blog.stephencleary.com/2009/07/xml-over-tcpip.html)  







**Section 2 - Socket Class**  
[2.1 - Socket operations](http://blog.stephencleary.com/2009/05/socket-operations.html)  
[2.2 - Error handling](http://blog.stephencleary.com/2009/05/error-handling.html)  
[2.3 - Using Socket as a client socket](http://blog.stephencleary.com/2009/05/using-socket-as-client-socket.html)  
[2.4 - Using Socket as a server (listening) socket](http://blog.stephencleary.com/2009/05/using-socket-as-server-listening-socket.html)  
[2.5 - Using Socket as a connected socket](http://blog.stephencleary.com/2009/06/using-socket-as-connected-socket.html)







**Section 3 - Miscellaneous**  
[3.1 - Resources](http://blog.stephencleary.com/2009/05/tcpip-resources.html)  
[3.2 - Getting the local IP address](http://blog.stephencleary.com/2009/05/getting-local-ip-address.html)







**Section C - Code**  
[C.1 - Length-prefix message framing for streams](http://blog.stephencleary.com/2009/04/sample-code-length-prefix-message.html)  
[C.2 - Getting the local IP addresses](http://blog.stephencleary.com/2009/05/getting-local-ip-addresses.html)


