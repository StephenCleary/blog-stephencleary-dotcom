---
layout: post
title: "Using Socket as a Client Socket"
---
(This post is part of the [TCP/IP .NET Sockets FAQ]({% post_url 2009-04-30-tcpip-net-sockets-faq %}))

A client socket connects to a known server socket. To do so, it usually proceeds through the operations below.

1. **Construct**. Socket construction is identical for all TCP/IP sockets; see [Socket Operations]({% post_url 2009-05-05-socket-operations %}) for details.
1. **Connect**. The client socket connects to the server socket by specifying the server's IP address and port. If the connection fails, the application may choose to notify the user and/or retry the connection (usually after a timeout), as appropriate; see [error handling]({% post_url 2009-05-14-error-handling %}) for details. Once the connection completes, the client socket is a connected socket.
1. **(repeat) Read and Write**. Asynchronous sockets normally have an active read at all times, and write as necessary; synchronous sockets must choose whether to read or write based on the application protocol. See [Using Socket as a Connected Socket]({% post_url 2009-06-13-using-socket-as-connected-socket %}) for details.
1. **Close**. Closing the socket releases the OS resources. By default, this will perform a graceful disconnect from the server in the background.

 
There are a couple of common variations for the above theme:

 1. A client application may wish to know when the disconnect from the server has completed. One reason for this is to prevent a graceful disconnect from getting promoted to an abortive disconnect, which can happen if the client exits shortly after closing its socket. In this case, the client may Disconnect the socket before Closing it, and delay exiting the process until the Disconnect has completed.
 1. A client application may wish to specify the network used for communication. This is normally done for security reasons, but there are other valid reasons to specify a network as well. If a client application wishes to control the network used for the connection, it may Bind before it Connects. In this case, usually only the IP address is specified in the Bind, allowing the OS to choose the port (the port number is set to 0, meaning "any available port"). Bind is normally a server socket operation and is covered in detail in [Using Socket as a Server (Listening) Socket]({% post_url 2009-05-27-using-socket-as-server-listening-socket %}).

(This post is part of the [TCP/IP .NET Sockets FAQ]({% post_url 2009-04-30-tcpip-net-sockets-faq %}))

