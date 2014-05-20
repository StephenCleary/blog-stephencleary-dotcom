---
layout: post
title: "Socket Operations"
---
(This post is part of the [TCP/IP .NET Sockets FAQ]({% post_url 2009-04-30-tcpip-net-sockets-faq %}))



There are a few logical operations that may be performed on a TCP/IP socket, regardless of whether the socket is synchronous or asynchronous. Each of the operations below is marked "immediate" (meaning it is completed immediately) or "delayed" (meaning it depends on the network for completion).


 
**Constructing** (immediate) - TCP/IP sockets use the InterNetwork (for IPv4) or InterNetworkV6 (for IPv6) [AddressFamily](http://msdn.microsoft.com/en-us/library/system.net.sockets.addressfamily.aspx), the Stream [SocketType](http://msdn.microsoft.com/en-us/library/system.net.sockets.sockettype.aspx), and the Tcp [ProtocolType](http://msdn.microsoft.com/en-us/library/system.net.sockets.protocoltype.aspx).  

MSDN links: [Socket](http://msdn.microsoft.com/en-us/library/2b86d684.aspx)



**Binding** (immediate) - A socket may be locally bound. This is normally done only on the server (listening) socket, and is how a server chooses the port it listens on. See [Using Socket as a Server (Listening) Socket]({% post_url 2009-05-27-using-socket-as-server-listening-socket %}) for details.  

MSDN links: [Bind](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.bind.aspx)



**Listening** (immediate) - A bound socket notifies the OS that it is almost ready to receive connections by _listening_. In spite of the term "listening", this operation only notifies the OS that the socket is _about_ to accept connections; it does not actually begin accepting connections, though the OS may accept a connection on behalf of the socket. See [Using Socket as a Server (Listening) Socket]({% post_url 2009-05-27-using-socket-as-server-listening-socket %}) for details.  

MSDN links: [Listen](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.listen.aspx)


 
**Accepting** (delayed) - A listening socket may accept an incoming connection. When an incoming connection is accepted, a new socket is created that is connected to the remote side; the listening socket continues listening. The new socket (which is connected) may be used for sending and receiving. See [Using Socket as a Server (Listening) Socket]({% post_url 2009-05-27-using-socket-as-server-listening-socket %}) for details.  

MSDN links: [Accept](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.accept.aspx), [BeginAccept](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.beginaccept.aspx), [EndAccept](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.endaccept.aspx), [AcceptAsync](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.acceptasync.aspx)


 
**Connecting** (delayed) - A (client) socket may connect to a (server) socket. TCP has a three-way handshake to complete the connection, so this operation is not instantaneous. Once a socket is connected, it may be used for sending and receiving. See [Using Socket as a Client Socket]({% post_url 2009-05-23-using-socket-as-client-socket %}) for details.  

MSDN links: [Connect](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.connect.aspx), [BeginConnect](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.beginconnect.aspx), [EndConnect](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.endconnect.aspx), [ConnectAsync](http://msdn.microsoft.com/en-us/library/bb538102.aspx)


 
**Reading** (delayed) - Connected sockets may perform a read operation. Reading takes incoming bytes from the stream and copies them into a buffer. A 0-byte read indicates a graceful closure from the remote side. See [Using Socket as a Connected Socket]({% post_url 2009-06-13-using-socket-as-connected-socket %}) for details.  

MSDN links: [Receive](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.receive.aspx), [BeginReceive](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.beginreceive.aspx), [EndReceive](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.endreceive.aspx), [ReceiveAsync](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.receiveasync.aspx)


 
**Writing** (delayed) - Connected sockets may perform a write operation. Writing places bytes in the outgoing stream. A successful write may complete before the remote OS acknowledges that the bytes were received. See [Using Socket as a Connected Socket]({% post_url 2009-06-13-using-socket-as-connected-socket %}) for details.  

MSDN links: [Send](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.send.aspx), [BeginSend](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.beginsend.aspx), [EndSend](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.endsend.aspx), [SendAsync](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.sendasync.aspx)


 
**Disconnecting** (delayed) - TCP/IP has a four-way handshake to terminate a connection gracefully: each side shuts down its own outgoing stream and receives an acknowledgment from the other side.  

MSDN links: [Disconnect](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.disconnect.aspx), [BeginDisconnect](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.begindisconnect.aspx), [EndDisconnect](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.enddisconnect.aspx), [DisconnectAsync](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.disconnectasync.aspx)


 
**Shutting down** (immediate) - Either the receiving stream or sending stream may be clamped shut. For receives, this is only a local operation; the other end of the connection is not notified. For sends, the outgoing stream is shut down (the same way Disconnect does it), and this is acknowledged by the other side; however, there is no notification of this operation completing.  

MSDN links: [Shutdown](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.shutdown.aspx)


 
**Closing** (immediate or delayed) - The actual socket resources are reclaimed when the socket is disposed (or closed). Normally, this acts immediate but is actually delayed, performing a graceful disconnect in the background and then actually reclaiming the socket resources when the disconnect completes. Socket.LingerState may be set to change Close to be a synchronous disconnect (delayed, but always synchronous), or an immediate shutdown (always immediate).  

MSDN links: [Close](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.close.aspx), [LingerState](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.lingerstate.aspx)



(This post is part of the [TCP/IP .NET Sockets FAQ]({% post_url 2009-04-30-tcpip-net-sockets-faq %}))

