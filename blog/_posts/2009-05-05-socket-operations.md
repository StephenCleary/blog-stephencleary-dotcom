---
layout: post
title: "Socket Operations"
series: "TCP/IP .NET Sockets FAQ"
seriesOrder: 210
seriesTitle: "Socket Operations"
---
There are a few logical operations that may be performed on a TCP/IP socket, regardless of whether the socket is synchronous or asynchronous. Each of the operations below is marked "immediate" (meaning it is completed immediately) or "delayed" (meaning it depends on the network for completion).

 
**Constructing** (immediate) - TCP/IP sockets use the InterNetwork (for IPv4) or InterNetworkV6 (for IPv6) [AddressFamily](http://msdn.microsoft.com/en-us/library/system.net.sockets.addressfamily.aspx?WT.mc_id=DT-MVP-5000058), the Stream [SocketType](http://msdn.microsoft.com/en-us/library/system.net.sockets.sockettype.aspx?WT.mc_id=DT-MVP-5000058), and the Tcp [ProtocolType](http://msdn.microsoft.com/en-us/library/system.net.sockets.protocoltype.aspx?WT.mc_id=DT-MVP-5000058).  

MSDN links: [Socket](http://msdn.microsoft.com/en-us/library/2b86d684.aspx?WT.mc_id=DT-MVP-5000058)

**Binding** (immediate) - A socket may be locally bound. This is normally done only on the server (listening) socket, and is how a server chooses the port it listens on. See [Using Socket as a Server (Listening) Socket]({% post_url 2009-05-27-using-socket-as-server-listening-socket %}) for details.  

MSDN links: [Bind](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.bind.aspx?WT.mc_id=DT-MVP-5000058)

**Listening** (immediate) - A bound socket notifies the OS that it is almost ready to receive connections by _listening_. In spite of the term "listening", this operation only notifies the OS that the socket is _about_ to accept connections; it does not actually begin accepting connections, though the OS may accept a connection on behalf of the socket. See [Using Socket as a Server (Listening) Socket]({% post_url 2009-05-27-using-socket-as-server-listening-socket %}) for details.  

MSDN links: [Listen](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.listen.aspx?WT.mc_id=DT-MVP-5000058)

 
**Accepting** (delayed) - A listening socket may accept an incoming connection. When an incoming connection is accepted, a new socket is created that is connected to the remote side; the listening socket continues listening. The new socket (which is connected) may be used for sending and receiving. See [Using Socket as a Server (Listening) Socket]({% post_url 2009-05-27-using-socket-as-server-listening-socket %}) for details.  

MSDN links: [Accept](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.accept.aspx?WT.mc_id=DT-MVP-5000058), [BeginAccept](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.beginaccept.aspx?WT.mc_id=DT-MVP-5000058), [EndAccept](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.endaccept.aspx?WT.mc_id=DT-MVP-5000058), [AcceptAsync](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.acceptasync.aspx?WT.mc_id=DT-MVP-5000058)

 
**Connecting** (delayed) - A (client) socket may connect to a (server) socket. TCP has a three-way handshake to complete the connection, so this operation is not instantaneous. Once a socket is connected, it may be used for sending and receiving. See [Using Socket as a Client Socket]({% post_url 2009-05-23-using-socket-as-client-socket %}) for details.  

MSDN links: [Connect](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.connect.aspx?WT.mc_id=DT-MVP-5000058), [BeginConnect](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.beginconnect.aspx?WT.mc_id=DT-MVP-5000058), [EndConnect](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.endconnect.aspx?WT.mc_id=DT-MVP-5000058), [ConnectAsync](http://msdn.microsoft.com/en-us/library/bb538102.aspx?WT.mc_id=DT-MVP-5000058)

 
**Reading** (delayed) - Connected sockets may perform a read operation. Reading takes incoming bytes from the stream and copies them into a buffer. A 0-byte read indicates a graceful closure from the remote side. See [Using Socket as a Connected Socket]({% post_url 2009-06-13-using-socket-as-connected-socket %}) for details.  

MSDN links: [Receive](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.receive.aspx?WT.mc_id=DT-MVP-5000058), [BeginReceive](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.beginreceive.aspx?WT.mc_id=DT-MVP-5000058), [EndReceive](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.endreceive.aspx?WT.mc_id=DT-MVP-5000058), [ReceiveAsync](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.receiveasync.aspx?WT.mc_id=DT-MVP-5000058)

 
**Writing** (delayed) - Connected sockets may perform a write operation. Writing places bytes in the outgoing stream. A successful write may complete before the remote OS acknowledges that the bytes were received. See [Using Socket as a Connected Socket]({% post_url 2009-06-13-using-socket-as-connected-socket %}) for details.  

MSDN links: [Send](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.send.aspx?WT.mc_id=DT-MVP-5000058), [BeginSend](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.beginsend.aspx?WT.mc_id=DT-MVP-5000058), [EndSend](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.endsend.aspx?WT.mc_id=DT-MVP-5000058), [SendAsync](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.sendasync.aspx?WT.mc_id=DT-MVP-5000058)

 
**Disconnecting** (delayed) - TCP/IP has a four-way handshake to terminate a connection gracefully: each side shuts down its own outgoing stream and receives an acknowledgment from the other side.  

MSDN links: [Disconnect](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.disconnect.aspx?WT.mc_id=DT-MVP-5000058), [BeginDisconnect](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.begindisconnect.aspx?WT.mc_id=DT-MVP-5000058), [EndDisconnect](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.enddisconnect.aspx?WT.mc_id=DT-MVP-5000058), [DisconnectAsync](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.disconnectasync.aspx?WT.mc_id=DT-MVP-5000058)

 
**Shutting down** (immediate) - Either the receiving stream or sending stream may be clamped shut. For receives, this is only a local operation; the other end of the connection is not notified. For sends, the outgoing stream is shut down (the same way Disconnect does it), and this is acknowledged by the other side; however, there is no notification of this operation completing.  

MSDN links: [Shutdown](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.shutdown.aspx?WT.mc_id=DT-MVP-5000058)

 
**Closing** (immediate or delayed) - The actual socket resources are reclaimed when the socket is disposed (or closed). Normally, this acts immediate but is actually delayed, performing a graceful disconnect in the background and then actually reclaiming the socket resources when the disconnect completes. Socket.LingerState may be set to change Close to be a synchronous disconnect (delayed, but always synchronous), or an immediate shutdown (always immediate).  

MSDN links: [Close](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.close.aspx?WT.mc_id=DT-MVP-5000058), [LingerState](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.lingerstate.aspx?WT.mc_id=DT-MVP-5000058)
