---
layout: post
title: "Using Socket as a Connected Socket"
tags: [".NET", "TCP/IP sockets"]
---


(This post is part of the [TCP/IP .NET Sockets FAQ](http://blog.stephencleary.com/2009/04/tcpip-net-sockets-faq.html))





A connected socket is one which has a connection to the remote side. When a client socket connects to a listening server socket, the result is two connected sockets: the client socket becomes connected, and the listening server creates a new socket that is connected. For more details about establishing or listening for socket connections, see [Using Socket as a Client Socket](http://blog.stephencleary.com/2009/05/using-socket-as-client-socket.html) and [Using Socket as a Server (Listening) Socket](http://blog.stephencleary.com/2009/05/using-socket-as-server-listening-socket.html).



> **Important note: ** A socket only _believes_ it is currently connected; it can never know for sure. It is possible for one side of a connection to realize it is no longer connected, while the other side continues believing it is connected. This is called the "half-open problem", and is covered in detail in [Detection of Half-Open (Dropped) Connections](http://blog.stephencleary.com/2009/05/detection-of-half-open-dropped.html).




There are two primary operations performed on connected sockets: Read and Write. Connected sockets may also Disconnect or Close the connection; these operations will be covered in more detail in a future FAQ entry.



## Writing



A socket may be written to at any time. A Write operation places bytes into the outgoing stream. If using asynchronous Write operations, multiple Write operations may be started, and the bytes will be placed into the outgoing stream in the correct order.



> **Important note: ** The completion of a Write operation does _not_ mean that the remote side has received the data.




The Write operation completes when the local OS has copied the entire write buffer, even though those bytes may not have been sent out on the network yet. Beginning TCP programmers often balk at this, because they think that they _must know_ if data has been received by the remote side. This reaction is called "send anxiety", and will be covered in a future FAQ entry.





Write operations may not complete immediately. TCP allows one side to inform the other side of how much buffer space it has; therefore, if the remote application is reading the bytes slowly, then the socket's send buffer may fill up, and the socket may not send the outgoing bytes immediately. In fact, it is possible to end up in a deadlock situation if both sides send lots of data but read only a little. This is one reason why seasoned socket programmers almost always use asynchronous Write operations instead of synchronous.





A Write operation may (immediately) fail; this is the most common way to detect dropped connections. When a Write operation fails, the application should assume that the connection is no longer viable; see [Error Handling](http://blog.stephencleary.com/2009/05/error-handling.html) for details.



## Error Detection



It is possible that the Write operation may fail _after_ it completes. TCP has a built-in retry mechanism, so the Write will only fail if it is quite sure the connection is no longer viable. In this situation, there is not a way for the OS to signal the application, so the it places the socket into an error state. This causes future socket operations to fail.





Most TCP protocols include a notion of a "keepalive message" which is written to the socket periodically (at least if there has been no other socket activity for some time). This enables the application to detect socket errors from "successful" Write operations that later failed. It also enables the application to detect lost connections, preventing the "half-open problem". Keepalive messages are discussed in more detail in [Detection of Half-Open (Dropped) Connections](http://blog.stephencleary.com/2009/05/detection-of-half-open-dropped.html).



## Reading



As long as the socket is connected, the OS is constantly reading on behalf of the application (unless the socket's receive buffer has been disabled). The incoming bytes are stored in the socket's receive buffer and held there until the application starts a Read operation. It is possible to start more than one asynchronous Read operation at a time, but this is strongly discouraged because the operations may complete out of order.





When an application performs a Read operation, it is requesting to read _N_ bytes from a socket. The OS will not wait until all _N_ bytes arrive; rather, it may complete the Read operation when it has at least one byte to return to the application. When an application requests to Read _N_ bytes, it actually receives at least one byte and at most _N_ bytes. This clears out the OS receive buffers faster and gets the data to the application sooner, but this also means that the application must deal with "partial receives". Common ways of handling this are covered in [Message Framing](http://blog.stephencleary.com/2009/04/message-framing.html).





It is important for an application to Read from the connection on a regular basis, to prevent the deadlock situation described above under "Writing". For this reason, experienced socket programmers usually have a single asynchronous Read operation _always_ running on a connected socket. Whenever the Read operation completes, another asynchronous Read operation is started.





Another advantage of reading constantly is that misbehaving applications are immediately detected. Most protocols have certain times when it would be an error for the remote side to send data. If the application does not constantly Read, then any data arriving at that time would be treated as data arriving at a later time. It is easier to debug misbehaving applications if the incoming data is read and logged at the time it arrives at the socket.



## Reading Zero Bytes



Many stream-oriented objects (including sockets) will signal the end of the stream by returning 0 bytes in response to a Read operation. This means that the remote side of the connection has gracefully closed the connection, and the socket should be closed.





The zero-length read _must_ be treated as a special case; if it is not, the receiving code usually enters an infinite loop attempting to read more data. A zero-length read is not an error condition; it merely means that the socket has been disconnected.



> **Important note: ** Most of the MSDN .NET socket examples do _not_ handle this correctly! They will enter an infinite loop if the socket is closed by the remote side.


## Disconnecting



Either side of a socket connection may initiate a Disconnect operation or Close the socket. Once one side of the connection starts disconnecting, the socket is no longer fully connected. It is possible for it to be partially connected for some time; this state is called "half-closed". Disconnecting socket connections (including the half-closed state) will be covered in a future FAQ entry.





(This post is part of the [TCP/IP .NET Sockets FAQ](http://blog.stephencleary.com/2009/04/tcpip-net-sockets-faq.html))

