---
layout: post
title: "Using Socket as a Server (Listening) Socket"
---
(This post is part of the [TCP/IP .NET Sockets FAQ]({% post_url 2009-04-30-tcpip-net-sockets-faq %}))

Normally, server sockets may accept multiple client connections. Conceptually, a server socket listens on a known port. When an incoming connection arrives, the listening socket _creates a new socket_ (the "child" socket), and establishes the connection on the child socket. The listening socket is then free to resume listening on the same port, while the child socket has an established connection with the client that is independent from its parent.

One result of this architecture is that the listening socket never actually performs a read or write operation. It is only used to create connected sockets.

The listening socket usually proceeds through the operations below.

1. **Construct**. Socket construction is identical for all TCP/IP sockets; see [Socket Operations]({% post_url 2009-05-05-socket-operations %}) for details.
1. **Bind**. Binding for listening sockets is usually done only on the port, setting the IP address parameter to IPAddress.Any ([MSDN](http://msdn.microsoft.com/en-us/library/system.net.ipaddress.any.aspx)). A Bind failure is usually due to another process already bound to that port (possibly another instance of the server process).
1. **Listen**. The listening socket actually begins listening at this point. It is not yet accepting connections, but the OS may accept connections on its behalf.  

_The confusing "backlog" parameter_. The "backlog" parameter to Socket.Listen is how many connections the OS may accept on behalf of the application. This is not the total number of active connections; it is only how many connections will be established if the application "gets behind". Once connections are Accepted, they move out of the backlog queue and no longer "count" against the backlog limit.  

_The value to pass for the "backlog" parameter_. Historically, this has been restricted to a maximum of 5, though modern systems have a cap of 200. Specifying a backlog higher than the maximum is not considered an error; the maximum value is used instead. The .NET docs fail to mention that int.MaxValue can be used to invoke the "dynamic backlog" feature (Windows Server systems only), essentially leaving it up to the OS. It is tempting to set this value very high (e.g., always passing int.MaxValue), but this would hurt system performance (on non-server machines) by pre-allocating a large amount of scarce resources. This value should be set to a reasonable amount (usually between 2 and 5), based on how many connections one is realistically expecting and how quickly they can be Accepted.
1. **(repeat) Accept**. When a socket connection is accepted by the listening socket, a new socket connection is created. The listening socket should continue listening on the same port by re-starting the Accept operation as soon as it completes. The result of a completed Accept operation is a new, connected socket. This new socket may be used for reading and writing. For more information on using connected sockets, see [Using Socket as a Connected Socket]({% post_url 2009-06-13-using-socket-as-connected-socket %}). The new socket is completely independent from the listening socket; closing either socket does not affect the other socket.
1. **Close**. Since the listening socket is never actually connected (it only accepts connected sockets), there is no Disconnect operation. Rather, closing a listening socket simply informs the OS that the socket is no longer listening and frees those resources immediately.

There are a few common variations on the above theme:

 1. A listening socket may choose to bind to an actual IP address in addition to a port. This is normally done for security reasons. If this is done, then the Bind operation may fail if the network cable is unplugged or wireless router is down.
 1. A listening socket may choose not to bind (actually, the socket is still bound; it is just bound to an OS-chosen port). This is extremely rare, and only found in very old protocols such as non-PASV FTP. This requires an application protocol that can notify the other side of the port that the OS chose to bind, and this tight coupling of the application protocol (e.g., FTP) with the transport mechanism (e.g., TCP) is not recommended. One reason is that it requires any NAT'ing (network address translating) devices to monitor the protocol and dynamically predict the necessary port forwarding.

(This post is part of the [TCP/IP .NET Sockets FAQ]({% post_url 2009-04-30-tcpip-net-sockets-faq %}))

