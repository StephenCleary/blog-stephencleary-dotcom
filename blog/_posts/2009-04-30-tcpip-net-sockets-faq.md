---
layout: post
title: "TCP/IP .NET Sockets FAQ"
series: "TCP/IP .NET Sockets FAQ"
seriesOrder: 0
seriesTitle: "Overview"
---
This is an attempt to address some TCP/IP frequently asked questions and present best practices. While the [WinSock Programmer's FAQ](http://tangentsoft.net/wskfaq/) will remain the ultimate FAQ for native code, there is a growing need for a simplified version that addresses the managed interface to TCP/IP sockets.

**Section 1 - Application Protocol Design**  

[1.1 - Message framing]({% post_url 2009-04-30-message-framing %}), also known as:  

&nbsp; "One side sent X bytes, but the other side only got Y bytes."  

&nbsp; "One side sent several packets, but the other side only got one packet, which was all the sent packets appended together."  

&nbsp; "I need the function that will send exactly one packet of data."  

[1.2 - Detection of half-open (dropped) connections]({% post_url 2009-05-16-detection-of-half-open-dropped %}), also known as:  

&nbsp; "My socket doesn't detect a lost connection; it just sits there forever waiting for more data to arrive."  

[1.3 - Application Protocol Specifications]({% post_url 2009-06-30-application-protocol-specifications %})  

[1.4 - XML over TCP/IP]({% post_url 2009-07-01-xml-over-tcpip %})  

**Section 2 - Socket Class**  

[2.1 - Socket operations]({% post_url 2009-05-05-socket-operations %})  

[2.2 - Error handling]({% post_url 2009-05-14-error-handling %})  

[2.3 - Using Socket as a client socket]({% post_url 2009-05-23-using-socket-as-client-socket %})  

[2.4 - Using Socket as a server (listening) socket]({% post_url 2009-05-27-using-socket-as-server-listening-socket %})  

[2.5 - Using Socket as a connected socket]({% post_url 2009-06-13-using-socket-as-connected-socket %})

**Section 3 - Miscellaneous**  

[3.1 - Resources]({% post_url 2009-05-04-tcpip-resources %})  

[3.2 - Getting the local IP address]({% post_url 2009-05-18-getting-local-ip-address %})

**Section C - Code**  

[C.1 - Length-prefix message framing for streams]({% post_url 2009-04-30-sample-code-length-prefix-message %})  

[C.2 - Getting the local IP addresses]({% post_url 2009-05-18-getting-local-ip-addresses %})

